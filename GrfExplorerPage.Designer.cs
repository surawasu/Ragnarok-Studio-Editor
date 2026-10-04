#nullable disable
namespace MavenROStudio;

partial class GrfExplorerPage
{
    private System.ComponentModel.IContainer components=null;
    internal ListView AssetsList;
    internal Button ExtractSelected;
    internal Button ProviderChain;
    internal Button TraceToItem;
    internal Label TitleLabel;

    protected override void Dispose(bool disposing){if(disposing)components?.Dispose();base.Dispose(disposing);}

    private void InitializeComponent()
    {
        AssetsList=new ListView();ExtractSelected=new Button();ProviderChain=new Button();TraceToItem=new Button();TitleLabel=new Label();
        var toolbar=new FlowLayoutPanel();
        SuspendLayout();
        Dock=DockStyle.Fill;BackColor=Color.FromArgb(25,21,36);ForeColor=Color.FromArgb(244,241,250);

        TitleLabel.Text="GRF Explorer 2.0 • winning resources + shadow/provider chain";
        TitleLabel.Dock=DockStyle.Top;TitleLabel.Height=34;TitleLabel.Font=new Font("Segoe UI",10F,FontStyle.Bold);TitleLabel.TextAlign=ContentAlignment.MiddleLeft;
        toolbar.Dock=DockStyle.Top;toolbar.Height=42;toolbar.WrapContents=false;
        ConfigureButton(ExtractSelected,"Extract Selected");ConfigureButton(ProviderChain,"Provider Chain");ConfigureButton(TraceToItem,"Trace → Item");
        toolbar.Controls.Add(ExtractSelected);toolbar.Controls.Add(ProviderChain);toolbar.Controls.Add(TraceToItem);

        AssetsList.Dock=DockStyle.Fill;AssetsList.View=View.Details;AssetsList.VirtualMode=true;AssetsList.FullRowSelect=true;AssetsList.HideSelection=false;
        AssetsList.Columns.Add("Resource path",590);AssetsList.Columns.Add("Winning GRF",190);AssetsList.Columns.Add("DATA.ini",70);
        AssetsList.Columns.Add("Providers",75);AssetsList.Columns.Add("Bytes",100);AssetsList.Columns.Add("Flags",60);

        Controls.Add(AssetsList);Controls.Add(toolbar);Controls.Add(TitleLabel);
        ResumeLayout(false);
    }
    static void ConfigureButton(Button b,string text){b.Text=text;b.AutoSize=true;b.Height=32;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;}
}
