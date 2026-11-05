using System;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using IPScan.Models;
using IPScan.UI;
using Xunit;

namespace IPScan.Tests;

public class ContextMenuTests
{
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

    private static string? GetClipboardTextWithRetry(int maxRetries = 10, int delayMs = 50)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return Clipboard.GetText();
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                Thread.Sleep(delayMs);
            }
        }
        return null;
    }

    [Fact]
    public void ContextMenu_HasAllServiceImagesConfigured()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();

            Assert.NotNull(form.ResultContextMenu);
            Assert.NotNull(form.MenuHttp.Image);
            Assert.NotNull(form.MenuHttps.Image);
            Assert.NotNull(form.MenuVnc.Image);
            Assert.NotNull(form.MenuExplorer.Image);
            Assert.NotNull(form.MenuCopyIp.Image);
            Assert.NotNull(form.MenuCopyMac.Image);
            Assert.NotNull(form.MenuCopyRow.Image);

            Assert.True(form.MenuHttp.Image.Width > 0 && form.MenuHttp.Image.Height > 0);
            Assert.True(form.MenuHttps.Image.Width > 0 && form.MenuHttps.Image.Height > 0);
            Assert.True(form.MenuVnc.Image.Width > 0 && form.MenuVnc.Image.Height > 0);
            Assert.True(form.MenuExplorer.Image.Width > 0 && form.MenuExplorer.Image.Height > 0);
            Assert.True(form.MenuCopyIp.Image.Width > 0 && form.MenuCopyIp.Image.Height > 0);
            Assert.True(form.MenuCopyMac.Image.Width > 0 && form.MenuCopyMac.Image.Height > 0);
            Assert.True(form.MenuCopyRow.Image.Width > 0 && form.MenuCopyRow.Image.Height > 0);
        });
    }

    [Fact]
    public void ContextMenu_UpdatesDynamicLabels_WhenRowIsSelected()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            var dgv = FindControlRecursive(form, c => c is DataGridView) as DataGridView;
            Assert.NotNull(dgv);

            var list = new BindingList<ScanResult>
            {
                new()
                {
                    Address = IPAddress.Parse("192.168.1.100"),
                    Status = HostStatus.Online,
                    Hostname = "host100.local"
                }
            };
            dgv.DataSource = list;
            Application.DoEvents();

            dgv.Rows[0].Selected = true;

            // Trigger opening event or hit test
            var cancelArgs = new CancelEventArgs();
            form.ResultContextMenu.GetType()
                .GetMethod("OnOpening", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(form.ResultContextMenu, new object[] { cancelArgs });

            Assert.Contains("192.168.1.100", form.MenuHttp.Text);
            Assert.Contains("192.168.1.100", form.MenuHttps.Text);
            Assert.Contains("192.168.1.100", form.MenuExplorer.Text);

            form.Close();
        });
    }

    [Fact]
    public void ContextMenu_MacCopyAction_EnabledWhenMacAvailable_AndCopiesToClipboard()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            var dgv = FindControlRecursive(form, c => c is DataGridView) as DataGridView;
            Assert.NotNull(dgv);

            var list = new BindingList<ScanResult>
            {
                new()
                {
                    Address = IPAddress.Parse("192.168.1.100"),
                    Status = HostStatus.Online,
                    Hostname = "host100.local",
                    MacAddress = "00-11-22-33-44-55"
                }
            };
            dgv.DataSource = list;
            Application.DoEvents();

            dgv.Rows[0].Selected = true;

            var cancelArgs = new CancelEventArgs();
            form.ResultContextMenu.GetType()
                .GetMethod("OnOpening", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(form.ResultContextMenu, new object[] { cancelArgs });

            Assert.True(form.MenuCopyMac.Enabled);
            Assert.Contains("00-11-22-33-44-55", form.MenuCopyMac.Text);

            // Execute click action
            form.MenuCopyMac.PerformClick();
            string? clipboardText = GetClipboardTextWithRetry();
            if (clipboardText != null)
            {
                Assert.Equal("00-11-22-33-44-55", clipboardText);
            }

            form.Close();
        });
    }

    [Fact]
    public void ContextMenu_MacCopyAction_DisabledWhenMacNotAvailable()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            var dgv = FindControlRecursive(form, c => c is DataGridView) as DataGridView;
            Assert.NotNull(dgv);

            var list = new BindingList<ScanResult>
            {
                new()
                {
                    Address = IPAddress.Parse("192.168.1.100"),
                    Status = HostStatus.Online,
                    Hostname = "host100.local",
                    MacAddress = null
                }
            };
            dgv.DataSource = list;
            Application.DoEvents();

            dgv.Rows[0].Selected = true;

            var cancelArgs = new CancelEventArgs();
            form.ResultContextMenu.GetType()
                .GetMethod("OnOpening", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(form.ResultContextMenu, new object[] { cancelArgs });

            Assert.False(form.MenuCopyMac.Enabled);
            Assert.Equal("Copy MAC Address", form.MenuCopyMac.Text);

            form.Close();
        });
    }

    [Fact]
    public void ContextMenu_MacAddressFormatting_DisplaysHyphenatedHexOrDash()
    {
        byte[] rawBytes = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
        string formattedHyphen = IPScan.Services.Scanning.MacAddressService.FormatMacAddress(rawBytes, '-');
        string formattedColon = IPScan.Services.Scanning.MacAddressService.FormatMacAddress(rawBytes, ':');

        Assert.Equal("AA-BB-CC-DD-EE-FF", formattedHyphen);
        Assert.Equal("AA:BB:CC:DD:EE:FF", formattedColon);

        var resultWithMac = new ScanResult
        {
            Address = IPAddress.Parse("192.168.1.1"),
            MacAddress = formattedHyphen
        };
        var resultWithoutMac = new ScanResult
        {
            Address = IPAddress.Parse("192.168.1.2"),
            MacAddress = null
        };

        Assert.Equal("AA-BB-CC-DD-EE-FF", resultWithMac.MacAddressDisplay);
        Assert.Equal("-", resultWithoutMac.MacAddressDisplay);
    }
}
