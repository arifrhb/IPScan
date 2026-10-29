using System.Reflection;
using System.Runtime.InteropServices;

namespace IPScan.UI;

/// <summary>
/// Provides reliable icon resolution and taskbar registration across standalone single-file builds.
/// </summary>
public static class AppIconHelper
{
    private const int WM_SETICON = 0x0080;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appID);

    private static Icon? _cachedIcon;

    public static void RegisterAppUserModelId()
    {
        try
        {
            SetCurrentProcessExplicitAppUserModelID("IPScan.NetworkScanner.1.2");
        }
        catch
        {
            // Ignore on platforms where shell32 is restricted
        }
    }

    public static Icon? GetAppIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        // 1. Try loading directly from embedded assembly manifest resource (works inside single-file exe)
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("IPScan.app.ico");
            if (stream != null)
            {
                _cachedIcon = new Icon(stream);
                return _cachedIcon;
            }
        }
        catch
        {
            // Fall through to next strategy
        }

        // 2. Try extracting from running executable's native PE icon resource
        try
        {
            string? exePath = Environment.ProcessPath ?? Application.ExecutablePath;
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                _cachedIcon = Icon.ExtractAssociatedIcon(exePath);
                if (_cachedIcon != null) return _cachedIcon;
            }
        }
        catch
        {
            // Fall through
        }

        // 3. Fallback to loose app.ico on disk if present
        try
        {
            string diskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(diskPath))
            {
                _cachedIcon = new Icon(diskPath);
                return _cachedIcon;
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }

    public static void ApplyToForm(Form form)
    {
        var icon = GetAppIcon();
        if (icon == null) return;

        try
        {
            form.Icon = icon;

            // Send explicit Win32 WM_SETICON messages to ensure titlebar and taskbar update immediately
            if (form.IsHandleCreated)
            {
                SendMessage(form.Handle, WM_SETICON, (IntPtr)ICON_SMALL, icon.Handle);
                SendMessage(form.Handle, WM_SETICON, (IntPtr)ICON_BIG, icon.Handle);
            }
            else
            {
                form.HandleCreated += (s, e) =>
                {
                    SendMessage(form.Handle, WM_SETICON, (IntPtr)ICON_SMALL, icon.Handle);
                    SendMessage(form.Handle, WM_SETICON, (IntPtr)ICON_BIG, icon.Handle);
                };
            }
        }
        catch
        {
            // Gracefully ignore Win32 messaging errors
        }
    }
}

