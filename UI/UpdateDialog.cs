using System.Diagnostics;
using IPScan.Models;
using IPScan.Services.Updates;

namespace IPScan.UI;

public partial class UpdateDialog : Form
{
    private readonly UpdateInfo _updateInfo;
    private readonly UpdateService _updateService;
    private readonly bool _autoStartDownload;
    private CancellationTokenSource? _downloadCts;
    private bool _isDownloading;

    public bool UpdateApplied { get; private set; }

    public UpdateDialog(UpdateInfo updateInfo, UpdateService updateService, bool autoStartDownload = false)
    {
        _updateInfo = updateInfo ?? throw new ArgumentNullException(nameof(updateInfo));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _autoStartDownload = autoStartDownload;

        InitializeComponent();
        AppIconHelper.ApplyToForm(this);
        PopulateDetails();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_autoStartDownload)
        {
            await StartUpdateAsync();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isDownloading && _downloadCts != null && !_downloadCts.IsCancellationRequested)
        {
            _downloadCts.Cancel();
        }
        base.OnFormClosing(e);
    }

    private void PopulateDetails()
    {
        lblSubtitle.Text = $"IPScan v{_updateInfo.LatestVersion} is available (installed: v{_updateInfo.CurrentVersion})";
        lblPackageInfo.Text = $"Package: {_updateInfo.AssetName} ({_updateInfo.FormattedAssetSize}) • {_updateInfo.BuildFlavor.ToDisplayName()}";

        string notes = string.IsNullOrWhiteSpace(_updateInfo.ReleaseNotes)
            ? "No release notes provided for this version."
            : _updateInfo.ReleaseNotes.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        txtReleaseNotes.Text = notes;
    }

    private async void btnUpdate_Click(object? sender, EventArgs e)
    {
        await StartUpdateAsync();
    }

    private void btnCancel_Click(object? sender, EventArgs e)
    {
        if (_isDownloading)
        {
            _downloadCts?.Cancel();
            btnCancel.Enabled = false;
            lblProgress.Text = "Cancelling download...";
        }
        else
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private void lnkGitHub_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _updateInfo.ReleasePageUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore if browser cannot be opened
        }
    }

    private async Task StartUpdateAsync()
    {
        if (_isDownloading) return;

        _isDownloading = true;
        _downloadCts = new CancellationTokenSource();
        btnUpdate.Enabled = false;
        btnCancel.Text = "Cancel";
        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.Value = 0;
        lblProgress.ForeColor = Color.FromArgb(71, 85, 105);
        lblProgress.Text = $"Downloading {_updateInfo.AssetName}...";

        try
        {
            var progress = new Progress<UpdateDownloadProgress>(p =>
            {
                if (IsDisposed) return;

                if (p.Percentage.HasValue)
                {
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressBar.Value = p.Percentage.Value;
                    lblProgress.Text = $"Downloading {p.Percentage.Value}% ({FormatBytes(p.BytesReceived)} / {FormatBytes(p.TotalBytes ?? 0)})...";
                }
                else
                {
                    progressBar.Style = ProgressBarStyle.Marquee;
                    lblProgress.Text = $"Downloading ({FormatBytes(p.BytesReceived)})...";
                }
            });

            string verifiedPath = await _updateService.DownloadAndVerifyUpdateAsync(
                _updateInfo,
                progress,
                _downloadCts.Token);

            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 100;
            lblProgress.Text = !string.IsNullOrWhiteSpace(_updateInfo.ExpectedSha256)
                ? "SHA-256 verified. Applying update and restarting..."
                : "Download complete. Applying update and restarting...";

            UpdateService.ApplyUpdateAndRestart(verifiedPath);
            UpdateApplied = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (OperationCanceledException)
        {
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;
            lblProgress.Text = "Update cancelled.";
            ResetButtonsAfterError();
        }
        catch (Exception ex)
        {
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;
            lblProgress.ForeColor = Color.FromArgb(185, 28, 28);
            lblProgress.Text = $"Update failed: {ex.Message}";
            MessageBox.Show(
                this,
                $"Unable to complete the automatic update:\n\n{ex.Message}\n\nYou can also download the release manually from GitHub.",
                "Update Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            ResetButtonsAfterError();
        }
        finally
        {
            _isDownloading = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    private void ResetButtonsAfterError()
    {
        btnUpdate.Enabled = true;
        btnCancel.Enabled = true;
        btnCancel.Text = "Later";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F0} KB";
        return $"{bytes} B";
    }
}

