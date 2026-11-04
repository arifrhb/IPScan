using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;

namespace IPScan.Services.External;

public record ShortcutResult(bool DesktopCreated, string DesktopPath, bool TaskbarPinned, string? Message = null);

public static class ShortcutService
{
    public const string DefaultShortcutName = "IPScan.lnk";
    public const string DefaultDescription = "IPScan - Standalone Network IP Scanner";

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLinkClass
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [Guid("0dd79870-8042-430d-91f7-0c60301c1259")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPinnedList3
    {
        [PreserveSig] int EnumObjects(out IntPtr ppenum);
        [PreserveSig] int GetPidl(IntPtr pItem, out IntPtr ppidl);
        [PreserveSig] int GetAppIDForObject(IntPtr pItem, [Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszAppID);
        [PreserveSig] int ItemIsPinned(IntPtr pItem);
        [PreserveSig] int GetPinnedItem(IntPtr pItem, out IntPtr ppPinnedItem);
        [PreserveSig] int Modify(IntPtr pUnk1, IntPtr pUnk2, [MarshalAs(UnmanagedType.LPWStr)] string pszPath, int flags);
    }

    [ComImport]
    [Guid("90aa3a4e-1c3b-42be-9950-e816d4e3d169")]
    private class TaskbandPinClass
    {
    }

    /// <summary>
    /// Creates a desktop shortcut pointing to the application.
    /// </summary>
    public static string CreateDesktopShortcut(
        string? customTargetExe = null,
        string? customWorkingDir = null,
        string? customIconLocation = null)
    {
        string? targetExe = customTargetExe;
        if (string.IsNullOrWhiteSpace(targetExe))
        {
            try
            {
                targetExe = Application.ExecutablePath;
            }
            catch
            {
                // Fallback
            }
        }

        if (string.IsNullOrWhiteSpace(targetExe) || !File.Exists(targetExe) || targetExe.EndsWith("testhost.exe", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                targetExe = Process.GetCurrentProcess().MainModule?.FileName;
            }
            catch
            {
                // Fallback
            }
        }

        if (string.IsNullOrWhiteSpace(targetExe) || !File.Exists(targetExe) || targetExe.EndsWith("testhost.exe", StringComparison.OrdinalIgnoreCase))
        {
            string fallback = Path.Combine(AppContext.BaseDirectory, "IPScan.exe");
            if (File.Exists(fallback))
            {
                targetExe = fallback;
            }
            else
            {
                targetExe = Environment.ProcessPath ?? AppDomain.CurrentDomain.FriendlyName;
            }
        }

        if (targetExe != null && targetExe.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            string exeCandidate = Path.ChangeExtension(targetExe, ".exe");
            if (File.Exists(exeCandidate))
            {
                targetExe = exeCandidate;
            }
        }

        string? workingDir = customWorkingDir;
        if (string.IsNullOrWhiteSpace(workingDir))
        {
            try
            {
                workingDir = Application.StartupPath;
            }
            catch
            {
                // Fallback
            }
        }

        if (string.IsNullOrWhiteSpace(workingDir) || !Directory.Exists(workingDir))
        {
            workingDir = Path.GetDirectoryName(targetExe) ?? AppContext.BaseDirectory;
        }

        string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string desktopShortcutPath = Path.Combine(desktopDir, DefaultShortcutName);

        string resolvedTarget = targetExe ?? Path.Combine(AppContext.BaseDirectory, "IPScan.exe");
        string iconLocation = customIconLocation ?? resolvedTarget;
        int iconIndex = 0;
        string looseIco = Path.Combine(workingDir, "app.ico");
        if (!File.Exists(iconLocation) && File.Exists(looseIco))
        {
            iconLocation = looseIco;
            iconIndex = 0;
        }

        CreateShortcutFile(desktopShortcutPath, resolvedTarget, workingDir, DefaultDescription, iconLocation, iconIndex);
        return desktopShortcutPath;
    }

    /// <summary>
    /// Creates a desktop shortcut and attempts to pin the application to the Windows Taskbar.
    /// </summary>
    public static ShortcutResult CreateShortcutAndPinToTaskbar(string? customTargetExe = null)
    {
        try
        {
            string desktopShortcutPath = CreateDesktopShortcut(customTargetExe);
            bool desktopSuccess = File.Exists(desktopShortcutPath);

            string targetExe = customTargetExe ?? Application.ExecutablePath;
            if (string.IsNullOrWhiteSpace(targetExe) || !File.Exists(targetExe))
            {
                targetExe = Path.Combine(AppContext.BaseDirectory, "IPScan.exe");
            }
            string workingDir = Path.GetDirectoryName(targetExe) ?? AppContext.BaseDirectory;

            bool taskbarSuccess = TryPinToTaskbar(desktopShortcutPath, targetExe, workingDir);

            string message = taskbarSuccess
                ? "Desktop shortcut created successfully and pinned to the taskbar!"
                : "Desktop shortcut created successfully! (To pin to taskbar on Windows 11, right-click the Desktop shortcut or the running taskbar icon and click 'Pin to taskbar').";

            return new ShortcutResult(desktopSuccess, desktopShortcutPath, taskbarSuccess, message);
        }
        catch (Exception ex)
        {
            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string desktopShortcutPath = Path.Combine(desktopDir, DefaultShortcutName);
            return new ShortcutResult(false, desktopShortcutPath, false, $"Failed to create desktop shortcut: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes a Windows .lnk shortcut file using COM IShellLinkW or WScript.Shell.
    /// </summary>
    public static void CreateShortcutFile(
        string shortcutPath,
        string targetPath,
        string workingDir,
        string description = DefaultDescription,
        string? iconLocation = null,
        int iconIndex = 0)
    {
        string? dir = Path.GetDirectoryName(shortcutPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string effectiveIconPath = iconLocation ?? targetPath;
        Exception? primaryException = null;

        try
        {
            var link = (IShellLinkW)new ShellLinkClass();
            link.SetPath(targetPath);
            link.SetWorkingDirectory(workingDir);
            link.SetDescription(description);
            link.SetIconLocation(effectiveIconPath, iconIndex);

            var file = (IPersistFile)link;
            file.Save(shortcutPath, true);
            return;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            primaryException = ex;
        }

        try
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = workingDir;
                shortcut.Description = description;
                shortcut.IconLocation = $"{effectiveIconPath},{iconIndex}";
                shortcut.Save();
                return;
            }
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (COMException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (primaryException is COMException) throw primaryException;
            throw new InvalidOperationException("Could not create shortcut using native ShellLink or WScript.Shell COM interfaces.", ex);
        }

        if (primaryException != null)
        {
            if (primaryException is COMException) throw primaryException;
            throw new InvalidOperationException("Could not create shortcut using native ShellLink.", primaryException);
        }

        throw new InvalidOperationException("Could not create shortcut using native ShellLink or WScript.Shell COM interfaces.");
    }

    /// <summary>
    /// Reads properties of an existing .lnk shortcut file.
    /// </summary>
    public static (string TargetPath, string WorkingDirectory, string IconLocation, int IconIndex) ReadShortcutFile(string shortcutPath)
    {
        var link = (IShellLinkW)new ShellLinkClass();
        var file = (IPersistFile)link;
        file.Load(shortcutPath, 0);

        var targetSb = new StringBuilder(260);
        link.GetPath(targetSb, targetSb.Capacity, IntPtr.Zero, 0);

        var dirSb = new StringBuilder(260);
        link.GetWorkingDirectory(dirSb, dirSb.Capacity);

        var iconSb = new StringBuilder(260);
        link.GetIconLocation(iconSb, iconSb.Capacity, out int iconIndex);

        return (targetSb.ToString(), dirSb.ToString(), iconSb.ToString(), iconIndex);
    }

    /// <summary>
    /// Attempts to pin the shortcut or target executable to the Windows taskbar.
    /// </summary>
    public static bool TryPinToTaskbar(string shortcutPath, string targetExe, string workingDir)
    {
        bool success = false;

        // 1. Copy shortcut into User Pinned\TaskBar directory if available
        try
        {
            string taskbarDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");

            if (Directory.Exists(taskbarDir))
            {
                string targetLnk = Path.Combine(taskbarDir, Path.GetFileName(shortcutPath));
                CreateShortcutFile(targetLnk, targetExe, workingDir);
                if (File.Exists(targetLnk))
                {
                    success = true;
                }
            }
        }
        catch
        {
            // Non-critical if user directory permissions differ
        }

        // 2. Try IPinnedList3 COM interface (Windows 10 / 11 Explorer interface)
        try
        {
            var pinner = (IPinnedList3)new TaskbandPinClass();
            int hr = pinner.Modify(IntPtr.Zero, IntPtr.Zero, shortcutPath, 2);
            if (hr == 0)
            {
                success = true;
            }
        }
        catch
        {
            // Handled gracefully
        }

        // 3. Try Shell.Application verb invocation ("taskbarpin" / "Pin to taskbar")
        try
        {
            Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
            if (shellAppType != null)
            {
                dynamic shellApp = Activator.CreateInstance(shellAppType)!;
                string folderPath = Path.GetDirectoryName(shortcutPath)!;
                string fileName = Path.GetFileName(shortcutPath);

                dynamic folder = shellApp.NameSpace(folderPath);
                if (folder != null)
                {
                    dynamic folderItem = folder.ParseName(fileName);
                    if (folderItem != null)
                    {
                        dynamic verbs = folderItem.Verbs();
                        for (int i = 0; i < verbs.Count; i++)
                        {
                            dynamic verb = verbs.Item(i);
                            string name = ((string)verb.Name).Replace("&", "").ToLowerInvariant();
                            if (name.Contains("pin to taskbar") || name.Contains("taskbarpin"))
                            {
                                verb.DoIt();
                                success = true;
                                break;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Handled gracefully
        }

        return success;
    }
}

