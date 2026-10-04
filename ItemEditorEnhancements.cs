using System.Text.RegularExpressions;

namespace MavenROStudio;

public sealed record ServerScriptSnippet(
    string Bonus,
    string Command,
    string Syntax,
    string Help,
    string Source
);

public sealed record RagnarokColorChoice(string Name,string Hex,string Help) {
    public override string ToString()=>$"{Name}  ^{Hex}";
}

public static class RagnarokColorHelper {
    public const string ResetHex="000000";
    public static readonly IReadOnlyList<RagnarokColorChoice> BasicColors=[
        new("Default / Black","000000","สีปกติ / ใช้ reset สี"),
        new("White","FFFFFF","สีขาว"),
        new("Red","FF0000","สีแดง • นิยมใช้คำเตือน / จุดสำคัญ"),
        new("Dark Red","CC0000","สีแดงเข้ม"),
        new("Green","00AA00","สีเขียว"),
        new("Bright Green","00FF00","สีเขียวสว่าง"),
        new("Blue","0000FF","สีน้ำเงิน"),
        new("RO Blue","0000CC","สีน้ำเงินมาตรฐานที่พบใน itemInfo เช่น Type/Weight"),
        new("Link Blue","0033CC","น้ำเงินอ่อนที่พบใน itemInfo"),
        new("Cyan","00FFFF","สีฟ้า Cyan"),
        new("Sky Blue","00BFFF","สีฟ้า"),
        new("Yellow","FFFF00","สีเหลือง"),
        new("Gold","FFD700","สีทอง"),
        new("Orange","FF9900","สีส้ม"),
        new("Purple","AA00FF","สีม่วง"),
        new("Pink","FF66CC","สีชมพู"),
        new("Gray","808080","สีเทาที่พบใน itemInfo"),
        new("Light Gray","C0C0C0","สีเทาอ่อน"),
        new("Brown","8B4513","สีน้ำตาล")
    ];

    public static string NormalizeHex(string value) {
        string hex=(value??"").Trim().TrimStart('^','#').ToUpperInvariant();
        if(!Regex.IsMatch(hex,@"^[0-9A-F]{6}$"))throw new ArgumentException("Color ต้องเป็น HEX 6 หลัก เช่น FF0000");
        return hex;
    }

    public static string Code(string hex)=>"^"+NormalizeHex(hex);
    public static string Wrap(string text,string hex,bool reset=true)=>Code(hex)+(text??"")+(reset?Code(ResetHex):"");
}

public static class RagnarokDescriptionPreview {
    static readonly Regex ColorCode=new(@"\^([0-9A-Fa-f]{6})",RegexOptions.Compiled);

    public static void Render(RichTextBox target,string text) {
        target.SuspendLayout();
        try {
            target.Clear();
            target.BackColor=Color.FromArgb(250,248,239);
            target.ForeColor=Color.FromArgb(32,32,32);
            target.Font=new Font("Tahoma",9.5f,FontStyle.Regular);
            target.DetectUrls=false;

            Color current=Color.FromArgb(32,32,32);
            int pos=0;
            foreach(Match m in ColorCode.Matches(text??"")) {
                if(m.Index>pos)Append(target,(text??"").Substring(pos,m.Index-pos),current);
                if(int.TryParse(m.Groups[1].Value,System.Globalization.NumberStyles.HexNumber,null,out int rgb))
                    current=Color.FromArgb((rgb>>16)&255,(rgb>>8)&255,rgb&255);
                pos=m.Index+m.Length;
            }
            if(pos<(text??"").Length)Append(target,(text??"")[pos..],current);
            target.Select(0,0);
        } finally { target.ResumeLayout(); }
    }

    static void Append(RichTextBox target,string value,Color color) {
        if(value.Length==0)return;
        int start=target.TextLength;
        target.AppendText(value.Replace("\r\n","\n").Replace("\r","\n"));
        target.Select(start,target.TextLength-start);
        target.SelectionColor=color;
    }
}

public static class ServerScriptCatalog {
    static readonly Regex ExportBonus=new(@"export_constant2\(\s*""(b[A-Za-z0-9_]+)""\s*,\s*(SP_[A-Za-z0-9_]+)\s*\)",RegexOptions.Compiled);
    static readonly Regex ExactSyntax=new(@"//\s*(bonus[1-5]?\s+b[A-Za-z0-9_]+[^;]*;)",RegexOptions.Compiled|RegexOptions.IgnoreCase);

