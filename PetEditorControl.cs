namespace MavenROStudio;

public sealed partial class PetEditorControl : YamlEditorControl
{
    public PetEditorControl() : base()
    {
        InitializeComponent();
        if (!DesignerSupport.IsDesignMode(this))
            RuntimeInitialize(DesignerSupport.EmptyEditorContext());
    }

    public PetEditorControl(EditorContext context) : base(context,"Pet Editor")
    {
        InitializeComponent();
        RuntimeInitialize(context);
    }
}
