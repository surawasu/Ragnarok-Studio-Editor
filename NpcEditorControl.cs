namespace MavenROStudio;

public sealed partial class NpcEditorControl : UserControl
{
    public NpcEditorControl()
    {
        InitializeComponent();
        if (!DesignerSupport.IsDesignMode(this))
            RuntimeInitialize(DesignerSupport.EmptyEditorContext());
    }

    public NpcEditorControl(EditorContext context)
    {
        InitializeComponent();
        RuntimeInitialize(context);
    }
}
