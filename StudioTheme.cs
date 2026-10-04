namespace MavenROStudio;

public static class StudioTheme {
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
    public static readonly Color Background=Color.FromArgb(14,12,20);
    public static readonly Color Sidebar=Color.FromArgb(18,15,27);
    public static readonly Color Surface=Color.FromArgb(25,21,36);
    public static readonly Color Surface2=Color.FromArgb(34,28,48);
    public static readonly Color Surface3=Color.FromArgb(45,37,62);
    public static readonly Color Input=Color.FromArgb(22,19,31);
    public static readonly Color Border=Color.FromArgb(66,54,86);
    public static readonly Color Purple=Color.FromArgb(139,92,246);
    public static readonly Color PurpleHover=Color.FromArgb(157,112,255);
    public static readonly Color PurplePressed=Color.FromArgb(112,69,214);
    public static readonly Color PurpleSoft=Color.FromArgb(55,39,82);
    public static readonly Color Text=Color.FromArgb(244,241,250);
    public static readonly Color Muted=Color.FromArgb(170,161,187);
    public static readonly Color Success=Color.FromArgb(74,222,128);
    public static readonly Color Warning=Color.FromArgb(250,204,21);
    public static readonly Color Danger=Color.FromArgb(248,113,113);
    public static readonly Color Info=Color.FromArgb(96,165,250);

    public static Font UiFont(float size=9.5f,FontStyle style=FontStyle.Regular)=>new("Segoe UI",size,style);
    public static Font MonoFont(float size=9.5f)=>new("Cascadia Mono",size,FontStyle.Regular);

    public static Button FlatButton(string text,bool primary=false) {
        var b=new Button {
            Text=text,
            AutoSize=true,
            Height=34,
            FlatStyle=FlatStyle.Flat,
            BackColor=primary?Purple:Surface2,
            ForeColor=Text,
            Font=UiFont(9.25f,primary?FontStyle.Bold:FontStyle.Regular),
            Cursor=Cursors.Hand,
            Padding=new Padding(10,0,10,0),
            Margin=new Padding(4,3,4,3),
            UseVisualStyleBackColor=false
        };
        b.FlatAppearance.BorderSize=0;
        b.FlatAppearance.MouseOverBackColor=primary?PurpleHover:Surface3;
        b.FlatAppearance.MouseDownBackColor=primary?PurplePressed:PurpleSoft;
        return b;
    }

    public static Label SectionTitle(string text)=>new(){
        Text=text,
        AutoSize=true,
        ForeColor=Text,
        Font=UiFont(11.5f,FontStyle.Bold),
        Margin=new Padding(0,4,0,8)
    };

    public static Label Badge(string text,Color color)=>new(){
        Text="  "+text+"  ",
        AutoSize=true,
        BackColor=Color.FromArgb(42,color),
        ForeColor=color,
        Font=UiFont(8.5f,FontStyle.Bold),
        Padding=new Padding(6,4,6,4),
        Margin=new Padding(5,4,5,4)
    };

    public static void Apply(Form form) {
        form.BackColor=Background;
        form.ForeColor=Text;
        form.Font=UiFont();
        EnableDarkTitleBar(form);
        ApplyRecursive(form);
    }

    static void EnableDarkTitleBar(Form form) {
        void ApplyNow() {
            try{
                int enabled=1;
                int result=DwmSetWindowAttribute(form.Handle,20,ref enabled,sizeof(int));
                if(result!=0)DwmSetWindowAttribute(form.Handle,19,ref enabled,sizeof(int));
            }catch{}
        }
        if(form.IsHandleCreated)ApplyNow();
        form.HandleCreated+=(_,_)=>ApplyNow();
    }

    public static void ApplyRecursive(Control root) {
        foreach(Control c in root.Controls) {
            ApplyControl(c);
            if(c.HasChildren)ApplyRecursive(c);
        }
    }

