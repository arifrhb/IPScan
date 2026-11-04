using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using IPScan.Services.External;
using IPScan.UI;
using Xunit;

namespace IPScan.Tests;

public class ShortcutServiceTests
{
    private static Control? FindControlRecursive(Control parent, Func<Control, bool> predicate)
    {
        if (predicate(parent)) return parent;
        foreach (Control child in parent.Controls)
        {
            var found = FindControlRecursive(child, predicate);
            if (found != null) return found;
        }
        return null;
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadEx != null)
        {
            throw new Exception("STA thread failed", threadEx);
        }
    }

    [Fact]
    public void MainForm_Contains_Shortcut_Button()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            var btnShortcut = FindControlRecursive(form, c => c is Button && c.Text == "Shortcut") as Button;
            Assert.NotNull(btnShortcut);
            Assert.Equal("Shortcut", btnShortcut.Text);
            Assert.True(btnShortcut.Visible);

            form.Close();
        });
    }

    [Fact]
    public void CreateShortcutFile_Generates_Valid_LnkFile()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "IPScan_ShortcutTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string tempLnk = Path.Combine(tempDir, "TestApp.lnk");
        string tempTarget = Path.Combine(tempDir, "FakeApp.exe");
        File.WriteAllText(tempTarget, "dummy");

        try
        {
            ShortcutService.CreateShortcutFile(tempLnk, tempTarget, tempDir, "Test description");
            Assert.True(File.Exists(tempLnk), "Shortcut .lnk file should be created successfully.");
            var fi = new FileInfo(tempLnk);
            Assert.True(fi.Length > 0, "Created .lnk file should not be 0 bytes.");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void CreateShortcutAndPinToTaskbar_ExecutesSafely()
    {
        var result = ShortcutService.CreateShortcutAndPinToTaskbar();
        // Desktop shortcut should be created, or in test environment fail gracefully without exception
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Message));
        if (result.DesktopCreated)
        {
            Assert.True(File.Exists(result.DesktopPath));
        }
    }
}