    public static List<ServerScriptSnippet> Load(string serverRoot) {
        var result=new Dictionary<string,ServerScriptSnippet>(StringComparer.OrdinalIgnoreCase);
        string constants=Path.Combine(serverRoot,"src","map","script_constants.hpp");
        string pc=Path.Combine(serverRoot,"src","map","pc.cpp");
        if(!File.Exists(constants)||!File.Exists(pc))return [];

        string constantText=File.ReadAllText(constants);
        string pcText=File.ReadAllText(pc);
        var bySp=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(Match m in ExportBonus.Matches(constantText))
            bySp[m.Groups[2].Value]=m.Groups[1].Value;

        foreach(var spec in new[]{
            (Name:"bonus",Function:"void pc_bonus(",Args:1),
            (Name:"bonus2",Function:"void pc_bonus2(",Args:2),
            (Name:"bonus3",Function:"void pc_bonus3(",Args:3),
            (Name:"bonus4",Function:"void pc_bonus4(",Args:4),
            (Name:"bonus5",Function:"void pc_bonus5(",Args:5)
        }) {
            int start=pcText.IndexOf(spec.Function,StringComparison.Ordinal);
            if(start<0)continue;
            int next=pcText.IndexOf("\nvoid pc_bonus",start+spec.Function.Length,StringComparison.Ordinal);
            if(next<0)next=pcText.Length;
            string block=pcText[start..next];

            var exact=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(Match m in ExactSyntax.Matches(block)) {
                string syntax=m.Groups[1].Value.Trim();
                var bn=Regex.Match(syntax,@"\bb[A-Za-z0-9_]+\b");
                if(bn.Success)exact[bn.Value]=syntax;
            }

            foreach(Match m in Regex.Matches(block,@"\bcase\s+(SP_[A-Za-z0-9_]+)\s*:")) {
                string sp=m.Groups[1].Value;
                if(!bySp.TryGetValue(sp,out string? bonus))continue;
                string syntax=exact.TryGetValue(bonus,out var x)?x:GenericSyntax(spec.Name,bonus,spec.Args);
                string help=HelpFor(syntax,bonus,spec.Name);
                result[spec.Name+"|"+bonus]=new(bonus,spec.Name,syntax,help,$"pc.cpp • {sp}");
            }
        }

        foreach(Match m in ExactSyntax.Matches(pcText)) {
            string syntax=m.Groups[1].Value.Trim();
            var cm=Regex.Match(syntax,@"^(bonus[1-5]?)\s+(b[A-Za-z0-9_]+)",RegexOptions.IgnoreCase);
            if(!cm.Success)continue;
            string command=cm.Groups[1].Value;
            string bonus=cm.Groups[2].Value;
            result.TryAdd(command+"|"+bonus,new(bonus,command,syntax,HelpFor(syntax,bonus,command),"pc.cpp comment"));
        }

        return result.Values.OrderBy(x=>x.Command).ThenBy(x=>x.Bonus,StringComparer.OrdinalIgnoreCase).ToList();
    }

    static string GenericSyntax(string command,string bonus,int args) {
        var values=Enumerable.Range(1,args).Select(i=>i==args?"value":$"arg{i}");
        return $"{command} {bonus},{string.Join(",",values)};";
    }

    static string HelpFor(string syntax,string bonus,string command) {
        var notes=new List<string>{$"Server-supported {command} bonus: {bonus}",$"Syntax: {syntax}"};
        string meaning=bonus switch {
            "bAllStats"=>"เพิ่ม STR/AGI/VIT/INT/DEX/LUK พร้อมกัน",
            "bMaxHP"=>"เพิ่ม Max HP แบบค่าคงที่",
            "bMaxHPrate"=>"เพิ่ม Max HP เป็นเปอร์เซ็นต์",
            "bAddRace"=>"เพิ่ม Physical Damage ต่อ Race ที่กำหนด",
            "bMagicAddRace"=>"เพิ่ม Magical Damage ต่อ Race ที่กำหนด",
            "bSubRace"=>"ลด Damage ที่ได้รับจาก Race ที่กำหนด",
            "bDropAddRace"=>"เพิ่มอัตรา Drop จาก Race ที่กำหนด",
            _ when bonus.StartsWith("bAdd",StringComparison.OrdinalIgnoreCase)=>"โบนัสเพิ่มค่า/ความเสียหายตามเงื่อนไขของชื่อ bonus",
            _ when bonus.StartsWith("bSub",StringComparison.OrdinalIgnoreCase)=>"โบนัสลดค่า/ความเสียหายตามเงื่อนไขของชื่อ bonus",
            _ when bonus.Contains("Rate",StringComparison.OrdinalIgnoreCase)=>"ค่าประเภทอัตรา/เปอร์เซ็นต์ โปรดตรวจหน่วยจาก source",
            _=>"ดู Syntax และ source case ด้านล่างเพื่อยืนยันความหมาย/หน่วย"
        };
        notes.Add("Meaning: "+meaning);
        if(syntax.Contains(",r,",StringComparison.OrdinalIgnoreCase))notes.Add("r = race constant เช่น RC_All, RC_DemiHuman, RC_Player_Human");
        if(syntax.Contains(",e,",StringComparison.OrdinalIgnoreCase))notes.Add("e = element constant");
        if(syntax.Contains(",s,",StringComparison.OrdinalIgnoreCase))notes.Add("s = size constant");
        if(syntax.Contains(",sk,",StringComparison.OrdinalIgnoreCase))notes.Add("sk = skill ID / skill constant");
        if(syntax.Contains(",iid,",StringComparison.OrdinalIgnoreCase))notes.Add("iid = Item ID");
        if(syntax.Contains(",mid,",StringComparison.OrdinalIgnoreCase))notes.Add("mid = Monster ID");
        notes.Add("ค่าจริงและช่วงที่รองรับขึ้นกับ case ใน pc.cpp ของ Server ปัจจุบัน");
        return string.Join("\r\n",notes);
    }
}
