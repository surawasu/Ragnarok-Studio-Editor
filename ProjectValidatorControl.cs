namespace MavenROStudio;

public sealed partial class ProjectValidatorControl : UserControl
{
    public ProjectValidatorControl()
    {
        InitializeComponent();
        if (!DesignerSupport.IsDesignMode(this))
            RuntimeInitialize(DesignerSupport.EmptyEditorContext());
    }

    public ProjectValidatorControl(EditorContext context)
    {
        InitializeComponent();
        RuntimeInitialize(context);
    }
}
