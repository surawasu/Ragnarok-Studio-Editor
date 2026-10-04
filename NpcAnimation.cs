using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MavenROStudio;

public sealed record RagnarokActLayer(
    int X,int Y,int SpriteIndex,bool Mirror,Color Tint,
    float ScaleX,float ScaleY,int Rotation,int SpriteType
);

public sealed record RagnarokActFrame(IReadOnlyList<RagnarokActLayer> Layers);

public sealed class RagnarokActAction
{
    public List<RagnarokActFrame> Frames { get; } = [];
    public float Speed { get; internal set; } = 6f;
    public int DelayMs => Math.Clamp((int)Math.Ceiling(Math.Max(0.1f,Speed)*25f),20,5000);
}

public sealed class RagnarokActFile
{
    public int Major { get; private init; }
    public int Minor { get; private init; }
    public List<RagnarokActAction> Actions { get; private init; } = [];
    public string Version => $"{Major}.{Minor}";
    public int ActionCount => Actions.Count;

    public static RagnarokActFile Parse(byte[] bytes)
    {
        if(bytes.Length<16||bytes[0]!=(byte)'A'||bytes[1]!=(byte)'C')
            throw new InvalidDataException("Invalid ACT header.");

        int minor=bytes[2],major=bytes[3];
        try{return ParseCore(bytes,major,minor,true);}
        catch(Exception first) when((major==2&&(minor==3||minor==4)))
        {
            try{return ParseCore(bytes,major,minor,false);}
            catch(Exception second)
            {
                throw new InvalidDataException($"ACT {major}.{minor} parse failed with/without anchors. {first.Message} | {second.Message}",second);
            }
        }
    }

    static RagnarokActFile ParseCore(byte[] bytes,int major,int minor,bool readAnchors)
    {
        int version=major*10+minor;
        var r=new ActReader(bytes);
        r.Position=4;
        int actionCount=r.UInt16();
        if(actionCount<0||actionCount>4096)throw new InvalidDataException("ACT action count out of range.");
        r.Position=16;

        var actions=new List<RagnarokActAction>(actionCount);
        for(int a=0;a<actionCount;a++)
        {
            int frameCount=r.Int32();
            if(frameCount<0||frameCount>20000)throw new InvalidDataException($"ACT frame count out of range at action {a}: {frameCount}");
            var action=new RagnarokActAction();
            for(int f=0;f<frameCount;f++)
            {
                r.Skip(32); // attack/fit ranges
                int layerCount=r.Int32();
                if(layerCount<0||layerCount>4096)throw new InvalidDataException($"ACT layer count out of range at action {a}, frame {f}: {layerCount}");
                var layers=new List<RagnarokActLayer>(layerCount);
                for(int l=0;l<layerCount;l++)
                {
                    int x=version>=26?(int)Math.Floor(r.Float()):r.Int32();
                    int y=version>=26?(int)Math.Floor(r.Float()):r.Int32();
                    int spriteIndex=r.Int32();
                    bool mirror=r.Int32()!=0;
                    Color tint=Color.White;
                    float sx=1f,sy=1f;
                    int rotation=0,spriteType=0;
                    if(version>=20)
                    {
                        byte cr=r.Byte(),cg=r.Byte(),cb=r.Byte(),ca=r.Byte();
                        tint=Color.FromArgb(ca,cr,cg,cb);
                        sx=r.Float();
                        sy=version>=24?r.Float():sx;
                        rotation=r.Int32();
                        spriteType=r.Int32();
                        if(version>=25){r.Int32();r.Int32();} // stored width/height
                    }
                    layers.Add(new RagnarokActLayer(x,y,spriteIndex,mirror,tint,sx,sy,rotation,spriteType));
                }

                if(version>=20)r.Int32(); // sound/event id
                if(readAnchors&&version>=23)
                {
                    int anchors=r.Int32();
                    if(anchors<0||anchors>1024)throw new InvalidDataException("ACT anchor count out of range.");
                    r.Skip(checked(anchors*16));
                }
                action.Frames.Add(new RagnarokActFrame(layers));
            }
            actions.Add(action);
        }

        if(version>=21)
        {
            int sounds=r.Int32();
            if(sounds<0||sounds>65536)throw new InvalidDataException("ACT sound count out of range.");
            r.Skip(checked(sounds*40));
            if(version>=22)
            {
                float previous=6f;
                foreach(var action in actions)
                {
                    if(r.Remaining>=4)previous=r.Float();
                    if(float.IsNaN(previous)||float.IsInfinity(previous)||previous<=0||previous>10000)previous=6f;
                    action.Speed=previous;
                }
            }
        }

        return new RagnarokActFile{Major=major,Minor=minor,Actions=actions};
    }

