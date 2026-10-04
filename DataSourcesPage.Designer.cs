#nullable disable
namespace MavenROStudio;

partial class DataSourcesPage
{
    private System.ComponentModel.IContainer components=null;
    internal ListBox ItemInfoFiles;
    internal ListBox ItemDbFiles;
    internal Button ItemInfoAdd,ItemInfoRemove,ItemInfoUp,ItemInfoDown,ItemInfoDetect,ItemInfoReload;
    internal Button ItemDbAdd,ItemDbRemove,ItemDbUp,ItemDbDown,ItemDbDetect,ItemDbReload;

    protected override void Dispose(bool disposing){if(disposing)components?.Dispose();base.Dispose(disposing);}

    private void InitializeComponent()
    {
        ItemInfoFiles=new ListBox();ItemDbFiles=new ListBox();
        ItemInfoAdd=new Button();ItemInfoRemove=new Button();ItemInfoUp=new Button();ItemInfoDown=new Button();ItemInfoDetect=new Button();ItemInfoReload=new Button();
        ItemDbAdd=new Button();ItemDbRemove=new Button();ItemDbUp=new Button();ItemDbDown=new Button();ItemDbDetect=new Button();ItemDbReload=new Button();
        var layout=new TableLayoutPanel();
        var left=CreateSourcePanel("itemInfo stack • ล่างสุดชนะเมื่อ ID ซ้ำ",ItemInfoFiles,
            new[]{ItemInfoAdd,ItemInfoRemove,ItemInfoUp,ItemInfoDown,ItemInfoDetect,ItemInfoReload},
            new[]{"เพิ่มไฟล์","ลบที่เลือก","↑ ขึ้น","↓ ลง","Auto Detect","Reload"});
        var right=CreateSourcePanel("Item DB stack • ล่างสุดชนะเมื่อ ID ซ้ำ",ItemDbFiles,
            new[]{ItemDbAdd,ItemDbRemove,ItemDbUp,ItemDbDown,ItemDbDetect,ItemDbReload},
            new[]{"เพิ่มไฟล์","ลบที่เลือก","↑ ขึ้น","↓ ลง","Auto Detect","Reload"});
        SuspendLayout();
        Dock=DockStyle.Fill;BackColor=Color.FromArgb(25,21,36);ForeColor=Color.FromArgb(244,241,250);
        layout.Dock=DockStyle.Fill;layout.ColumnCount=2;layout.RowCount=1;layout.Padding=new Padding(0,4,0,0);
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        layout.Controls.Add(left,0,0);layout.Controls.Add(right,1,0);
        Controls.Add(layout);
        ResumeLayout(false);
    }

    static Panel CreateSourcePanel(string title,ListBox box,Button[] buttons,string[] labels)
    {
        var p=new Panel{Dock=DockStyle.Fill,Padding=new Padding(8)};
        var label=new Label{Text=title,Dock=DockStyle.Top,Height=35,Font=new Font("Segoe UI",10F,FontStyle.Bold)};
        var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=82,Padding=new Padding(0,5,0,0),WrapContents=true};
        box.Dock=DockStyle.Fill;box.HorizontalScrollbar=true;
        for(int i=0;i<buttons.Length;i++){buttons[i].Text=labels[i];buttons[i].AutoSize=true;buttons[i].Height=32;buttons[i].FlatStyle=FlatStyle.Flat;buttons[i].FlatAppearance.BorderSize=0;bar.Controls.Add(buttons[i]);}
        p.Controls.Add(box);p.Controls.Add(bar);p.Controls.Add(label);return p;
    }
}
