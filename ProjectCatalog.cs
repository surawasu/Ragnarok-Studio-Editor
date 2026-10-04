using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace MavenROStudio;

public sealed record ServerItemRef(int Id,string AegisName,string Name,string Source,string Raw="");
public sealed record ItemSyncComparison(string Status,string Reason,string ClientName,string ServerName,string ClientSource,string ServerSource);

public sealed class ProjectCatalog {
    public IReadOnlyDictionary<int,Item> ClientItems { get; private set; } = new Dictionary<int,Item>();
    public IReadOnlyDictionary<int,Item> ServerItems { get; private set; } = new Dictionary<int,Item>();
    public Assets? Assets { get; private set; }
    public string ClientRoot { get; private set; } = "";
    public string ServerRoot { get; private set; } = "";

    public List<string> ItemDbFiles { get; private set; } = [];
    public List<string> BarterFiles { get; private set; } = [];
    public List<string> PetFiles { get; private set; } = [];
    public List<string> NpcFiles { get; private set; } = [];
    public Dictionary<string,ServerItemRef> ByAegis { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<int,ServerItemRef> ById { get; private set; } = [];

    public void Update(string clientRoot,string serverRoot,IReadOnlyDictionary<int,Item> client,IReadOnlyDictionary<int,Item> server,Assets? assets) {
        ClientRoot=clientRoot; ServerRoot=serverRoot;
        ClientItems=client; ServerItems=server; Assets=assets;
    }

    public void Discover(CancellationToken ct=default) {
        ItemDbFiles=[];BarterFiles=[];PetFiles=[];NpcFiles=[];
        ByAegis=new(StringComparer.OrdinalIgnoreCase);ById=[];
        if(!Directory.Exists(ServerRoot))return;

        string db=Path.Combine(ServerRoot,"db");
        if(Directory.Exists(db)) {
            ItemDbFiles=Directory.EnumerateFiles(db,"item_db*.yml",SearchOption.AllDirectories)
                .Where(x=>!x.Contains($"{Path.DirectorySeparatorChar}import-tmpl{Path.DirectorySeparatorChar}",StringComparison.OrdinalIgnoreCase))
                .OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();
            PetFiles=Directory.EnumerateFiles(db,"pet_db*.yml",SearchOption.AllDirectories)
                .Where(x=>!x.Contains($"{Path.DirectorySeparatorChar}import-tmpl{Path.DirectorySeparatorChar}",StringComparison.OrdinalIgnoreCase))
                .OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();
        }

        string npc=Path.Combine(ServerRoot,"npc");
        if(Directory.Exists(npc)) {
            NpcFiles=Directory.EnumerateFiles(npc,"*.txt",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();
            BarterFiles=Directory.EnumerateFiles(npc,"*.yml",SearchOption.AllDirectories)
                .Where(IsYamlTypeBarter).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();
        }
        BuildAegisIndex(ct);
    }

    static bool IsYamlTypeBarter(string file) {
        try {
            using var sr=new StreamReader(file,Encoding.UTF8,true);
            for(int i=0;i<80&&!sr.EndOfStream;i++) {
                var line=sr.ReadLine();
                if(line!=null&&line.Contains("BARTER_DB",StringComparison.OrdinalIgnoreCase))return true;
            }
        } catch {}
        return Path.GetFileName(file).Contains("barter",StringComparison.OrdinalIgnoreCase);
    }

    public List<string> RecommendedItemDbFiles() {
        if(!Directory.Exists(ServerRoot))return [];
        bool renewal=false;
        string renewalHeader=Path.Combine(ServerRoot,"src","config","renewal.hpp");
        if(File.Exists(renewalHeader)) {
            renewal=File.ReadLines(renewalHeader).Any(line=>Regex.IsMatch(line,@"^\s*#define\s+RENEWAL\b"));
        }
        string mode=renewal?"re":"pre-re";
        var candidates=new[]{
            Path.Combine(ServerRoot,"db",mode,"item_db_usable.yml"),
            Path.Combine(ServerRoot,"db",mode,"item_db_equip.yml"),
            Path.Combine(ServerRoot,"db",mode,"item_db_etc.yml"),
            Path.Combine(ServerRoot,"db","import","item_db.yml"),
            Path.Combine(ServerRoot,"db","import","item_db_cos_Hi.yml"),
            Path.Combine(ServerRoot,"db","import","maven_ro_studio_item_db.yml")
        };
        return candidates.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    void BuildAegisIndex(CancellationToken ct) {
        var idRx=new Regex(@"^\s*-\s+Id:\s*(\d+)\s*$",RegexOptions.Compiled);
        var aegisRx=new Regex(@"^\s+AegisName:\s*(.+?)\s*$",RegexOptions.Compiled);
        var nameRx=new Regex(@"^\s+Name:\s*(.+?)\s*$",RegexOptions.Compiled);
        var activeFiles=RecommendedItemDbFiles();
        if(activeFiles.Count==0)activeFiles=ItemDbFiles;
        foreach(var file in activeFiles) {
            ct.ThrowIfCancellationRequested();
            int id=0; string aegis="",name="";var block=new List<string>();
            void Flush() {
                if(id<=0||aegis.Length==0)return;
                var row=new ServerItemRef(id,aegis.Trim('"','\''),name.Trim('"','\''),file,string.Join(Environment.NewLine,block));
                ById[id]=row;ByAegis[row.AegisName]=row;
            }
            foreach(var line in File.ReadLines(file)) {
                var m=idRx.Match(line);
                if(m.Success){Flush();block.Clear();id=int.Parse(m.Groups[1].Value);aegis="";name="";block.Add(line);continue;}
                if(id<=0)continue;
                block.Add(line);
                m=aegisRx.Match(line);if(m.Success){aegis=m.Groups[1].Value;continue;}
                m=nameRx.Match(line);if(m.Success)name=m.Groups[1].Value;
            }
            Flush();
        }
    }

    public bool HasItemIcon(Item item) {
        if(Assets==null||string.IsNullOrWhiteSpace(item.Resource))return false;
        string folder=@"data\texture\유저인터페이스\item\";
        string resource=item.UnicodeResource?Assets.RawKey(item.Resource):item.Resource;
        return Assets.Entries.ContainsKey(Assets.RawKey(folder)+resource+".bmp");
    }

    static string ComparableName(string value) =>
        Regex.Replace((value??"").Trim().Normalize(NormalizationForm.FormKC),@"\s+"," ");

    public ItemSyncComparison CompareFor(int id) {
        bool c=ClientItems.TryGetValue(id,out var ci);
        bool hasIndexed=ById.TryGetValue(id,out var indexed);
        bool s=ServerItems.TryGetValue(id,out var si)||(ServerItems.Count==0&&hasIndexed);
        string serverName=si?.Name??indexed?.Name??"";
        string serverSource=si?.Source??indexed?.Source??"";

        if(c&&s) {
            string clientName=ci!.Name;
            bool same=serverName.Length==0||ComparableName(clientName).Equals(ComparableName(serverName),StringComparison.OrdinalIgnoreCase);
            return new(same?"MATCH":"MISMATCH",
                same?"Display Name matches":$"Display Name differs: Client \"{clientName}\" vs Server \"{serverName}\"",
                clientName,serverName,ci.Source,serverSource);
        }

        return c
            ? new("CLIENT ONLY","Item exists only in Client",ci!.Name,"",ci.Source,"")
            : s
                ? new("SERVER ONLY","Item exists only in Server","",serverName,"",serverSource)
                : new("MISSING","Item not found in Client or Server","","","","");
    }

    public string StatusFor(int id) => CompareFor(id).Status;
}

public static class SafeProjectFile {
    public static string Backup(string file) {
        if(!File.Exists(file))return "";
        string dir=Path.Combine(Path.GetDirectoryName(file)!,".mavenro-backup");
        Directory.CreateDirectory(dir);
        string stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        string backup=Path.Combine(dir,Path.GetFileName(file)+"."+stamp+".bak");
        File.Copy(file,backup,false);
        return backup;
    }

    public static void Write(string file,string content,bool validateYaml=false) {
        if(validateYaml)ValidateYaml(content);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        Backup(file);
        string tmp=file+".mavenro.tmp";
        File.WriteAllText(tmp,content,new UTF8Encoding(false));
        File.Move(tmp,file,true);
    }

    public static void ValidateYaml(string text) {
        using var sr=new StringReader(text);
        var yaml=new YamlStream();yaml.Load(sr);
        if(yaml.Documents.Count==0)throw new InvalidDataException("YAML contains no document.");
    }

    public static string LuaString(string value)=>"\""+value.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","").Replace("\n","\\n")+"\"";
    public static string Aegis(string value,int id) {
        string s=Regex.Replace(value.Normalize(NormalizationForm.FormKD),@"[^A-Za-z0-9_]+","_").Trim('_');
        if(string.IsNullOrWhiteSpace(s))s="Maven_Item_"+id;
        if(char.IsDigit(s[0]))s="Item_"+s;
        return s.Length>48?s[..48]:s;
    }

    public static string ClientEntry(Item item,int slotCount=0,int classNum=0,bool costume=false) {
        var lines=item.Description.Replace("\r","").Split('\n');
        string desc=string.Join(",\r\n      ",lines.Select(LuaString));
        if(desc.Length==0)desc=LuaString("");
        return $@"  [{item.Id}] = {{
    unidentifiedDisplayName = {LuaString(item.Name)},
    unidentifiedResourceName = {LuaString(item.Resource)},
    unidentifiedDescriptionName = {{ {LuaString("")} }},
    identifiedDisplayName = {LuaString(item.Name)},
    identifiedResourceName = {LuaString(item.Resource)},
    identifiedDescriptionName = {{
      {desc}
    }},
    slotCount = {slotCount},
    ClassNum = {classNum},
    costume = {(costume?"true":"false")}
  }}";
    }

    static int FindMatchingBrace(string text,int open) {
        int depth=0;char quote='\0';bool escape=false,lineComment=false;
        for(int i=open;i<text.Length;i++) {
            char c=text[i];
            if(lineComment){if(c=='\n')lineComment=false;continue;}
            if(quote!='\0') {
                if(escape){escape=false;continue;}
                if(c=='\\'){escape=true;continue;}
                if(c==quote)quote='\0';
                continue;
            }
            if(c=='-'&&i+1<text.Length&&text[i+1]=='-'){lineComment=true;i++;continue;}
            if(c=='"'||c=='\''){quote=c;continue;}
            if(c=='{')depth++;
            else if(c=='}'&&--depth==0)return i;
        }
        return -1;
    }

    static void WriteBytes(string file,byte[] bytes) {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        Backup(file);
        string tmp=file+".mavenro.tmp";
        File.WriteAllBytes(tmp,bytes);
        File.Move(tmp,file,true);
    }

    public static (bool Found,int SlotCount,int ClassNum,bool Costume) ReadClientTableMetadata(string file,string tableName,int itemId) {
        if(!File.Exists(file))return(false,0,0,false);
        string raw=Encoding.Latin1.GetString(File.ReadAllBytes(file));
        var tableMatch=Regex.Match(raw,@"\b"+Regex.Escape(tableName)+@"\s*=\s*\{");
        if(!tableMatch.Success)return(false,0,0,false);
        int tableOpen=raw.IndexOf('{',tableMatch.Index);
        int tableClose=FindMatchingBrace(raw,tableOpen);
        if(tableClose<0)return(false,0,0,false);
        string tableBody=raw[(tableOpen+1)..tableClose];
        var entryMatch=Regex.Match(tableBody,$@"(?m)^[ \t]*\[\s*{itemId}\s*\]\s*=\s*\{{");
        if(!entryMatch.Success)return(false,0,0,false);
        int start=tableOpen+1+entryMatch.Index;
        int open=raw.IndexOf('{',start);
        int close=FindMatchingBrace(raw,open);
        if(close<0)return(false,0,0,false);
        string block=raw[start..(close+1)];
        int slot=0,classNum=0;bool costume=false;
        var m=Regex.Match(block,@"\bslotCount\s*=\s*(\d+)",RegexOptions.IgnoreCase);if(m.Success)int.TryParse(m.Groups[1].Value,out slot);
        m=Regex.Match(block,@"\bClassNum\s*=\s*(\d+)",RegexOptions.IgnoreCase);if(m.Success)int.TryParse(m.Groups[1].Value,out classNum);
        m=Regex.Match(block,@"\bcostume\s*=\s*(true|false)",RegexOptions.IgnoreCase);if(m.Success)costume=m.Groups[1].Value.Equals("true",StringComparison.OrdinalIgnoreCase);
        return(true,slot,classNum,costume);
    }

    // Updates only one entry inside an existing custom itemInfo table.
    // Existing bytes outside the entry are preserved exactly, preventing accidental
    // recoding of legacy CP874/CP949/mixed-encoding client files.
    public static void UpsertClientTableEntry(string file,string tableName,Item item,int codepage=949,int? slotOverride=null,int? classOverride=null,bool? costumeOverride=null) {
        if(!File.Exists(file))throw new FileNotFoundException("Client itemInfo target not found.",file);
        byte[] bytes=File.ReadAllBytes(file);
        string raw=Encoding.Latin1.GetString(bytes);
        var tableMatch=Regex.Match(raw,@"\b"+Regex.Escape(tableName)+@"\s*=\s*\{");
        if(!tableMatch.Success)throw new InvalidDataException($"Lua table {tableName} not found in {Path.GetFileName(file)}.");
        int tableOpen=raw.IndexOf('{',tableMatch.Index);
        int tableClose=FindMatchingBrace(raw,tableOpen);
        if(tableClose<0)throw new InvalidDataException("Unbalanced itemInfo table.");

        int slot=0,classNum=0;bool costume=false;
        int replaceStart=-1,replaceEnd=-1;
        string tableBody=raw[(tableOpen+1)..tableClose];
        var entryMatch=Regex.Match(tableBody,$@"(?m)^[ \t]*\[\s*{item.Id}\s*\]\s*=\s*\{{");
        if(entryMatch.Success) {
            replaceStart=tableOpen+1+entryMatch.Index;
            int entryOpen=raw.IndexOf('{',replaceStart);
            int entryClose=FindMatchingBrace(raw,entryOpen);
            if(entryClose<0)throw new InvalidDataException($"Unbalanced itemInfo entry {item.Id}.");
            string oldBlock=raw[replaceStart..(entryClose+1)];
            var m=Regex.Match(oldBlock,@"\bslotCount\s*=\s*(\d+)",RegexOptions.IgnoreCase);if(m.Success)int.TryParse(m.Groups[1].Value,out slot);
            m=Regex.Match(oldBlock,@"\bClassNum\s*=\s*(\d+)",RegexOptions.IgnoreCase);if(m.Success)int.TryParse(m.Groups[1].Value,out classNum);
            m=Regex.Match(oldBlock,@"\bcostume\s*=\s*(true|false)",RegexOptions.IgnoreCase);if(m.Success)costume=m.Groups[1].Value.Equals("true",StringComparison.OrdinalIgnoreCase);
            replaceEnd=entryClose+1;
            if(replaceEnd<raw.Length&&raw[replaceEnd]==',')replaceEnd++;
        }

        if(slotOverride.HasValue)slot=slotOverride.Value;
        if(classOverride.HasValue)classNum=classOverride.Value;
        if(costumeOverride.HasValue)costume=costumeOverride.Value;
        var enc=Encoding.GetEncoding(codepage,EncoderFallback.ReplacementFallback,DecoderFallback.ReplacementFallback);
        string encodedEntry=Encoding.Latin1.GetString(enc.GetBytes(ClientEntry(item,slot,classNum,costume)+","));
        string updated;
        if(replaceStart>=0)updated=raw[..replaceStart]+encodedEntry+raw[replaceEnd..];
        else updated=raw[..tableClose]+"\r\n"+encodedEntry+"\r\n"+raw[tableClose..];
        WriteBytes(file,Encoding.Latin1.GetBytes(updated));
    }

    public static void UpsertClientOverride(string file,Item item,int codepage=949) {
        var map=new Dictionary<int,Item>();
        if(File.Exists(file)) {
            try {
                foreach(var row in LuaData.Parse(File.ReadAllBytes(file),file,codepage,CancellationToken.None))map[row.Id]=row;
            } catch {}
        }
        map[item.Id]=item with{Source=file};
        var sb=new StringBuilder("tbl = {\r\n");
        bool first=true;
        foreach(var row in map.Values.OrderBy(x=>x.Id)) {
            if(!first)sb.Append(",\r\n");
            sb.Append(ClientEntry(row));first=false;
        }
        sb.Append("\r\n}\r\n");
        Write(file,sb.ToString());
    }

    static string RawField(Item item,string key) {
        if(string.IsNullOrWhiteSpace(item.Raw))return "";
        var m=Regex.Match(item.Raw,$@"(?mi)^\s*(?:-\s*)?{Regex.Escape(key)}:\s*([^\r\n#]+)");
        return m.Success?m.Groups[1].Value.Trim().Trim('"','\''):"";
    }

    public static string ServerYaml(IEnumerable<Item> items) {
        var sb=new StringBuilder();
        sb.AppendLine("Header:");sb.AppendLine("  Type: ITEM_DB");sb.AppendLine("  Version: 3");sb.AppendLine();
        sb.AppendLine("Body:");
        string[] optional=["Buy","Sell","Weight","Attack","MagicAttack","Defense","Range","Slots","WeaponLevel","ArmorLevel","EquipLevelMin","EquipLevelMax","View","Refineable","Gradable"];
        foreach(var i in items.OrderBy(x=>x.Id)) {
            string aegis=string.IsNullOrWhiteSpace(i.Resource)?RawField(i,"AegisName"):i.Resource;
            if(string.IsNullOrWhiteSpace(aegis))aegis=Aegis(i.Name,i.Id);
            string type=RawField(i,"Type");if(string.IsNullOrWhiteSpace(type))type="Etc";
            sb.AppendLine($"  - Id: {i.Id}");
            sb.AppendLine($"    AegisName: {Aegis(aegis,i.Id)}");
            sb.AppendLine($"    Name: {YamlScalar(i.Name)}");
            sb.AppendLine($"    Type: {type}");
            foreach(var key in optional) {
                string value=RawField(i,key);
                if(value.Length>0)sb.AppendLine($"    {key}: {value}");
            }
            AppendScriptBlock(sb,"Script",RawScriptField(i,"Script"));
            AppendScriptBlock(sb,"EquipScript",RawScriptField(i,"EquipScript"));
            AppendScriptBlock(sb,"UnEquipScript",RawScriptField(i,"UnEquipScript"));
        }
        return sb.ToString();
    }

    static string YamlScalar(string value)=>"\""+value.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r"," ").Replace("\n"," ")+"\"";

    static string RawScriptField(Item item,string key) {
        if(string.IsNullOrWhiteSpace(item.Raw))return "";
        var lines=item.Raw.Replace("\r","").Split('\n');
        for(int i=0;i<lines.Length;i++) {
            var header=Regex.Match(lines[i],$@"^(?<indent>\s*){Regex.Escape(key)}:\s*(?<rest>.*)$");
            if(!header.Success)continue;
            string rest=header.Groups["rest"].Value.Trim();
            if(rest!="|")return rest.Trim('"','\'');
            int headerIndent=header.Groups["indent"].Value.Length;
            var body=new List<string>();
            for(int j=i+1;j<lines.Length;j++) {
                string line=lines[j];
                if(line.Length==0){body.Add("");continue;}
                int indent=line.TakeWhile(char.IsWhiteSpace).Count();
                if(indent<=headerIndent)break;
                int remove=Math.Min(line.Length,headerIndent+2);
                body.Add(line[remove..]);
            }
            return string.Join("\n",body).TrimEnd();
        }
        return "";
    }

    static void AppendScriptBlock(StringBuilder sb,string key,string script) {
        if(string.IsNullOrWhiteSpace(script))return;
        sb.AppendLine($"    {key}: |");
        foreach(var line in script.Replace("\r","").Split('\n'))
            if(line.Length>0)sb.AppendLine("      "+line);
    }

    public static void UpsertServerOverride(string file,Item item) {
        var map=new Dictionary<int,Item>();
        if(File.Exists(file)) {
            try{foreach(var row in ItemDb.Load(file,CancellationToken.None))map[row.Id]=row;}catch{}
        }
        map[item.Id]=new Item(item.Id,item.Name,item.Resource,item.Description,file,item.Raw);
        Write(file,ServerYaml(map.Values),true);
    }

    public static void EnsureServerImport(string serverRoot,string importRelative) {
        string master=Path.Combine(serverRoot,"db","item_db.yml");
        if(!File.Exists(master))throw new FileNotFoundException("db/item_db.yml not found",master);
        string text=File.ReadAllText(master);
        string normalized=importRelative.Replace('\\','/');
        if(text.Contains(normalized,StringComparison.OrdinalIgnoreCase))return;
        if(!text.EndsWith("\n"))text+=Environment.NewLine;
        text+="  - Path: "+normalized+Environment.NewLine;
        Write(master,text,true);
    }
}
