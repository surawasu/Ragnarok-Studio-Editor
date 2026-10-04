#nullable disable
namespace MavenROStudio;

partial class DiagnosticsPage
{
    private System.ComponentModel.IContainer components=null;
    internal TextBox LogText;
    protected override void Dispose(bool disposing){if(disposing)components?.Dispose();base.Dispose(disposing);}
    private void InitializeComponent()
    {
        LogText=new TextBox();
        SuspendLayout();
        Dock=DockStyle.Fill;Padding=new Padding(8);BackColor=Color.FromArgb(25,21,36);ForeColor=Color.FromArgb(244,241,250);
        LogText.Dock=DockStyle.Fill;LogText.Multiline=true;LogText.ReadOnly=true;LogText.ScrollBars=ScrollBars.Both;LogText.Font=new Font("Cascadia Mono",9F);
        Controls.Add(LogText);
        ResumeLayout(false);PerformLayout();
    }
}