    sealed class ActReader(byte[] data)
    {
        public int Position { get; set; }
        public int Remaining => data.Length-Position;
        void Need(int count){if(count<0||Position<0||Position>data.Length-count)throw new EndOfStreamException("Unexpected end of ACT.");}
        public void Skip(int count){Need(count);Position+=count;}
        public byte Byte(){Need(1);return data[Position++];}
        public ushort UInt16(){Need(2);ushort v=BitConverter.ToUInt16(data,Position);Position+=2;return v;}
        public int Int32(){Need(4);int v=BitConverter.ToInt32(data,Position);Position+=4;return v;}
        public float Float(){Need(4);float v=BitConverter.ToSingle(data,Position);Position+=4;return v;}
    }
}

public sealed class RagnarokSprSheet : IDisposable
{
    public int Major { get; private init; }
    public int Minor { get; private init; }
    public List<Bitmap> Indexed { get; private init; } = [];
    public List<Bitmap> Rgba { get; private init; } = [];
    public string Version => $"{Major}.{Minor}";

    public Bitmap? GetImage(int spriteType,int index)
    {
        var source=spriteType==0?Indexed:Rgba;
        return index>=0&&index<source.Count?source[index]:null;
    }

    public static RagnarokSprSheet Parse(byte[] bytes)
    {
        if(bytes.Length<8||bytes[0]!=(byte)'S'||bytes[1]!=(byte)'P')
            throw new InvalidDataException("Invalid SPR header.");

        int minor=bytes[2],major=bytes[3],version=major*10+minor;
        int indexed,bgra,p;
        if(version>=32)
        {
            indexed=0;
            bgra=ReadInt(bytes,4);
            p=8;
        }
        else
        {
            indexed=ReadU16(bytes,4);
            bgra=ReadU16(bytes,6);
            p=version>=20?8:6;
        }
        if(indexed<0||indexed>65535||bgra<0||bgra>65535)throw new InvalidDataException("SPR frame count out of range.");

        var indexedRaw=new List<(int W,int H,byte[] Pixels)>(indexed);
        int paletteOffset=indexed>0?bytes.Length-1024:bytes.Length;
        for(int i=0;i<indexed;i++)
        {
            Need(bytes,p,4);int w=ReadU16(bytes,p),h=ReadU16(bytes,p+2);p+=4;
            ValidateSize(w,h);
            int length;
            if(version>=22){Need(bytes,p,4);length=ReadInt(bytes,p);p+=4;}
            else if(version>=21){Need(bytes,p,2);length=ReadU16(bytes,p);p+=2;}
            else length=checked(w*h);
            if(length<0||p>paletteOffset-length)throw new InvalidDataException("SPR indexed frame data out of range.");
            var encoded=bytes.AsSpan(p,length).ToArray();p+=length;
            var pixels=version>=21?DecompressRle(encoded,checked(w*h)):encoded;
            indexedRaw.Add((w,h,pixels));
        }

        var rgba=new List<Bitmap>(bgra);
        for(int i=0;i<bgra;i++)
        {
            Need(bytes,p,4);int w=ReadU16(bytes,p),h=ReadU16(bytes,p+2);p+=4;
            ValidateSize(w,h);
            if(version>=32)throw new NotSupportedException("SPR 3.2 compressed BGRA preview is not supported yet.");
            int length=checked(w*h*4);
            if(p>paletteOffset-length)throw new InvalidDataException("SPR RGBA frame data out of range.");
            rgba.Add(DecodeRgba(bytes,p,w,h));p+=length;
        }

        var indexedFrames=new List<Bitmap>(indexed);
        if(indexed>0)
        {
            if(bytes.Length<1024)throw new InvalidDataException("SPR palette missing.");
            int pal=bytes.Length-1024;
            foreach(var raw in indexedRaw)indexedFrames.Add(DecodeIndexed(raw.W,raw.H,raw.Pixels,bytes,pal));
        }

        return new RagnarokSprSheet{Major=major,Minor=minor,Indexed=indexedFrames,Rgba=rgba};
    }

