using System.Diagnostics;
using System.Reflection;
using IPScan.Models;
using IPScan.Services.Updates;

namespace IPScan.UI;

public partial class AboutDialog : Form
{
    private readonly UpdateService? _sharedUpdateService;

    public bool UpdateApplied { get; private set; }

    public AboutDialog(UpdateService? updateService = null)
    {
        _sharedUpdateService = updateService;
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
        var flavor = UpdateService.DetectCurrentBuildFlavor();
        string flavorTag = flavor == BuildFlavor.Lite ? "Lightweight" : "Standalone";
        lblFramework.Text = $"Runtime: .NET {Environment.Version.Major}.{Environment.Version.Minor} ({System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}) • {flavorTag}";
    }

    private async void btnCheckUpdate_Click(object? sender, EventArgs e)
    {
        btnCheckUpdate.Enabled = false;
        string originalText = btnCheckUpdate.Text;
        btnCheckUpdate.Text = "Checking...";

        UpdateService? ownedService = null;
        try
        {
            var service = _sharedUpdateService ?? (ownedService = new UpdateService());
            var update = await service.CheckForUpdateAsync();

            if (update == null)
            {
                MessageBox.Show(
                    this,
                    $"No published release was found yet on GitHub.\nYou are running v{UpdateService.GetCurrentVersionString()}.",
                    "Check for Updates",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (!update.IsUpdateAvailable)
            {
                MessageBox.Show(
                    this,
                    $"You are using the latest version of IPScan (v{update.CurrentVersion}).",
                    "Up to Date",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var updateDialog = new UpdateDialog(update, service);
            updateDialog.ShowDialog(this);
            if (updateDialog.UpdateApplied)
            {
                UpdateApplied = true;
                Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Unable to check for updates right now:\n\n{ex.Message}",
                "Update Check Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            ownedService?.Dispose();
            if (!IsDisposed)
            {
                btnCheckUpdate.Enabled = true;
                btnCheckUpdate.Text = originalText;
            }
        }
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
                FileName = "https://github.com/arifrhb/IPScan/releases",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore if default browser cannot be opened
        }
    }
}

