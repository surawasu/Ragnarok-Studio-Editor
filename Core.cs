using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace MavenROStudio;

public sealed record Item(int Id, string Name, string Resource, string Description, string Source, string Raw, bool UnicodeResource = false);
public sealed record Asset(string Path, string Archive, long Offset, long Compressed, long Size, byte Flags);
public sealed class Assets {
    public Dictionary<string, Asset> Entries { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, List<Asset>> Providers { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> PriorityByArchive { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Messages { get; } = [];
    public string Root = "";
    public bool LooseFirst;
    public static string Key(string path) => new string(path.Replace('/', '\\').TrimStart('\\').Select(c => c >= 'A' && c <= 'Z' ? (char)(c + 32) : c).ToArray());
    public static string RawKey(string path) => Key(Encoding.Latin1.GetString(Encoding.GetEncoding(949, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(path)));
    public static string Display(string path) => Encoding.GetEncoding(949).GetString(Encoding.Latin1.GetBytes(path));
    static byte[] Inflate(byte[] data, int expected, int limit) {
        if (expected < 0 || expected > limit) throw new InvalidDataException("Decompressed size exceeds limit.");
        using var src = new MemoryStream(data);
        using var z = new ZLibStream(src, CompressionMode.Decompress);
        var output = new byte[expected];
        z.ReadExactly(output);
        if (z.ReadByte() != -1) throw new InvalidDataException("Decompressed length mismatch.");
        return output;
    }
    public void Load(string root, bool loose, CancellationToken ct, Action<string> progress) {
        Root = Path.GetFullPath(root); LooseFirst = loose;
        var ini = Path.Combine(Root, "DATA.ini");
        if (!File.Exists(ini)) throw new FileNotFoundException("Select client folder containing DATA.ini", ini);
        bool inData = false;
        var order = new SortedDictionary<int, string>();
        foreach (var line in File.ReadLines(ini)) {
            var t = line.Trim();
            if (t.StartsWith('[')) { inData = t.Equals("[Data]", StringComparison.OrdinalIgnoreCase); continue; }
            if (!inData) continue;
            var m = Regex.Match(t, @"^(\d+)\s*=\s*(.+?)\s*$");
            if (m.Success) order[int.Parse(m.Groups[1].Value)] = m.Groups[2].Value.Trim();
        }
        if (order.Count == 0) throw new InvalidDataException("No GRF entries in [Data].");
        foreach (var pair in order) {
            ct.ThrowIfCancellationRequested();
            string file = Path.GetFullPath(Path.Combine(Root, pair.Value));
            if (!file.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("GRF path must remain inside client folder.");
            if (!File.Exists(file))
                throw new FileNotFoundException($"GRF listed in DATA.ini was not found: {pair.Value}",file);
            long archiveBytes=new FileInfo(file).Length;
            progress($"Index: {pair.Value} ({archiveBytes / 1073741824d:N2} GiB)");
            PriorityByArchive[file] = pair.Key;
            try {
                LoadGrf(file, ct);
            } catch(Exception ex) when(ex is not OperationCanceledException) {
                throw new InvalidDataException($"GRF load failed: {Path.GetFileName(file)} ({archiveBytes:N0} bytes / {archiveBytes / 1073741824d:N2} GiB). {ex.Message}",ex);
            }
            Messages.Add($"{pair.Key}: {file}");
        }
    }
    void LoadGrf(string file, CancellationToken ct) {
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var r = new BinaryReader(fs);
        var h = r.ReadBytes(46);
        if (h.Length != 46 || Encoding.ASCII.GetString(h, 0, 15) != "Master of Magic")
            throw new InvalidDataException("Invalid GRF: " + file);
        if (BitConverter.ToUInt32(h, 42) != 0x200) throw new NotSupportedException("Only GRF 2.0 supported: " + file);
        long tableOffset = 46L + (long)BitConverter.ToUInt32(h, 30);
        if (tableOffset < 46 || tableOffset > fs.Length - 8)
            throw new InvalidDataException($"Invalid GRF table offset {tableOffset:N0} for archive length {fs.Length:N0}.");
        fs.Position = tableOffset;

        long compressed = r.ReadUInt32();
        long size = r.ReadUInt32();
        const int MaxTableBytes = 512 * 1024 * 1024;
        if (compressed <= 0 || size <= 0 || compressed > MaxTableBytes || size > MaxTableBytes || compressed > fs.Length - fs.Position)
            throw new InvalidDataException($"Invalid GRF table length. compressed={compressed:N0}, decompressed={size:N0}, archive={fs.Length:N0}.");

        var tableBytes = new byte[(int)compressed];
        fs.ReadExactly(tableBytes);
        var table = Inflate(tableBytes, (int)size, MaxTableBytes);
        int p = 0, duplicates = 0;
        while (p < table.Length) {
            ct.ThrowIfCancellationRequested();
            int end = Array.IndexOf(table, (byte)0, p);
            if (end < p || end - p > 4096 || table.Length - end - 1 < 17) throw new InvalidDataException("Malformed GRF table.");
            var name = Key(Encoding.Latin1.GetString(table, p, end - p));
            p = end + 1;
            long c = BitConverter.ToUInt32(table, p);
            long original = BitConverter.ToUInt32(table, p + 8);
            byte flags = table[p + 12];
            long offset = 46L + (long)BitConverter.ToUInt32(table, p + 13);
            p += 17;
            if ((flags & 1) == 0) continue;
            if (offset < 46 || offset > fs.Length || c > fs.Length - offset)
                throw new InvalidDataException($"Invalid GRF entry bounds in {Path.GetFileName(file)}: offset={offset:N0}, compressed={c:N0}, archive={fs.Length:N0}.");
            var asset=new Asset(name,file,offset,c,original,flags);
            if(!Providers.TryGetValue(name,out var providers))Providers[name]=providers=[];
            providers.Add(asset);
            if (!Entries.TryAdd(name, asset)) duplicates++;
        }
        Messages.Add($"{Path.GetFileName(file)}: {fs.Length:N0} bytes ({fs.Length / 1073741824d:N2} GiB), {duplicates:N0} lower-priority entries shadowed");
    }
    public (byte[] Bytes, string Source) Read(string rawPath, CancellationToken ct) {
        ct.ThrowIfCancellationRequested();
        string key = Key(rawPath);
        if (LooseFirst) {
            string relative = Display(key);
            var full = Path.GetFullPath(Path.Combine(Root, relative));
            if (full.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(full)) {
                if (new FileInfo(full).Length > 64 * 1024 * 1024) throw new InvalidDataException("Loose asset too large.");
                return (File.ReadAllBytes(full), full);
            }
        }
        if (!Entries.TryGetValue(key, out var e)) throw new FileNotFoundException("Resource missing: " + Display(key));
        if ((e.Flags & 6) != 0) throw new NotSupportedException("Encrypted GRF entry: " + e.Archive + " :: " + Display(key));
        const int PreviewLimit = 64 * 1024 * 1024;
        if (e.Compressed < 0 || e.Size < 0 || e.Compressed > PreviewLimit || e.Size > PreviewLimit)
            throw new InvalidDataException($"Asset exceeds 64 MiB preview limit. compressed={e.Compressed:N0}, size={e.Size:N0}. The GRF can still be indexed.");
        using var fs = new FileStream(e.Archive, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024, FileOptions.RandomAccess);
        if (e.Offset < 0 || e.Offset > fs.Length || e.Compressed > fs.Length - e.Offset)
            throw new InvalidDataException("Asset bounds are outside the GRF archive.");
        fs.Position = e.Offset;
        byte[] bytes = new byte[(int)e.Compressed]; fs.ReadExactly(bytes);
        ct.ThrowIfCancellationRequested();
        return (Inflate(bytes, (int)e.Size, PreviewLimit), e.Archive + " :: " + Display(key));
    }
    public (byte[] Bytes, string Source) ItemImage(Item item, bool collection, CancellationToken ct) {
        if (string.IsNullOrEmpty(item.Resource)) throw new FileNotFoundException("No resource configured.");
        string folder = collection ? @"data\texture\유저인터페이스\collection\" : @"data\texture\유저인터페이스\item\";
        string resource = item.UnicodeResource ? RawKey(item.Resource) : item.Resource;
        return Read(RawKey(folder) + resource + ".bmp", ct);
    }
}

// Tokenizes literal Lua data; never executes client scripts.
public sealed class LuaData {
    readonly string text; int pos;
    public string Token = ""; public bool Quoted;
    public bool End => !Quoted && Token.Length == 0;
    public LuaData(string source) { text = source; Next(); }
    public void Next() {
        Quoted = false;
        while (pos < text.Length) {
            if (char.IsWhiteSpace(text[pos])) { pos++; continue; }
            if (pos + 1 < text.Length && text[pos..(pos+2)] == "--") {
                pos += 2;
                if (TryLong(out _)) continue;
                while (pos < text.Length && text[pos] != '\n') pos++;
                continue;
            }
            break;
        }
        if (pos >= text.Length) { Token = ""; return; }
        if (TryLong(out var longText)) { Token = longText; Quoted = true; return; }
        char c = text[pos++];
        if (c == '"' || c == '\'') {
            Quoted = true;
            var b = new StringBuilder(); bool closed = false;
            while (pos < text.Length) {
                char n = text[pos++];
                if (n == c) { closed = true; break; }
                if (n != '\\') { b.Append(n); continue; }
                if (pos >= text.Length) throw new InvalidDataException("Unterminated Lua escape.");
                n = text[pos++];
                if (n >= '0' && n <= '9') {
                    int v = n - '0', count = 1;
                    while (count < 3 && pos < text.Length && char.IsAsciiDigit(text[pos])) { v = v * 10 + text[pos++] - '0'; count++; }
                    if (v > 255) throw new InvalidDataException("Invalid Lua decimal escape.");
                    b.Append((char)v);
                } else b.Append(n switch { 'n' => '\n', 'r' => '\r', 't' => '\t', _ => n });
            }
            if (!closed) throw new InvalidDataException("Unterminated Lua string.");
            Token = b.ToString(); return;
        }
        if (char.IsLetterOrDigit(c) || c == '_') {
            int start = pos - 1;
            while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] == '_')) pos++;
            Token = text[start..pos]; return;
        }
        Token = c.ToString();
    }
    bool TryLong(out string value) {
        value = "";
        if (pos >= text.Length || text[pos] != '[') return false;
        int p = pos + 1; while (p < text.Length && text[p] == '=') p++;
        if (p >= text.Length || text[p] != '[') return false;
        string close = "]" + new string('=', p-pos-1) + "]";
        int end = text.IndexOf(close, p+1, StringComparison.Ordinal);
        if (end < 0) throw new InvalidDataException("Unterminated Lua long string.");
        value = text[(p+1)..end]; pos = end + close.Length; return true;
    }
    public bool Eat(string value) { if (Quoted || Token != value) return false; Next(); return true; }
    public void Require(string value) { if (!Eat(value)) throw new InvalidDataException($"Expected Lua token: {value} at char {pos:N0}; current token={Token}"); }
    public object Value() {
        if (Quoted) { var s = Token; Next(); return s; }
        if (!Eat("{")) { var s = Token; if (s.Length == 0) throw new InvalidDataException("Unexpected end of Lua."); Next(); return s; }
        var map = new Dictionary<string, object>(); int index = 1;
        while (!End && (Quoted || Token != "}")) {
            string key; object val;
            if (Eat("[")) { key = Token; Next(); Require("]"); Require("="); val = Value(); }
            else {
                string first = Token; bool wasString = Quoted; Next();
                if (!wasString && Eat("=")) { key = first; val = Value(); }
                else { key = (index++).ToString(); val = first; }
            }
            map[key] = val;
            if (!Eat(",")) Eat(";");
        }
        Require("}"); return map;
    }
    static bool IsLua51Bytecode(byte[] bytes) =>
        bytes.Length >= 4 && bytes[0] == 0x1B && bytes[1] == (byte)'L' && bytes[2] == (byte)'u' && bytes[3] == (byte)'a';

    static (string Raw, Func<string,string> Decode, bool UnicodeResource, string EncodingName) DecodeText(byte[] bytes,int requestedCodepage) {
        if(bytes.Length >= 3 && bytes[0]==0xEF && bytes[1]==0xBB && bytes[2]==0xBF) {
            string text=new UTF8Encoding(false,true).GetString(bytes,3,bytes.Length-3);
            return(text,s=>s,true,"UTF-8 BOM");
        }
        if(bytes.Length >= 2 && bytes[0]==0xFF && bytes[1]==0xFE) {
            string text=new UnicodeEncoding(false,true,true).GetString(bytes,2,bytes.Length-2);
            return(text,s=>s,true,"UTF-16 LE");
        }
        if(bytes.Length >= 2 && bytes[0]==0xFE && bytes[1]==0xFF) {
            string text=new UnicodeEncoding(true,true,true).GetString(bytes,2,bytes.Length-2);
            return(text,s=>s,true,"UTF-16 BE");
        }

        // Prefer real UTF-8 when the full file validates and contains non-ASCII bytes.
        // ASCII-only Lua is equivalent under all supported single-byte codepages.
        if(bytes.Any(b=>b>=0x80)) {
            try {
                string utf8=new UTF8Encoding(false,true).GetString(bytes);
                return(utf8,s=>s,true,"UTF-8 auto");
            } catch(DecoderFallbackException) {}
        }

        // For legacy CP949/CP874 itemInfo keep a Latin-1 byte-preserving token stream.
        // Display strings are decoded with the selected codepage, while resource bytes
        // remain lossless for GRF lookup.
        int cp=requestedCodepage==65001?949:requestedCodepage;
        var enc=Encoding.GetEncoding(cp,EncoderFallback.ReplacementFallback,DecoderFallback.ReplacementFallback);
        string raw=Encoding.Latin1.GetString(bytes);
        string DecodeLegacy(string s)=>enc.GetString(Encoding.Latin1.GetBytes(s));
        return(raw,DecodeLegacy,false,enc.WebName);
    }

    public static string Describe(byte[] bytes,int codepage) {
        if(IsLua51Bytecode(bytes))return "Lua 5.1 bytecode (.lub/.lua)";
        var decoded=DecodeText(bytes,codepage);
        return "Lua text ("+decoded.EncodingName+")";
    }

    public static List<Item> Parse(byte[] bytes, string source, int codepage, CancellationToken ct) {
        // Detect by content, not extension: compiled .lua and textual .lub both work.
        if (IsLua51Bytecode(bytes)) return LubData.Parse(bytes, source, codepage, ct);

        var decoded=DecodeText(bytes,codepage);
        var l = new LuaData(decoded.Raw); var result = new Dictionary<int,Item>();
        while (!l.End) {
            ct.ThrowIfCancellationRequested();
            if (!l.Eat("[")) { l.Next(); continue; }
            if (!int.TryParse(l.Token, out int id)) { l.Next(); continue; }
            l.Next();
            if (!l.Eat("]") || !l.Eat("=") || l.Token != "{") continue;
            if (l.Value() is not Dictionary<string, object> values) continue;
            string Get(string key) => values.TryGetValue(key, out var v) ? v as string ?? "" : "";
            if (!values.ContainsKey("identifiedDisplayName")) continue;
            string description = values.TryGetValue("identifiedDescriptionName", out var d) && d is Dictionary<string,object> lines
                ? string.Join(Environment.NewLine, lines.Values.OfType<string>().Select(decoded.Decode)) : "";
            result[id] = new Item(id, decoded.Decode(Get("identifiedDisplayName")), Get("identifiedResourceName"), description, source, "", decoded.UnicodeResource);
        }
        if (result.Count == 0) throw new InvalidDataException("No literal itemInfo entries found in Lua text. Dynamic Lua expressions/functions are not executed.");
        return result.Values.OrderBy(x=>x.Id).ToList();
    }
}
public static class ItemDb {
    public static List<Item> Load(string file, CancellationToken ct) {
        try {
            using var reader = new StreamReader(file, new UTF8Encoding(false, true), true);
            var yaml = new YamlStream(); yaml.Load(reader);
            if (yaml.Documents.Count == 0 || yaml.Documents[0].RootNode is not YamlMappingNode root ||
                !root.Children.TryGetValue(new YamlScalarNode("Body"), out var body) || body is not YamlSequenceNode seq)
                throw new InvalidDataException("Select an item database with a Body list (not an import-only file).");
            var items = new Dictionary<int,Item>();
            foreach (var entry in seq.OfType<YamlMappingNode>()) {
                ct.ThrowIfCancellationRequested();
                string Field(string k) => entry.Children.TryGetValue(new YamlScalarNode(k), out var value) ? value.ToString() : "";
                if (!int.TryParse(Field("Id"), out var id)) continue;
                var name = Field("Name"); if (name.Length == 0) name = Field("AegisName");
                var rawBuilder=new StringBuilder();
                foreach(var kv in entry.Children) {
                    string key=kv.Key.ToString();
                    if((key=="Script"||key=="EquipScript"||key=="UnEquipScript")&&kv.Value is YamlScalarNode scriptNode) {
                        rawBuilder.AppendLine(key+": |");
                        foreach(var line in (scriptNode.Value??"").Replace("\r","").Split('\n'))
                            if(line.Length>0)rawBuilder.AppendLine("  "+line);
                    } else rawBuilder.AppendLine(key+": "+kv.Value);
                }
                var raw=rawBuilder.ToString().TrimEnd();
                items[id] = new Item(id, name, Field("AegisName"), raw, file, raw);
            }
            return items.Values.OrderBy(x=>x.Id).ToList();
        } catch(OperationCanceledException){throw;}
        catch(Exception strictError) {
            var loose=LoadLenient(file,ct);
            if(loose.Count==0)throw new InvalidDataException($"Strict YAML failed ({strictError.Message}) and lenient rAthena scan found no items.",strictError);
            return loose;
        }
    }

    static List<Item> LoadLenient(string file,CancellationToken ct) {
        var rows=new Dictionary<int,Item>();
        var block=new List<string>();int id=0;string aegis="",name="";
        var idRx=new Regex(@"^\s*-\s+Id:\s*(\d+)\s*(?:#.*)?$",RegexOptions.Compiled);
        var aegisRx=new Regex(@"^\s+AegisName:\s*(.+?)\s*(?:#.*)?$",RegexOptions.Compiled);
        var nameRx=new Regex(@"^\s+Name:\s*(.+?)\s*(?:#.*)?$",RegexOptions.Compiled);
        void Flush() {
            if(id<=0)return;
            string cleanAegis=aegis.Trim().Trim('"','\'');
            string cleanName=(name.Length>0?name:aegis).Trim().Trim('"','\'');
            rows[id]=new Item(id,cleanName,cleanAegis,"",file,string.Join(Environment.NewLine,block));
        }
        foreach(var line in File.ReadLines(file)) {
            ct.ThrowIfCancellationRequested();
            var m=idRx.Match(line);
            if(m.Success) {
                Flush();block.Clear();id=int.Parse(m.Groups[1].Value);aegis="";name="";block.Add(line);continue;
            }
            if(id<=0)continue;
            block.Add(line);
            m=aegisRx.Match(line);if(m.Success){aegis=m.Groups[1].Value;continue;}
            m=nameRx.Match(line);if(m.Success)name=m.Groups[1].Value;
        }
        Flush();
        return rows.Values.OrderBy(x=>x.Id).ToList();
    }
}
