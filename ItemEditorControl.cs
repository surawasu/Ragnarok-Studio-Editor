namespace MavenROStudio;

public sealed partial class ItemEditorControl : UserControl
{
    public ItemEditorControl()
    {
        InitializeComponent();
        if (!DesignerSupport.IsDesignMode(this))
            RuntimeInitialize(DesignerSupport.EmptyEditorContext());
    }

    public ItemEditorControl(EditorContext context)
    {
        InitializeComponent();
        RuntimeInitialize(context);
    }
}