    public static void ApplyControl(Control c) {
        if(c.Tag is string tag&&tag.Equals("theme:preserve",StringComparison.OrdinalIgnoreCase))return;

        switch(c) {
            case Button b:
                StyleButton(b);
                break;
            case TextBox tb:
                tb.BackColor=Input;tb.ForeColor=Text;tb.BorderStyle=BorderStyle.FixedSingle;
                tb.Font=tb.Font.Name.Contains("Consolas",StringComparison.OrdinalIgnoreCase)?MonoFont(tb.Font.Size):UiFont(tb.Font.Size);
                break;
            case RichTextBox rb:
                rb.BackColor=Input;rb.ForeColor=Text;rb.BorderStyle=BorderStyle.FixedSingle;
                if(rb.Font.Name.Contains("Consolas",StringComparison.OrdinalIgnoreCase))rb.Font=MonoFont(rb.Font.Size);
                break;
            case ComboBox cb:
                cb.BackColor=Input;cb.ForeColor=Text;cb.FlatStyle=FlatStyle.Flat;
                break;
            case ListBox lb:
                lb.BackColor=Input;lb.ForeColor=Text;lb.BorderStyle=BorderStyle.FixedSingle;
                break;
            case ListView lv:
                StyleListView(lv);
                break;
            case NumericUpDown nud:
                nud.BackColor=Input;nud.ForeColor=Text;nud.BorderStyle=BorderStyle.FixedSingle;
                break;
            case CheckBox ck:
                ck.ForeColor=Text;ck.BackColor=Color.Transparent;
                break;
            case RadioButton rb2:
                rb2.ForeColor=Text;rb2.BackColor=Color.Transparent;
                break;
            case Label label:
                if(label.BackColor==Color.Transparent||label.BackColor==SystemColors.Control)label.BackColor=Color.Transparent;
                if(label.ForeColor==SystemColors.ControlText||label.ForeColor==Color.Black)label.ForeColor=Muted;
                break;
            case GroupBox gb:
                gb.ForeColor=Text;gb.BackColor=Surface;
                break;
            case TabControl tabs:
                StyleTabs(tabs);
                break;
            case SplitContainer split:
                split.BackColor=Border;split.SplitterWidth=2;
                split.Panel1.BackColor=Surface;split.Panel2.BackColor=Surface;
                break;
            case FlowLayoutPanel flow:
                if(flow.BackColor==SystemColors.Control||flow.BackColor==Color.White||flow.BackColor==Color.FromArgb(247,247,250))flow.BackColor=Surface;
                break;
            case TableLayoutPanel table:
                if(table.BackColor==SystemColors.Control||table.BackColor==Color.White) table.BackColor=Surface;
                break;
            case Panel panel:
                if(panel.BackColor==SystemColors.Control||panel.BackColor==Color.White||panel.BackColor==Color.FromArgb(246,246,249)||panel.BackColor==Color.FromArgb(242,242,246)||panel.BackColor==Color.FromArgb(247,247,250))
                    panel.BackColor=Surface;
                break;
            case UserControl userControl:
                userControl.BackColor=Surface;
                userControl.ForeColor=Text;
                break;
        }
    }

    public static void StyleButton(Button b,bool primary=false) {
        bool saveAction=b.Text.StartsWith("Save",StringComparison.OrdinalIgnoreCase)||b.Text.Equals("Reload Project",StringComparison.OrdinalIgnoreCase);
        bool deleteAction=b.Text.Contains("Delete",StringComparison.OrdinalIgnoreCase)||b.Text.Contains("ลบ",StringComparison.OrdinalIgnoreCase);
        bool accent=primary||saveAction;

        b.FlatStyle=FlatStyle.Flat;
        b.FlatAppearance.BorderSize=0;
        b.UseVisualStyleBackColor=false;
        b.Cursor=Cursors.Hand;
        b.ForeColor=deleteAction?Danger:Text;
        if(accent||b.BackColor==SystemColors.Control||b.BackColor==Color.White)b.BackColor=accent?Purple:Surface2;
        b.FlatAppearance.MouseOverBackColor=deleteAction?Color.FromArgb(68,35,47):accent?PurpleHover:Surface3;
        b.FlatAppearance.MouseDownBackColor=deleteAction?Color.FromArgb(82,39,52):accent?PurplePressed:PurpleSoft;
        if(b.Height<32)b.Height=32;
        if(b.Padding==Padding.Empty)b.Padding=new Padding(8,0,8,0);
        b.Font=UiFont(b.Font.Size,accent?FontStyle.Bold:FontStyle.Regular);
    }

    public static void StylePrimary(Button b)=>StyleButton(b,true);

