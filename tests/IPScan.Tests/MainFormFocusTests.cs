using System;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using IPScan.UI;
using Xunit;

namespace IPScan.Tests;

public class MainFormFocusTests
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
    public void Form_SetsFocusToInputBox_OnLoad()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            var txtInput = FindControlRecursive(form, c => c is TextBox) as TextBox;
            Assert.NotNull(txtInput);
            Assert.Same(txtInput, form.ActiveControl);

            form.Close();
        });
    }

    [Fact]
    public void Form_InputBox_AcceptsDirectTypingOnLoad()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            var txtInput = FindControlRecursive(form, c => c is TextBox) as TextBox;
            Assert.NotNull(txtInput);
            Assert.Same(txtInput, form.ActiveControl);

            // Simulate user directly typing into the focused control
            txtInput.Text = "192.168.1.1-50";
            Application.DoEvents();

            var lblTarget = FindControlRecursive(form, c => c is Label && c.Text.Contains("targets")) as Label;
            Assert.NotNull(lblTarget);
            Assert.Contains("50 targets", lblTarget.Text);

            form.Close();
        });
    }
}
