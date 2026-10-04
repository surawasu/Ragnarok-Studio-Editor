namespace MavenROStudio;

public sealed partial class BarterEditorControl : YamlEditorControl
{
    public BarterEditorControl() : base()
    {
        InitializeComponent();
        if (!DesignerSupport.IsDesignMode(this))
            RuntimeInitialize(DesignerSupport.EmptyEditorContext());
    }

    public BarterEditorControl(EditorContext context) : base(context,"Barter Shop Editor • Visual Craft Layout")
    {
        InitializeComponent();
        RuntimeInitialize(context);
    }
}
