using System.ComponentModel;
using System.Diagnostics;

namespace MavenROStudio;

[DesignerCategory("Form")]
public sealed partial class Studio : Form
{
    /// <summary>
    /// Parameterless constructor required by the WinForms Designer.
    /// Heavy project initialization is skipped while hosted by Visual Studio designer.
    /// </summary>
    public Studio() : this(false) { }

    internal Studio(bool smoke)
    {
        InitializeComponent();
        if (IsDesignerHosted())
            return;

        RuntimeInitialize(smoke);
    }

    static bool IsDesignerHosted()
    {
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return true;

        try
        {
            string process = Process.GetCurrentProcess().ProcessName;
            if (process.Contains("DesignToolsServer", StringComparison.OrdinalIgnoreCase) ||
                process.Contains("devenv", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        catch { }

        return false;
    }
}
