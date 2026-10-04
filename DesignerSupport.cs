using System.ComponentModel;
using System.Diagnostics;

namespace MavenROStudio;

public static class DesignerSupport
{
    public static bool IsDesignMode(Control? control=null)
    {
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return true;

        if (control?.Site?.DesignMode == true)
            return true;

        try
        {
            string process=Process.GetCurrentProcess().ProcessName;
            return process.Contains("DesignToolsServer",StringComparison.OrdinalIgnoreCase)
                || process.Contains("devenv",StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    public static EditorContext EmptyEditorContext()=>new()
    {
        Settings=new Settings(),
        Catalog=new ProjectCatalog(),
        ReloadClient=()=>Task.CompletedTask,
        ReloadServer=()=>Task.CompletedTask,
        RegisterClientSource=_=>{},
        RegisterServerSource=_=>{},
        ClientSourceChain=_=>Array.Empty<string>(),
        ServerSourceChain=_=>Array.Empty<string>(),
        Log=_=>{}
    };
}
