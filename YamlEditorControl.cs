namespace MavenROStudio;

public partial class YamlEditorControl : UserControl
{
    public YamlEditorControl()
    {
        InitializeComponent();
        if (!DesignerSupport.IsDesignMode(this))
            RuntimeInitialize(DesignerSupport.EmptyEditorContext(),"YAML Editor");
    }

    protected YamlEditorControl(EditorContext ctx,string title)
    {
        InitializeComponent();
        RuntimeInitialize(ctx,title);
    }
}
