using System.Drawing.Imaging;
using System.Text;
using System.Text.RegularExpressions;

namespace MavenROStudio;

public sealed record NpcModelInfo(
    string Model,
    string Constant,
    int ClassId,
    string Resource,
    string SprPath,
    string ActPath,
    bool HasSprite,
    bool HasAct,
    bool ServerSupported,
    string Archive,
    string Category
);

public static class NpcModelCatalog
{
    static readonly string[] JobNameCandidates=[
        @"data\luafiles514\lua files\datainfo\jobname.lub",
        @"data\luafiles514\datainfo\jobname.lub",
        @"data\luafiles514\lua files\datainfo\jobname.lua"
    ];

    public static List<NpcModelInfo> Load(ProjectCatalog catalog,CancellationToken ct=default)
    {
        var ids=ParseServerNpcEnum(catalog.ServerRoot,ct);
        var resources=ParseClientJobNames(catalog.Assets,ct);
        var keys=ids.Keys.Concat(resources.Keys).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();
        var result=new List<NpcModelInfo>(keys.Count);
        foreach(var constant in keys)
        {
            ct.ThrowIfCancellationRequested();
            string model=constant.StartsWith("JT_",StringComparison.OrdinalIgnoreCase)?constant[3..]:constant;
            string resource=resources.TryGetValue(constant,out var r)&&!string.IsNullOrWhiteSpace(r)?r:model;
            string spr=@"data\sprite\npc\"+resource+".spr";
            string act=@"data\sprite\npc\"+resource+".act";
            bool hasSpr=Has(catalog.Assets,spr);
            bool hasAct=Has(catalog.Assets,act);
            string archive="";
            if(hasSpr&&catalog.Assets!=null)
            {
                var key=Assets.RawKey(spr);
                if(catalog.Assets.Entries.TryGetValue(key,out var asset))archive=Path.GetFileName(asset.Archive);
            }
            int classId=ids.TryGetValue(constant,out var id)?id:-1;
            result.Add(new NpcModelInfo(
                model,constant,classId,resource,spr,act,hasSpr,hasAct,classId>=0,archive,Category(model,hasSpr)));
        }
        return result;
    }

    static bool Has(Assets? assets,string path)
    {
        if(assets==null)return false;
        try{return assets.Entries.ContainsKey(Assets.RawKey(path));}
        catch{return assets.Entries.ContainsKey(Assets.Key(path));}
    }

    static Dictionary<string,string> ParseClientJobNames(Assets? assets,CancellationToken ct)
    {
        var map=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        if(assets==null)return map;
        foreach(var candidate in JobNameCandidates)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var data=assets.Read(candidate,ct);
                if(data.Bytes.Length>=4&&data.Bytes[0]==0x1b&&data.Bytes[1]==(byte)'L')continue;
                string text=Encoding.GetEncoding(949,EncoderFallback.ReplacementFallback,DecoderFallback.ReplacementFallback).GetString(data.Bytes);
                foreach(Match m in Regex.Matches(text,@"\[\s*jobtbl\.(JT_[A-Za-z0-9_]+)\s*\]\s*=\s*[""']([^""']+)[""']",RegexOptions.IgnoreCase))
                    map[m.Groups[1].Value]=m.Groups[2].Value;
                if(map.Count>0)break;
            }
            catch(OperationCanceledException){throw;}
            catch{}
        }
        return map;
    }

    public static Dictionary<string,int> ParseServerNpcEnum(string serverRoot,CancellationToken ct=default)
    {
        var result=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        string file=Path.Combine(serverRoot??"","src","map","npc.hpp");
        if(!File.Exists(file))return result;
        string text=File.ReadAllText(file);
        int start=text.IndexOf("enum e_job_types",StringComparison.Ordinal);
        if(start<0)return result;
        int open=text.IndexOf('{',start);
        int close=open>=0?text.IndexOf("};",open,StringComparison.Ordinal):-1;
        if(open<0||close<0)return result;
        string body=text[(open+1)..close];
        body=Regex.Replace(body,@"/\*.*?\*/","",RegexOptions.Singleline);
        body=Regex.Replace(body,@"//.*?$","",RegexOptions.Multiline);
        int value=-1;
        foreach(var raw in body.Split(','))
        {
            ct.ThrowIfCancellationRequested();
            string token=raw.Trim();
            if(token.Length==0)continue;
            var m=Regex.Match(token,@"^(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\s*=\s*(?<value>0x[0-9A-Fa-f]+|\d+))?");
            if(!m.Success)continue;
            if(m.Groups["value"].Success)
            {
                string v=m.Groups["value"].Value;
                value=v.StartsWith("0x",StringComparison.OrdinalIgnoreCase)
                    ?Convert.ToInt32(v[2..],16):int.Parse(v);
            }
            else value++;
            string name=m.Groups["name"].Value;
            if(name.StartsWith("JT_",StringComparison.OrdinalIgnoreCase))result[name]=value;
        }
        return result;
    }

    static string Category(string model,bool hasSprite)
    {
        if(model.Contains("KAFRA",StringComparison.OrdinalIgnoreCase))return "Kafra";
        if(model.Contains("WARP",StringComparison.OrdinalIgnoreCase)||model.Contains("TELEPORT",StringComparison.OrdinalIgnoreCase))return "Warp";
        if(model.Contains("_F_",StringComparison.OrdinalIgnoreCase)||Regex.IsMatch(model,@"(?:^|_)F(?:_|$)",RegexOptions.IgnoreCase))return "Female";
        if(model.Contains("_M_",StringComparison.OrdinalIgnoreCase)||Regex.IsMatch(model,@"(?:^|_)M(?:_|$)",RegexOptions.IgnoreCase))return "Male";
        if(model.Contains("HIDDEN",StringComparison.OrdinalIgnoreCase)||model.Contains("EFFECT",StringComparison.OrdinalIgnoreCase))return "Hidden / Effect";
        if(!hasSprite)return "No Sprite";
        return "Other";
    }

    public static Bitmap? Preview(ProjectCatalog catalog,NpcModelInfo model,CancellationToken ct=default)
    {
        if(!model.HasSprite||catalog.Assets==null)return null;
        var data=catalog.Assets.Read(model.SprPath,ct);
        return RagnarokSprPreview.DecodeFirstFrame(data.Bytes);
    }
}