    public void Dispose()
    {
        foreach(var b in Indexed)b.Dispose();
        foreach(var b in Rgba)b.Dispose();
        Indexed.Clear();Rgba.Clear();
    }

    static Bitmap DecodeIndexed(int w,int h,byte[] pixels,byte[] bytes,int pal)
    {
        var argb=new byte[w*h*4];
        for(int i=0;i<pixels.Length;i++)
        {
            int idx=pixels[i],o=i*4;
            if(idx==0){argb[o+3]=0;continue;}
            int c=pal+idx*4;
            argb[o+0]=bytes[c+0]; // B
            argb[o+1]=bytes[c+1]; // G
            argb[o+2]=bytes[c+2]; // R
            argb[o+3]=255;
        }
        return FromArgbBytes(w,h,argb);
    }

    static Bitmap DecodeRgba(byte[] bytes,int p,int w,int h)
    {
        var argb=new byte[w*h*4];
        for(int y=0;y<h;y++)
        for(int x=0;x<w;x++)
        {
            int src=p+4*((h-y-1)*w+x);
            int dst=4*(y*w+x);
            argb[dst+0]=bytes[src+1];
            argb[dst+1]=bytes[src+2];
            argb[dst+2]=bytes[src+3];
            argb[dst+3]=bytes[src+0];
        }
        return FromArgbBytes(w,h,argb);
    }

    static Bitmap FromArgbBytes(int w,int h,byte[] data)
    {
        var bmp=new Bitmap(w,h,PixelFormat.Format32bppArgb);
        var bd=bmp.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
        try
        {
            if(bd.Stride==w*4)Marshal.Copy(data,0,bd.Scan0,data.Length);
            else
            {
                for(int y=0;y<h;y++)Marshal.Copy(data,y*w*4,IntPtr.Add(bd.Scan0,y*bd.Stride),w*4);
            }
        }
        finally{bmp.UnlockBits(bd);}
        return bmp;
    }

    static byte[] DecompressRle(byte[] encoded,int length)
    {
        var output=new byte[length];int o=0;
        for(int i=0;i<encoded.Length&&o<length;i++)
        {
            byte b=encoded[i];
            if(b==0)
            {
                if(++i>=encoded.Length)break;
                o=Math.Min(length,o+encoded[i]);
            }
            else output[o++]=b;
        }
        return output;
    }

    static void ValidateSize(int w,int h)
    {
        if(w<=0||h<=0||w>4096||h>4096||(long)w*h>16_777_216)throw new InvalidDataException($"SPR frame size out of range: {w}x{h}");
    }
    static void Need(byte[] b,int p,int n){if(p<0||n<0||p>b.Length-n)throw new EndOfStreamException("Unexpected end of SPR.");}
    static int ReadU16(byte[] b,int p){Need(b,p,2);return BitConverter.ToUInt16(b,p);}
    static int ReadInt(byte[] b,int p){Need(b,p,4);return BitConverter.ToInt32(b,p);}
}

public sealed class RagnarokNpcAnimation : IDisposable
{
    public RagnarokSprSheet Spr { get; }
    public RagnarokActFile Act { get; }
    public RagnarokNpcAnimation(RagnarokSprSheet spr,RagnarokActFile act){Spr=spr;Act=act;}

