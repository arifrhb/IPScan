namespace IPScan.UI;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null!;
    private NumericUpDown numConcurrency = null!;
    private NumericUpDown numTimeout = null!;
    private NumericUpDown numLargeScanThreshold = null!;
    private CheckBox chkResolveDns = null!;
    private CheckBox chkResolveNetBios = null!;
    private CheckBox chkQueryUser = null!;
    private Label lblVnc = null!;
    private TextBox txtVncPath = null!;
    private Label lblVncStatus = null!;
    private Button btnBrowseVnc = null!;
    private Button btnDetectVnc = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        
        var lblConcurrency = new Label();
        var lblTimeout = new Label();
        var lblLargeScan = new Label();
        var lblUserWarning = new Label();
        var grpPerformance = new GroupBox();
        var grpDiscovery = new GroupBox();
        var grpTools = new GroupBox();

        numConcurrency = new NumericUpDown();
        numTimeout = new NumericUpDown();
        numLargeScanThreshold = new NumericUpDown();
        chkResolveDns = new CheckBox();
        chkResolveNetBios = new CheckBox();
        chkQueryUser = new CheckBox();
        lblVnc = new Label();
        txtVncPath = new TextBox();
        lblVncStatus = new Label();
        btnBrowseVnc = new Button();
        btnDetectVnc = new Button();
        btnSave = new Button();
        btnCancel = new Button();

        // 
        // grpPerformance
        // 
        grpPerformance.Location = new Point(12, 12);
        grpPerformance.Size = new Size(524, 115);
        grpPerformance.Text = "Performance & Concurrency";

        lblConcurrency.Location = new Point(15, 25);
        lblConcurrency.Size = new Size(240, 20);
        lblConcurrency.Text = "Max Concurrency (threads):";

        numConcurrency.Location = new Point(265, 23);
        numConcurrency.Size = new Size(90, 23);
        numConcurrency.Minimum = 1;
        numConcurrency.Maximum = 256;
        numConcurrency.Value = 50;

        lblTimeout.Location = new Point(15, 54);
        lblTimeout.Size = new Size(240, 20);
        lblTimeout.Text = "Ping Timeout (milliseconds):";

        numTimeout.Location = new Point(265, 52);
        numTimeout.Size = new Size(90, 23);
        numTimeout.Minimum = 100;
        numTimeout.Maximum = 10000;
        numTimeout.Increment = 100;
        numTimeout.Value = 1000;

        lblLargeScan.Location = new Point(15, 83);
        lblLargeScan.Size = new Size(240, 20);
        lblLargeScan.Text = "Large scan warning threshold (targets):";

        numLargeScanThreshold.Location = new Point(265, 81);
        numLargeScanThreshold.Size = new Size(90, 23);
        numLargeScanThreshold.Minimum = 10;
        numLargeScanThreshold.Maximum = 65536;
        numLargeScanThreshold.Increment = 50;
        numLargeScanThreshold.Value = 256;

        grpPerformance.Controls.AddRange(new Control[] {
            lblConcurrency, numConcurrency,
            lblTimeout, numTimeout,
            lblLargeScan, numLargeScanThreshold
        });

        // 
        // grpDiscovery
        // 
        grpDiscovery.Location = new Point(12, 137);
        grpDiscovery.Size = new Size(524, 140);
        grpDiscovery.Text = "Host Discovery & Identification";

        chkResolveDns.Location = new Point(15, 22);
        chkResolveDns.Size = new Size(480, 24);
        chkResolveDns.Text = "Resolve Hostnames via Reverse DNS";
        chkResolveDns.Checked = true;

        chkResolveNetBios.Location = new Point(15, 48);
        chkResolveNetBios.Size = new Size(480, 24);
        chkResolveNetBios.Text = "Resolve NetBIOS Names (Node Status via UDP port 137)";
        chkResolveNetBios.Checked = true;

        chkQueryUser.Location = new Point(15, 74);
        chkQueryUser.Size = new Size(480, 24);
        chkQueryUser.Text = "Query Logged-in Username (WMI, NetAPI & NetBIOS discovery)";
        chkQueryUser.Checked = true;

        lblUserWarning.Location = new Point(35, 98);
        lblUserWarning.Size = new Size(480, 36);
        lblUserWarning.ForeColor = Color.DarkSlateGray;
        lblUserWarning.Font = new Font(Font.FontFamily, 8.25f, FontStyle.Italic);
        lblUserWarning.Text = "Note: Discovers logged-in users via authorized Windows WMI/RPC (Win32_ComputerSystem & interactive session owner), NetAPI32, and NetBIOS registered names.";

        grpDiscovery.Controls.AddRange(new Control[] {
            chkResolveDns, chkResolveNetBios, chkQueryUser, lblUserWarning
        });

        // 
        // grpTools
        // 
        grpTools.Location = new Point(12, 287);
        grpTools.Size = new Size(524, 115);
        grpTools.Text = "External Tools & VNC Viewer";

        lblVnc.Location = new Point(15, 23);
        lblVnc.AutoSize = true;
        lblVnc.Text = "VNC Viewer Executable path (Auto-detect):";

        btnDetectVnc.Location = new Point(275, 18);
        btnDetectVnc.Size = new Size(100, 26);
        btnDetectVnc.Text = "Auto-detect";
        btnDetectVnc.UseVisualStyleBackColor = true;
        btnDetectVnc.Click += btnDetectVnc_Click;

        txtVncPath.Location = new Point(15, 52);
        txtVncPath.Size = new Size(405, 23);

        btnBrowseVnc.Location = new Point(426, 51);
        btnBrowseVnc.Size = new Size(84, 25);
        btnBrowseVnc.Text = "Browse...";
        btnBrowseVnc.UseVisualStyleBackColor = true;
        btnBrowseVnc.Click += btnBrowseVnc_Click;

        lblVncStatus.Location = new Point(15, 83);
        lblVncStatus.Size = new Size(495, 26);
        lblVncStatus.Font = new Font(Font.FontFamily, 8f, FontStyle.Italic);
        lblVncStatus.ForeColor = Color.DarkSlateGray;
        lblVncStatus.Text = "Checks 64-bit and 32-bit RealVNC (Program Files) and standard registry paths.";

        grpTools.Controls.AddRange(new Control[] {
            lblVnc, btnDetectVnc, txtVncPath, btnBrowseVnc, lblVncStatus
        });

        // 
        // Buttons
        // 
        btnSave.Location = new Point(360, 412);
        btnSave.Size = new Size(82, 30);
        btnSave.Text = "Save";
        btnSave.UseVisualStyleBackColor = true;
        btnSave.Click += btnSave_Click;

        btnCancel.Location = new Point(450, 412);
        btnCancel.Size = new Size(82, 30);
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.DialogResult = DialogResult.Cancel;

        // 
        // SettingsForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(548, 454);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Scan & Application Settings";
        CancelButton = btnCancel;
        AcceptButton = btnSave;

        Controls.AddRange(new Control[] {
            grpPerformance, grpDiscovery, grpTools, btnSave, btnCancel
        });
    }
}