public static class RagnarokSprPreview
{
    public static Bitmap? DecodeFirstFrame(byte[] bytes)
    {
        if(bytes.Length<12||bytes[0]!=(byte)'S'||bytes[1]!=(byte)'P')return null;
        int minor=bytes[2],major=bytes[3];
        int indexed=BitConverter.ToUInt16(bytes,4);
        int rgba=(major>1||(major==1&&minor>=1))&&bytes.Length>=8?BitConverter.ToUInt16(bytes,6):0;
        int p=(major>1||(major==1&&minor>=1))?8:6;

        if(indexed>0)
        {
            if(p+4>bytes.Length)return null;
            int width=BitConverter.ToUInt16(bytes,p);int height=BitConverter.ToUInt16(bytes,p+2);p+=4;
            if(width<=0||height<=0||width>2048||height>2048)return null;
            int dataLength=width*height;
            bool compressed=major>2||(major==2&&minor>=1);
            int end;
            if(compressed)
            {
                if(p+2>bytes.Length)return null;
                int packed=BitConverter.ToUInt16(bytes,p);p+=2;
                if(packed<0||p+packed>bytes.Length)return null;
                end=p+packed;
            }
            else end=Math.Min(bytes.Length-1024,p+dataLength);

            var pixels=new byte[dataLength];
            int o=0;
            while(p<end&&o<pixels.Length)
            {
                byte value=bytes[p++];
                if(compressed&&value==0&&p<end)
                {
                    int count=bytes[p++];
                    for(int i=0;i<count&&o<pixels.Length;i++)pixels[o++]=0;
                }
                else pixels[o++]=value;
            }
            if(o==0)return null;
            int paletteOffset=bytes.Length-1024;
            if(paletteOffset<0)return null;
            var bmp=new Bitmap(width,height,PixelFormat.Format32bppArgb);
            for(int y=0;y<height;y++)
            for(int x=0;x<width;x++)
            {
                int index=pixels[y*width+x];
                if(index==0){bmp.SetPixel(x,y,Color.Transparent);continue;}
                int c=paletteOffset+index*4;
                if(c+2>=bytes.Length){bmp.SetPixel(x,y,Color.Transparent);continue;}
                // Ragnarok SPR palettes are stored B,G,R,unused.
                bmp.SetPixel(x,y,Color.FromArgb(255,bytes[c+2],bytes[c+1],bytes[c]));
            }
            return bmp;
        }

        // Some custom v2.1 SPRs contain only RGBA frames.
        if(rgba>0&&p+4<=bytes.Length)
        {
            int width=BitConverter.ToUInt16(bytes,p);int height=BitConverter.ToUInt16(bytes,p+2);p+=4;
            long need=(long)width*height*4;
            if(width<=0||height<=0||need<=0||p+need>bytes.Length)return null;
            var bmp=new Bitmap(width,height,PixelFormat.Format32bppArgb);
            for(int y=0;y<height;y++)
            for(int x=0;x<width;x++)
            {
                int c=p+(y*width+x)*4;
                bmp.SetPixel(x,y,Color.FromArgb(bytes[c+3],bytes[c+2],bytes[c+1],bytes[c]));
            }
            return bmp;
        }
        return null;
    }
}
