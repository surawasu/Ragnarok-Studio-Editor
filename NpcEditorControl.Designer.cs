#nullable disable
namespace MavenROStudio;

partial class NpcEditorControl
{
    private System.ComponentModel.IContainer components=null;
    private Button btnNpcRefresh,btnNpcLoad,btnNpcCheck,btnNpcSave,btnNpcNew;

    protected override void Dispose(bool disposing){if(disposing)components?.Dispose();base.Dispose(disposing);}

    private void InitializeComponent()
    {
        btnNpcRefresh=new Button();btnNpcLoad=new Button();btnNpcCheck=new Button();btnNpcSave=new Button();btnNpcNew=new Button();
        var top=new FlowLayoutPanel();
        var split=new SplitContainer();
        ((System.ComponentModel.ISupportInitialize)split).BeginInit();split.Panel1.SuspendLayout();split.Panel2.SuspendLayout();split.SuspendLayout();
        SuspendLayout();

        Dock=DockStyle.Fill;BackColor=Color.FromArgb(25,21,36);ForeColor=Color.FromArgb(244,241,250);
        top.Dock=DockStyle.Top;top.Height=42;top.WrapContents=false;top.Padding=new Padding(2,3,0,0);
        search.Width=240;search.PlaceholderText="filter filename";
        ConfigureButton(btnNpcRefresh,"Refresh");ConfigureButton(btnNpcLoad,"Load");ConfigureButton(btnNpcCheck,"Check");ConfigureButton(btnNpcSave,"Save");ConfigureButton(btnNpcNew,"New NPC");
        top.Controls.Add(search);top.Controls.Add(btnNpcRefresh);top.Controls.Add(btnNpcLoad);top.Controls.Add(btnNpcCheck);top.Controls.Add(btnNpcSave);top.Controls.Add(btnNpcNew);

        split.Dock=DockStyle.Fill;split.SplitterDistance=310;
        files.Dock=DockStyle.Fill;files.HorizontalScrollbar=true;
        editor.Dock=DockStyle.Fill;editor.Multiline=true;editor.AcceptsTab=true;editor.ScrollBars=ScrollBars.Both;editor.WordWrap=false;editor.Font=new Font("Cascadia Mono",10F);
        split.Panel1.Controls.Add(files);split.Panel2.Controls.Add(editor);

        status.Dock=DockStyle.Bottom;status.Height=26;status.Text="Designer Ready";status.TextAlign=ContentAlignment.MiddleLeft;
        Controls.Add(split);Controls.Add(top);Controls.Add(status);

        split.Panel1.ResumeLayout(false);split.Panel2.ResumeLayout(false);((System.ComponentModel.ISupportInitialize)split).EndInit();split.ResumeLayout(false);
        ResumeLayout(false);
    }

    static void ConfigureButton(Button b,string text){b.Text=text;b.AutoSize=true;b.Height=30;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;}
}
