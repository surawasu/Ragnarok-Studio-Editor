#nullable disable
namespace MavenROStudio;

partial class YamlEditorControl
{
    private System.ComponentModel.IContainer components=null;
    protected Button btnYamlRefresh,btnYamlLoad,btnYamlValidate,btnYamlSave,btnYamlTemplate;
    protected SplitContainer yamlSplit;

    protected override void Dispose(bool disposing){if(disposing)components?.Dispose();base.Dispose(disposing);}

    private void InitializeComponent()
    {
        Files=new ListBox();Editor=new TextBox();Summary=new ListView();yamlStatus=new Label();yamlTitle=new Label();
        btnYamlRefresh=new Button();btnYamlLoad=new Button();btnYamlValidate=new Button();btnYamlSave=new Button();btnYamlTemplate=new Button();
        yamlSplit=new SplitContainer();
        var top=new FlowLayoutPanel();
        ((System.ComponentModel.ISupportInitialize)yamlSplit).BeginInit();yamlSplit.Panel1.SuspendLayout();yamlSplit.Panel2.SuspendLayout();yamlSplit.SuspendLayout();
        SuspendLayout();

        Dock=DockStyle.Fill;BackColor=Color.FromArgb(25,21,36);ForeColor=Color.FromArgb(244,241,250);

        top.Dock=DockStyle.Top;top.Height=42;top.WrapContents=false;top.Padding=new Padding(2,3,0,0);
        ConfigureButton(btnYamlRefresh,"Refresh");ConfigureButton(btnYamlLoad,"Load");ConfigureButton(btnYamlValidate,"Validate");
        ConfigureButton(btnYamlSave,"Save");ConfigureButton(btnYamlTemplate,"New Template");
        yamlTitle.AutoSize=true;yamlTitle.Padding=new Padding(14,8,0,0);yamlTitle.Font=new Font("Segoe UI",10F,FontStyle.Bold);yamlTitle.Text="YAML Editor";
        top.Controls.Add(btnYamlRefresh);top.Controls.Add(btnYamlLoad);top.Controls.Add(btnYamlValidate);top.Controls.Add(btnYamlSave);top.Controls.Add(btnYamlTemplate);top.Controls.Add(yamlTitle);

        yamlSplit.Dock=DockStyle.Fill;yamlSplit.SplitterDistance=310;
        Files.Dock=DockStyle.Fill;Files.HorizontalScrollbar=true;
        Editor.Dock=DockStyle.Fill;Editor.Multiline=true;Editor.AcceptsTab=true;Editor.ScrollBars=ScrollBars.Both;Editor.WordWrap=false;Editor.Font=new Font("Cascadia Mono",10F);
        Summary.Dock=DockStyle.Bottom;Summary.Height=180;Summary.View=View.Details;Summary.FullRowSelect=true;Summary.HideSelection=false;
        yamlSplit.Panel1.Controls.Add(Files);yamlSplit.Panel2.Controls.Add(Editor);yamlSplit.Panel2.Controls.Add(Summary);

        yamlStatus.Dock=DockStyle.Bottom;yamlStatus.Height=26;yamlStatus.Text="Designer Ready";yamlStatus.TextAlign=ContentAlignment.MiddleLeft;
        Controls.Add(yamlSplit);Controls.Add(top);Controls.Add(yamlStatus);

        yamlSplit.Panel1.ResumeLayout(false);yamlSplit.Panel2.ResumeLayout(false);((System.ComponentModel.ISupportInitialize)yamlSplit).EndInit();yamlSplit.ResumeLayout(false);
        ResumeLayout(false);
    }

    static void ConfigureButton(Button b,string text){b.Text=text;b.AutoSize=true;b.Height=30;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;}
}
