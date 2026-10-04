using System.Text;
using System.Globalization;
namespace MavenROStudio;

// Bounded Lua 5.1 literal-table decoder. No native Lua, I/O, calls or loops are executed.
static class LubData {
    sealed record Proto(uint[] Code, object?[] Constants, Proto[] Children, byte Upvalues);
    static int Count(BinaryReader r,int limit=20000000) {
        int n=r.ReadInt32();if(n<0||n>limit)throw new InvalidDataException("LUB count out of range.");return n;
    }
    static string String(BinaryReader r,int sizeT) {
        ulong n=sizeT==4?r.ReadUInt32():r.ReadUInt64();
        if(n==0)return "";
        if(n>64*1024*1024||n>(ulong)(r.BaseStream.Length-r.BaseStream.Position))throw new InvalidDataException("Invalid LUB string size.");
        var b=r.ReadBytes((int)n);if(b[^1]!=0)throw new InvalidDataException("LUB string terminator missing.");
        return Encoding.Latin1.GetString(b,0,b.Length-1);
    }
    static Proto Read(BinaryReader r,int sizeT,int depth=0) {
        if(depth>64)throw new InvalidDataException("LUB nesting limit.");
        String(r,sizeT);r.ReadInt32();r.ReadInt32();
        byte up=r.ReadByte();r.ReadByte();r.ReadByte();r.ReadByte();
        int n=Count(r);var code=new uint[n];for(int i=0;i<n;i++)code[i]=r.ReadUInt32();
        n=Count(r);var constants=new object?[n];
        for(int i=0;i<n;i++)constants[i]=r.ReadByte() switch {
            0=>null,1=>r.ReadByte()!=0,3=>r.ReadDouble(),4=>String(r,sizeT),_=>throw new InvalidDataException("Unknown Lua constant")
        };
        n=Count(r,100000);var children=new Proto[n];for(int i=0;i<n;i++)children[i]=Read(r,sizeT,depth+1);
        n=Count(r);r.BaseStream.Seek(checked(n*4L),SeekOrigin.Current);
        n=Count(r);for(int i=0;i<n;i++){String(r,sizeT);r.ReadInt32();r.ReadInt32();}
        n=Count(r);for(int i=0;i<n;i++)String(r,sizeT);
        if(r.BaseStream.Position>r.BaseStream.Length)throw new InvalidDataException("Truncated LUB.");
        return new(code,constants,children,up);
    }
    static string Key(object? o)=>o switch {string s=>s,double d=>d.ToString("G",CultureInfo.InvariantCulture),_=>throw new InvalidDataException("Unsupported table key")};
    public static List<Item> Parse(byte[] bytes,string source,int cp,CancellationToken ct) {
        using var r=new BinaryReader(new MemoryStream(bytes));
        if(bytes.Length<12)throw new InvalidDataException("Truncated LUB header.");
        var h=r.ReadBytes(12);
        if(h[0]!=27||h[1]!=76||h[2]!=117||h[3]!=97||h[4]!=0x51||h[5]!=0||h[6]!=1||h[7]!=4||(h[8]!=4&&h[8]!=8)||h[9]!=4||h[10]!=8||h[11]!=0)
            throw new NotSupportedException("Only standard little-endian Lua 5.1 LUB supported.");
        var p=Read(r,h[8]);
        var reg=new object?[512];var globals=new Dictionary<string,object?>();
        object? RK(int v)=>v>=256?p.Constants[v-256]:reg[v];
        for(int pc=0;pc<p.Code.Length;pc++) {
            if((pc&1023)==0)ct.ThrowIfCancellationRequested();
            uint ins=p.Code[pc];int op=(int)(ins&63),a=(int)(ins>>6&255),b=(int)(ins>>23&511),c=(int)(ins>>14&511),bx=(int)(ins>>14);
            switch(op) {
                case 0:reg[a]=reg[b];break;
                case 1:reg[a]=p.Constants[bx];break;
                case 2:reg[a]=b!=0;if(c!=0)pc++;break;
                case 3:for(int i=a;i<=b;i++)reg[i]=null;break;
                case 5:globals.TryGetValue(Key(p.Constants[bx]),out reg[a]);break;
                case 6:reg[a]=reg[b] is Dictionary<string,object?> get&&get.TryGetValue(Key(RK(c)),out var value)?value:null;break;
                case 7:globals[Key(p.Constants[bx])]=reg[a];break;
                case 9:
                    if(reg[a] is not Dictionary<string,object?> t)throw new InvalidDataException("LUB SETTABLE target invalid.");
                    t[Key(RK(b))]=RK(c);break;
                case 10:reg[a]=new Dictionary<string,object?>();break;
                case 30:pc=p.Code.Length;break;
                case 34:
                    if(b==0)throw new NotSupportedException("Dynamic SETLIST unsupported.");
                    if(c==0)c=checked((int)p.Code[++pc]);
                    if(reg[a] is not Dictionary<string,object?> table)throw new InvalidDataException("Invalid SETLIST.");
                    for(int i=1;i<=b;i++)table[((c-1)*50+i).ToString(CultureInfo.InvariantCulture)]=reg[a+i];
                    break;
                case 36:reg[a]="<function: not executed>";pc+=p.Children[bx].Upvalues;break;
                default:throw new NotSupportedException($"LUB contains dynamic opcode {op} at {pc}. Literal-table reader refuses executable logic.");
            }
        }
        var candidates=globals.Values.Concat(reg).OfType<Dictionary<string,object?>>().Distinct().ToList();
        Dictionary<string,object?>? root=candidates.OrderByDescending(t=>t.Values.OfType<Dictionary<string,object?>>().Count(v=>v.ContainsKey("identifiedDisplayName"))).FirstOrDefault();
        if(root==null)throw new InvalidDataException("No item table in LUB.");
        string Decode(string s)=>Encoding.GetEncoding(cp).GetString(Encoding.Latin1.GetBytes(s));
        var output=new List<Item>();
        foreach(var kv in root) {
            ct.ThrowIfCancellationRequested();
            if(!int.TryParse(kv.Key,out var id)||kv.Value is not Dictionary<string,object?> v||!v.ContainsKey("identifiedDisplayName"))continue;
            string Get(string key)=>v.TryGetValue(key,out var o)?o as string??"":"";
            string description=v.TryGetValue("identifiedDescriptionName",out var d)&&d is Dictionary<string,object?> lines?string.Join(Environment.NewLine,lines.Values.OfType<string>().Select(Decode)):"";
            output.Add(new Item(id,Decode(Get("identifiedDisplayName")),Get("identifiedResourceName"),description,source,""));
        }
        if(output.Count==0)throw new InvalidDataException("No items decoded.");
        return output.OrderBy(x=>x.Id).ToList();
    }
}
