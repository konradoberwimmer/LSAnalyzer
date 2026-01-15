using Microsoft.Win32;

namespace LSAnalyzer.Services;

public class RegistryService : IRegistryService
{
    public string? GetDefaultRLocation()
    {
        return Registry.GetValue(@"HKEY_CURRENT_USER\Software\R-core\R64", "InstallPath", null)?.ToString() ??
               Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\R-core\R64", "InstallPath", null)?.ToString() ?? 
               null;
    }
}