    public static void StyleListView(ListView lv) {
        lv.BackColor=Input;
        lv.ForeColor=Text;
        lv.BorderStyle=BorderStyle.None;
        lv.GridLines=false;
        lv.FullRowSelect=true;
        lv.OwnerDraw=true;
        lv.Font=UiFont(9.15f);
        lv.DrawColumnHeader-=DrawListHeader;
        lv.DrawColumnHeader+=DrawListHeader;
        lv.DrawItem-=DrawListItem;
        lv.DrawItem+=DrawListItem;
        lv.DrawSubItem-=DrawListSubItem;
        lv.DrawSubItem+=DrawListSubItem;
    }

    static void DrawListHeader(object? sender,DrawListViewColumnHeaderEventArgs e) {
        using var bg=new SolidBrush(Surface2);
        e.Graphics.FillRectangle(bg,e.Bounds);
        var textRect=new Rectangle(e.Bounds.X+8,e.Bounds.Y,e.Bounds.Width-12,e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics,e.Header?.Text??"",UiFont(8.75f,FontStyle.Bold),textRect,Muted,
            TextFormatFlags.VerticalCenter|TextFormatFlags.Left|TextFormatFlags.EndEllipsis);
    }

    static void DrawListItem(object? sender,DrawListViewItemEventArgs e) {
        if(sender is ListView lv&&lv.View!=View.Details)e.DrawDefault=true;
    }

    static void DrawListSubItem(object? sender,DrawListViewSubItemEventArgs e) {
        bool selected=e.Item?.Selected==true;
        string value=e.SubItem?.Text??"";
        Color bg=selected?PurpleSoft:(e.ItemIndex%2==0?Input:Surface);
        Color fg=selected?Color.White:value switch{
            "MATCH"=>Success,
            "MISMATCH"=>Danger,
            "CLIENT ONLY"=>Warning,
            "SERVER ONLY"=>Info,
            "MISSING"=>Danger,
            _=>Text
        };
        using var brush=new SolidBrush(bg);
        e.Graphics.FillRectangle(brush,e.Bounds);
        var textRect=new Rectangle(e.Bounds.X+8,e.Bounds.Y,e.Bounds.Width-12,e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics,value,UiFont(9.1f,value is "MATCH" or "MISMATCH" or "CLIENT ONLY" or "SERVER ONLY"?FontStyle.Bold:FontStyle.Regular),textRect,fg,
            TextFormatFlags.VerticalCenter|TextFormatFlags.Left|TextFormatFlags.EndEllipsis);
    }

    public static void StyleTabs(TabControl tabs) {
        tabs.DrawMode=TabDrawMode.OwnerDrawFixed;
        tabs.SizeMode=TabSizeMode.Fixed;
        tabs.ItemSize=new Size(Math.Max(110,tabs.ItemSize.Width),34);
        tabs.Padding=new Point(14,4);
        tabs.BackColor=Surface;
        tabs.ForeColor=Text;
        foreach(TabPage page in tabs.TabPages){page.BackColor=Surface;page.ForeColor=Text;}
        tabs.DrawItem-=DrawTab;
        tabs.DrawItem+=DrawTab;
    }

    static void DrawTab(object? sender,DrawItemEventArgs e) {
        if(sender is not TabControl tabs||e.Index<0||e.Index>=tabs.TabPages.Count)return;
        var rect=e.Bounds;
        bool selected=e.Index==tabs.SelectedIndex;
        using var bg=new SolidBrush(selected?Purple:Surface2);
        using var fg=new SolidBrush(selected?Color.White:Muted);
        e.Graphics.FillRectangle(bg,rect);
        string text=tabs.TabPages[e.Index].Text;
        var size=e.Graphics.MeasureString(text,UiFont(9f,selected?FontStyle.Bold:FontStyle.Regular));
        e.Graphics.DrawString(text,UiFont(9f,selected?FontStyle.Bold:FontStyle.Regular),fg,
            rect.Left+(rect.Width-size.Width)/2,rect.Top+(rect.Height-size.Height)/2);
    }

    public static void StyleStatus(Label label,string status) {
        label.ForeColor=status switch {
            "MATCH"=>Success,
            "MISMATCH"=>Danger,
            "CLIENT ONLY"=>Warning,
            "SERVER ONLY"=>Info,
            _=>Muted
        };
        label.Font=UiFont(label.Font.Size,FontStyle.Bold);
    }
}