    public Bitmap Render(int actionIndex,int frameIndex,int canvas=512)
    {
        if(actionIndex<0||actionIndex>=Act.Actions.Count)throw new ArgumentOutOfRangeException(nameof(actionIndex));
        var action=Act.Actions[actionIndex];
        if(action.Frames.Count==0)return Empty(canvas);
        frameIndex=Math.Clamp(frameIndex,0,action.Frames.Count-1);
        var frame=action.Frames[frameIndex];

        var result=new Bitmap(canvas,canvas,PixelFormat.Format32bppArgb);
        using var g=Graphics.FromImage(result);
        g.Clear(Color.Transparent);
        g.InterpolationMode=InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode=PixelOffsetMode.Half;
        g.CompositingMode=CompositingMode.SourceOver;
        float originX=canvas/2f,originY=canvas*0.58f;

        foreach(var layer in frame.Layers)
        {
            if(layer.SpriteIndex<0)continue;
            var image=Spr.GetImage(layer.SpriteType,layer.SpriteIndex);
            if(image==null)continue;

            float sx=SafeScale(layer.ScaleX),sy=SafeScale(layer.ScaleY);
            bool mirror=layer.Mirror;
            if(sx<0){sx=-sx;mirror=!mirror;}
            if(sy<0)sy=-sy;

            var state=g.Save();
            try
            {
                g.TranslateTransform(originX+layer.X,originY+layer.Y);
                if(layer.Rotation!=0)g.RotateTransform(layer.Rotation);
                g.ScaleTransform(mirror?-sx:sx,sy);

                using var attr=new ImageAttributes();
                float rr=layer.Tint.R/255f,gg=layer.Tint.G/255f,bb=layer.Tint.B/255f,aa=layer.Tint.A/255f;
                var matrix=new ColorMatrix(new[]{
                    new[]{rr,0f,0f,0f,0f},
                    new[]{0f,gg,0f,0f,0f},
                    new[]{0f,0f,bb,0f,0f},
                    new[]{0f,0f,0f,aa,0f},
                    new[]{0f,0f,0f,0f,1f}
                });
                attr.SetColorMatrix(matrix);
                var dest=new Rectangle(-image.Width/2,-image.Height/2,image.Width,image.Height);
                g.DrawImage(image,dest,0,0,image.Width,image.Height,GraphicsUnit.Pixel,attr);
            }
            finally{g.Restore(state);}
        }
        return result;
    }

    static float SafeScale(float value)=>float.IsFinite(value)&&Math.Abs(value)>0.001f&&Math.Abs(value)<100f?value:1f;
    static Bitmap Empty(int size)=>new(size,size,PixelFormat.Format32bppArgb);
    public void Dispose()=>Spr.Dispose();
}

public sealed record NpcAnimationChoice(string Name,int GroupIndex)
{
    public override string ToString()=>Name;
}

public static class NpcAnimationActions
{
    static readonly string[] Common=["Idle","Walking","Attack","Hit","Dead","Special 1","Special 2","Special 3","Special 4","Special 5"];

    public static List<NpcAnimationChoice> Groups(RagnarokActFile act)
    {
        int count=(act.ActionCount+7)/8;
        var result=new List<NpcAnimationChoice>(count);
        for(int i=0;i<count;i++)
        {
            string name=i<Common.Length?Common[i]:$"Action Group {i}";
            int from=i*8,to=Math.Min(act.ActionCount-1,from+7);
            result.Add(new NpcAnimationChoice($"{name}  [{from}-{to}]",i));
        }
        return result;
    }

    public static int ResolveAction(RagnarokActFile act,int group,int direction)
    {
        if(act.ActionCount==0)return -1;
        int baseIndex=Math.Max(0,group)*8;
        int target=baseIndex+Math.Clamp(direction,0,7);
        if(target<act.ActionCount)return target;
        if(baseIndex<act.ActionCount)return baseIndex;
        return Math.Min(act.ActionCount-1,Math.Clamp(direction,0,7));
    }
}
