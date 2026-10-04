#nullable disable
namespace MavenROStudio;

partial class PetEditorControl
{
    private System.ComponentModel.IContainer petComponents=null;
    private void InitializeComponent()
    {
        petComponents=new System.ComponentModel.Container();
        Summary.Columns.Clear();
        Summary.Columns.Add("Mob",150);
        Summary.Columns.Add("Egg",150);
        Summary.Columns.Add("Food",150);
        Summary.Columns.Add("Capture",80);
        yamlTitle.Text="Pet Editor";
    }
}
