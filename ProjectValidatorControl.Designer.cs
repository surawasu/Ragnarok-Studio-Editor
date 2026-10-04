#nullable disable
namespace MavenROStudio;

partial class ProjectValidatorControl
{
    private System.ComponentModel.IContainer components=null;
    private Button btnValidatorScan,btnValidatorExport;

    protected override void Dispose(bool disposing){if(disposing)components?.Dispose();base.Dispose(disposing);}

    private void InitializeComponent()
    {
        btnValidatorScan=new Button();btnValidatorExport=new Button();
        var top=new FlowLayoutPanel();
        SuspendLayout();

        Dock=DockStyle.Fill;BackColor=Color.FromArgb(25,21,36);ForeColor=Color.FromArgb(244,241,250);
        top.Dock=DockStyle.Top;top.Height=42;top.Padding=new Padding(2,3,0,0);
        ConfigureButton(btnValidatorScan,"Scan Project");ConfigureButton(btnValidatorExport,"Export Report");
        top.Controls.Add(btnValidatorScan);top.Controls.Add(btnValidatorExport);

        report.Dock=DockStyle.Fill;report.Multiline=true;report.ReadOnly=true;report.ScrollBars=ScrollBars.Both;report.WordWrap=false;report.Font=new Font("Cascadia Mono",10F);
        status.Dock=DockStyle.Bottom;status.Height=26;status.Text="Designer Ready";status.TextAlign=ContentAlignment.MiddleLeft;

        Controls.Add(report);Controls.Add(top);Controls.Add(status);
        ResumeLayout(false);
    }

    static void ConfigureButton(Button b,string text){b.Text=text;b.AutoSize=true;b.Height=30;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;}
}
