using System.Diagnostics;
using System.Reflection;

namespace IPScan.UI;

public partial class AboutDialog : Form
{
    public AboutDialog()
    {
        InitializeComponent();
        LoadAssemblyInformation();
        LoadAppIcon();
    }

    private void LoadAppIcon()
    {
        try
        {
            var icon = AppIconHelper.GetAppIcon();
            if (icon != null)
            {
                Icon = icon;
                picIcon.Image = icon.ToBitmap();
            }
        }
        catch
        {
            // Fallback gracefully if icon loading fails
        }
    }

    private void LoadAssemblyInformation()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var ver = assembly.GetName().Version;
        string versionStr = ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.1.0";

        var infoVerAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (infoVerAttr != null && !string.IsNullOrWhiteSpace(infoVerAttr.InformationalVersion))
        {
            versionStr = infoVerAttr.InformationalVersion.Split('+')[0]; // Strip build commit hash if any
        }

        lblVersion.Text = $"Version {versionStr}";
        lblFramework.Text = $"Runtime: .NET {Environment.Version.Major}.{Environment.Version.Minor} ({System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture})";
    }

    private void btnClose_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void lnkGitHub_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/arifrhb",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore if default browser cannot be opened
        }
    }
}

