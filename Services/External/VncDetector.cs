using Microsoft.Win32;

namespace IPScan.Services.External;

/// <summary>
/// Detects existing VNC client installations on Windows across standard paths and the registry.
/// Does not download or bundle external binaries.
/// </summary>
public static class VncDetector
{
    private static readonly string[] ExecutableNames = { "vncviewer.exe", "tvnviewer.exe" };

    /// <summary>
    /// Searches for an installed VNC viewer executable.
    /// Returns the absolute path if found, or null.
    /// </summary>
    public static string? DetectVncViewer(string? userConfiguredPath = null)
    {
        // 1. Check user configured path
        if (!string.IsNullOrWhiteSpace(userConfiguredPath) && File.Exists(userConfiguredPath))
        {
            return userConfiguredPath;
        }

        // 2. Explicit RealVNC Viewer 64-bit and 32-bit locations
        var explicitRealVncDirs = new[]
        {
            @"C:\Program Files\RealVNC\VNC Viewer",
            @"C:\Program Files (x86)\RealVNC\VNC Viewer",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"RealVNC\VNC Viewer"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"RealVNC\VNC Viewer")
        };

        foreach (var dir in explicitRealVncDirs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Directory.Exists(dir))
            {
                string standardExe = Path.Combine(dir, "vncviewer.exe");
                if (File.Exists(standardExe))
                {
                    return standardExe;
                }

                // Check for any viewer executable in the directory (e.g. VNC-Viewer.exe)
                try
                {
                    var exeFiles = Directory.GetFiles(dir, "*.exe");
                    var match = exeFiles.FirstOrDefault(f => Path.GetFileName(f).Contains("vnc", StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        return match;
                    }
                }
                catch
                {
                    // Access check fallback
                }
            }
        }

        // 3. Check Windows Registry App Paths
        string? registryPath = CheckRegistryAppPaths();
        if (!string.IsNullOrEmpty(registryPath) && File.Exists(registryPath))
        {
            return registryPath;
        }

        // 4. Check standard Program Files and application directories
        var programFolders = new List<string?>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        };

        var candidateSubPaths = new[]
        {
            @"RealVNC\VNC Viewer\vncviewer.exe",
            @"RealVNC\vncviewer.exe",
            @"TightVNC\tvnviewer.exe",
            @"TightVNC Software\TightVNC\tvnviewer.exe",
            @"UltraVNC\vncviewer.exe",
            @"uvnc bvba\UltraVNC\vncviewer.exe",
            @"TigerVNC\vncviewer.exe",
            @"Programs\TigerVNC\vncviewer.exe"
        };

        foreach (var folder in programFolders)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) continue;

            foreach (var subPath in candidateSubPaths)
            {
                string fullPath = Path.Combine(folder, subPath);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        // 5. Check system PATH
        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var directories = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (var dir in directories)
            {
                foreach (var exe in ExecutableNames)
                {
                    try
                    {
                        string candidate = Path.Combine(dir.Trim(), exe);
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                    catch
                    {
                        // Ignore invalid PATH entries
                    }
                }
            }
        }

        return null;
    }

    private static string? CheckRegistryAppPaths()
    {
        foreach (var exe in ExecutableNames)
        {
            string keyName = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exe}";
            try
            {
                using var hklmKey = Registry.LocalMachine.OpenSubKey(keyName);
                var val = hklmKey?.GetValue(null) as string;
                if (!string.IsNullOrWhiteSpace(val) && File.Exists(val))
                {
                    return val;
                }

                using var hkcuKey = Registry.CurrentUser.OpenSubKey(keyName);
                val = hkcuKey?.GetValue(null) as string;
                if (!string.IsNullOrWhiteSpace(val) && File.Exists(val))
                {
                    return val;
                }
            }
            catch
            {
                // Registry access security restrictions
            }
        }

        return null;
    }
}

