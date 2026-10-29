using IPScan.Models;
using IPScan.Services.External;

namespace IPScan.UI;

public partial class SettingsForm : Form
{
    private readonly ScanOptions _options;

    public SettingsForm(ScanOptions currentOptions)
    {
        _options = currentOptions ?? new ScanOptions();
        InitializeComponent();
        AppIconHelper.ApplyToForm(this);
        LoadSettings();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        btnDetectVnc.Left = lblVnc.Right + 8;
    }

    private void LoadSettings()
    {
        numConcurrency.Value = Math.Clamp(_options.Concurrency, numConcurrency.Minimum, numConcurrency.Maximum);
        numTimeout.Value = Math.Clamp(_options.TimeoutMs, numTimeout.Minimum, numTimeout.Maximum);
        numLargeScanThreshold.Value = Math.Clamp(_options.LargeScanWarningThreshold, numLargeScanThreshold.Minimum, numLargeScanThreshold.Maximum);
        chkResolveDns.Checked = _options.ResolveDns;
        chkResolveNetBios.Checked = _options.ResolveNetBios;
        chkQueryUser.Checked = _options.QueryLoggedInUser;

        string? vncPath = _options.CustomVncViewerPath;
        if (string.IsNullOrWhiteSpace(vncPath))
        {
            vncPath = VncDetector.DetectVncViewer();
        }

        txtVncPath.Text = vncPath ?? string.Empty;
        txtVncPath.TextChanged += (s, e) => UpdateVncStatus(txtVncPath.Text);
        UpdateVncStatus(vncPath);
    }

    private void UpdateVncStatus(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            lblVncStatus.Text = $"Active VNC Viewer: {path}";
            lblVncStatus.ForeColor = Color.DarkGreen;
        }
        else
        {
            lblVncStatus.Text = "No VNC viewer detected automatically. Click Browse to select your custom location.";
            lblVncStatus.ForeColor = Color.DarkSlateGray;
        }
    }

    private void btnSave_Click(object? sender, EventArgs e)
    {
        _options.Concurrency = (int)numConcurrency.Value;
        _options.TimeoutMs = (int)numTimeout.Value;
        _options.LargeScanWarningThreshold = (int)numLargeScanThreshold.Value;
        _options.ResolveDns = chkResolveDns.Checked;
        _options.ResolveNetBios = chkResolveNetBios.Checked;
        _options.QueryLoggedInUser = chkQueryUser.Checked;
        _options.CustomVncViewerPath = string.IsNullOrWhiteSpace(txtVncPath.Text) ? null : txtVncPath.Text.Trim();

        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnBrowseVnc_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Select VNC Viewer Executable",
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            txtVncPath.Text = ofd.FileName;
            UpdateVncStatus(ofd.FileName);
        }
    }

    private void btnDetectVnc_Click(object? sender, EventArgs e)
    {
        string? detected = VncDetector.DetectVncViewer();
        if (!string.IsNullOrEmpty(detected))
        {
            txtVncPath.Text = detected;
            UpdateVncStatus(detected);
            MessageBox.Show(this, $"Detected installed VNC viewer:\n{detected}", "VNC Viewer Detected", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            UpdateVncStatus(null);
            MessageBox.Show(this, "No installed VNC viewer was automatically detected in standard directories (RealVNC 64/32-bit) or the registry.\n\nYou can manually browse to your viewer's .exe file using the Browse button.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

