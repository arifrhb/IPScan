using System.Diagnostics;
using System.Net;

namespace IPScan.Services.External;

/// <summary>
/// Safely launches external viewers, browsers, and Windows management tools
/// without passing raw strings into shell command interpreters.
/// </summary>
public static class ExternalToolLauncher
{
    public static void OpenHttp(IPAddress address)
    {
        string url = $"http://{address}";
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    public static void OpenHttps(IPAddress address)
    {
        string url = $"https://{address}";
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    public static void OpenSharedFolders(IPAddress address)
    {
        string uncPath = $@"\\{address}\";
        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = true,
            Arguments = $"\"{uncPath}\""
        };
        Process.Start(startInfo);
    }

    public static bool LaunchVnc(IPAddress address, string? customVncPath, out string? errorMessage)
    {
        errorMessage = null;
        string? vncPath = null;

        if (!string.IsNullOrWhiteSpace(customVncPath))
        {
            if (File.Exists(customVncPath))
            {
                vncPath = customVncPath;
            }
            else
            {
                errorMessage = $"The configured VNC viewer path does not exist:\n'{customVncPath}'\n\nPlease verify your custom VNC executable path in Settings, or clear it to use auto-detection.";
                return false;
            }
        }
        else
        {
            vncPath = VncDetector.DetectVncViewer();
        }

        if (string.IsNullOrWhiteSpace(vncPath) || !File.Exists(vncPath))
        {
            errorMessage = "No installed VNC viewer was detected on this system.\n\n" +
                           "Common viewers checked: TightVNC, RealVNC, UltraVNC, TigerVNC.\n" +
                           "You can configure a custom VNC viewer executable path in Settings.";
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = vncPath,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(address.ToString());

            Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to launch VNC viewer '{Path.GetFileName(vncPath)}': {ex.Message}";
            return false;
        }
    }
}
