using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using IPScan.Models;
using IPScan.UI;
using Xunit;

namespace IPScan.Tests;

public class SettingsFormTests
{
    private static void RunOnStaThread(System.Action action)
    {
        System.Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (System.Exception ex)
            {
                threadEx = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadEx != null)
        {
            throw new System.Exception("STA thread failed", threadEx);
        }
    }

    [Fact]
    public void SettingsForm_VncControlsLayout_IsConfiguredProperly()
    {
        RunOnStaThread(() =>
        {
            var options = new ScanOptions();
            using var form = new SettingsForm(options);
            form.Show(); // Triggers OnLoad to position btnDetectVnc relative to lblVnc

            var lblVncField = typeof(SettingsForm).GetField("lblVnc", BindingFlags.NonPublic | BindingFlags.Instance);
            var btnDetectField = typeof(SettingsForm).GetField("btnDetectVnc", BindingFlags.NonPublic | BindingFlags.Instance);
            var txtVncField = typeof(SettingsForm).GetField("txtVncPath", BindingFlags.NonPublic | BindingFlags.Instance);
            var btnBrowseField = typeof(SettingsForm).GetField("btnBrowseVnc", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(lblVncField);
            Assert.NotNull(btnDetectField);
            Assert.NotNull(txtVncField);
            Assert.NotNull(btnBrowseField);

            var lblVnc = (Label)lblVncField.GetValue(form)!;
            var btnDetect = (Button)btnDetectField.GetValue(form)!;
            var txtVnc = (TextBox)txtVncField.GetValue(form)!;
            var btnBrowse = (Button)btnBrowseField.GetValue(form)!;

            // Label text check
            Assert.Contains("VNC Viewer Executable path (Auto-detect)", lblVnc.Text);

            // Button text checks
            Assert.Equal("Auto-detect", btnDetect.Text);
            Assert.Equal("Browse...", btnBrowse.Text);

            // Auto-detect button should be wide enough (>= 90px)
            Assert.True(btnDetect.Width >= 90, $"btnDetect.Width was {btnDetect.Width}, expected >= 90");

            // Auto-detect button is placed after the label (horizontally)
            Assert.True(btnDetect.Left >= lblVnc.Left, "btnDetect should be placed after the label");

            // TextBox and Browse button should be on a lower row than the label and auto-detect button
            Assert.True(txtVnc.Top > btnDetect.Top, "TextBox should be below Auto-detect button");
            Assert.True(btnBrowse.Top > btnDetect.Top, "Browse button should be below Auto-detect button");

            // Browse button should be to the right of TextBox
            Assert.True(btnBrowse.Left > txtVnc.Left, "Browse button should be to the right of TextBox");

            form.Close();
        });
    }
}

