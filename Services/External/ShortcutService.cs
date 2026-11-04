using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

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
    /// Creates a desktop shortcut and attempts to pin the application to the Windows Taskbar.
    /// </summary>
    public static ShortcutResult CreateShortcutAndPinToTaskbar(string? customTargetExe = null)
    {
        string targetExe = customTargetExe ?? Environment.ProcessPath ?? AppDomain.CurrentDomain.FriendlyName;
        if (string.IsNullOrWhiteSpace(targetExe) || !File.Exists(targetExe))
        {
            // In unit test or development scenarios, fallback to base directory exe
            string fallback = Path.Combine(AppContext.BaseDirectory, "IPScan.exe");
            if (File.Exists(fallback))
            {
                targetExe = fallback;
            }
        }

        string workingDir = Path.GetDirectoryName(targetExe) ?? AppContext.BaseDirectory;
        string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string desktopShortcutPath = Path.Combine(desktopDir, DefaultShortcutName);

        bool desktopSuccess = false;
        try
        {
            CreateShortcutFile(desktopShortcutPath, targetExe, workingDir, DefaultDescription);
            desktopSuccess = File.Exists(desktopShortcutPath);
        }
        catch (Exception ex)
        {
            return new ShortcutResult(false, desktopShortcutPath, false, $"Failed to create desktop shortcut: {ex.Message}");
        }

        bool taskbarSuccess = TryPinToTaskbar(desktopShortcutPath, targetExe, workingDir);

        string message = taskbarSuccess
            ? "Desktop shortcut created successfully and pinned to the taskbar!"
            : "Desktop shortcut created successfully! (To pin to taskbar on Windows 11, right-click the Desktop shortcut or the running taskbar icon and click 'Pin to taskbar').";

        return new ShortcutResult(desktopSuccess, desktopShortcutPath, taskbarSuccess, message);
    }

    /// <summary>
    /// Writes a Windows .lnk shortcut file using COM IShellLinkW or WScript.Shell.
    /// </summary>
    public static void CreateShortcutFile(string shortcutPath, string targetPath, string workingDir, string description = DefaultDescription)
    {
        string? dir = Path.GetDirectoryName(shortcutPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        try
        {
            var link = (IShellLinkW)new ShellLinkClass();
            link.SetPath(targetPath);
            link.SetWorkingDirectory(workingDir);
            link.SetDescription(description);
            link.SetIconLocation(targetPath, 0);

            var file = (IPersistFile)link;
            file.Save(shortcutPath, true);
            return;
        }
        catch
        {
            // Fallback to WScript.Shell
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
                shortcut.IconLocation = targetPath + ",0";
                shortcut.Save();
                return;
            }
        }
        catch
        {
            // Both methods failed, rethrow
        }

        throw new InvalidOperationException("Could not create shortcut using native ShellLink or WScript.Shell COM interfaces.");
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

