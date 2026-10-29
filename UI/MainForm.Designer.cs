using IPScan.Models;

namespace IPScan.UI;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private Panel pnlInputWrapper = null!;
    private TextBox txtInput = null!;
    private Button btnStart = null!;
    private Button btnCancel = null!;
    private Button btnLoadFile = null!;
    private Button btnSettings = null!;
    private Button btnExport = null!;
    private Button btnAbout = null!;
    private Label lblTargetInfo = null!;
    private Label lblFilter = null!;
    private Button btnFilterAll = null!;
    private Button btnFilterOnline = null!;
    private Button btnFilterOffline = null!;
    private Panel pnlGridContainer = null!;
    private DataGridView gridResults = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel lblStatus = null!;
    private ToolStripProgressBar progressBar = null!;
    private ToolStripStatusLabel lblElapsed = null!;
    private ToolStripStatusLabel lblVersionStrip = null!;
    private ContextMenuStrip contextMenu = null!;

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

        var pnlTop = new Panel();
        var lblInputHeader = new Label();
        lblFilter = new Label();

        pnlInputWrapper = new Panel();
        txtInput = new TextBox();
        btnStart = new Button();
        btnCancel = new Button();
        btnLoadFile = new Button();
        btnSettings = new Button();
        btnExport = new Button();
        btnAbout = new Button();
        lblTargetInfo = new Label();

        btnFilterAll = new Button();
        btnFilterOnline = new Button();
        btnFilterOffline = new Button();

        gridResults = new DataGridView();
        pnlGridContainer = new Panel();
        statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel();
        progressBar = new ToolStripProgressBar();
        lblElapsed = new ToolStripStatusLabel();
        lblVersionStrip = new ToolStripStatusLabel();

        contextMenu = new ContextMenuStrip(components);
        var menuHttp = new ToolStripMenuItem("Open HTTP (http://<IP>)");
        var menuHttps = new ToolStripMenuItem("Open HTTPS (https://<IP>)");
        var menuVnc = new ToolStripMenuItem("Launch VNC Viewer");
        var menuExplorer = new ToolStripMenuItem("Open Shared Folders (\\\\<IP>\\)");
        var sep1 = new ToolStripSeparator();
        var menuCopyIp = new ToolStripMenuItem("Copy IP Address");
        var menuCopyRow = new ToolStripMenuItem("Copy Row Details");

        // 
        // pnlTop
        // 
        pnlTop.Dock = DockStyle.Top;
        pnlTop.Height = 88;
        pnlTop.BackColor = SystemColors.Control;

        // Row 1: Clear "IP Range:" label, compact input wrapper on the right, and comfortable margins
        lblInputHeader.Location = new Point(25, 15);
        lblInputHeader.Size = new Size(80, 20);
        lblInputHeader.Text = "IP Range:";
        lblInputHeader.TextAlign = ContentAlignment.MiddleLeft;
        lblInputHeader.Font = new Font(Font.FontFamily, 9f, FontStyle.Bold);

        pnlInputWrapper.Location = new Point(110, 10);
        pnlInputWrapper.Size = new Size(360, 30);
        pnlInputWrapper.BackColor = Color.White;

        txtInput.Dock = DockStyle.None;
        txtInput.BorderStyle = BorderStyle.None;
        txtInput.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
        txtInput.PlaceholderText = "e.g. 192.168.1.1-254 or 192.168.1.1";
        txtInput.Location = new Point(10, 5);
        txtInput.Width = 340;
        txtInput.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        pnlInputWrapper.Controls.Add(txtInput);

        btnLoadFile.Location = new Point(480, 10);
        btnLoadFile.Size = new Size(85, 30);
        btnLoadFile.Text = "Upload";
        btnLoadFile.UseVisualStyleBackColor = true;
        btnLoadFile.Click += btnLoadFile_Click;

        lblTargetInfo.Location = new Point(575, 15);
        lblTargetInfo.Size = new Size(250, 20);
        lblTargetInfo.TextAlign = ContentAlignment.MiddleLeft;
        lblTargetInfo.Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular);

        // Row 2: Action buttons aligned with symmetrical 25px left margin and filter alongside
        btnStart.Location = new Point(25, 48);
        btnStart.Size = new Size(90, 30);
        btnStart.Text = "Start Scan";
        btnStart.BackColor = Color.FromArgb(0, 120, 215);
        btnStart.ForeColor = Color.White;
        btnStart.FlatStyle = FlatStyle.Flat;
        btnStart.Font = new Font(Font.FontFamily, 9f, FontStyle.Bold);
        btnStart.UseVisualStyleBackColor = false;
        btnStart.Click += btnStart_Click;

        btnCancel.Location = new Point(121, 48);
        btnCancel.Size = new Size(82, 30);
        btnCancel.Text = "Stop (Esc)";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;

        btnSettings.Location = new Point(209, 48);
        btnSettings.Size = new Size(75, 30);
        btnSettings.Text = "Settings";
        btnSettings.UseVisualStyleBackColor = true;
        btnSettings.Click += btnSettings_Click;

        btnExport.Location = new Point(290, 48);
        btnExport.Size = new Size(78, 30);
        btnExport.Text = "Export...";
        btnExport.UseVisualStyleBackColor = true;
        btnExport.Click += btnExport_Click;

        btnAbout.Location = new Point(374, 48);
        btnAbout.Size = new Size(68, 30);
        btnAbout.Text = "About";
        btnAbout.UseVisualStyleBackColor = true;
        btnAbout.Click += btnAbout_Click;

        lblFilter.Location = new Point(458, 53);
        lblFilter.Size = new Size(46, 20);
        lblFilter.Text = "Filter:";
        lblFilter.Font = new Font(Font.FontFamily, 9f, FontStyle.Bold);

        btnFilterAll.Location = new Point(506, 48);
        btnFilterAll.Size = new Size(58, 30);
        btnFilterAll.Text = "All";
        btnFilterAll.FlatStyle = FlatStyle.Flat;
        btnFilterAll.UseVisualStyleBackColor = true;
        btnFilterAll.Click += (s, e) => SetFilterMode(ResultFilterMode.Both);

        btnFilterOnline.Location = new Point(570, 48);
        btnFilterOnline.Size = new Size(68, 30);
        btnFilterOnline.Text = "Online";
        btnFilterOnline.FlatStyle = FlatStyle.Flat;
        btnFilterOnline.UseVisualStyleBackColor = true;
        btnFilterOnline.Click += (s, e) => SetFilterMode(ResultFilterMode.Online);

        btnFilterOffline.Location = new Point(644, 48);
        btnFilterOffline.Size = new Size(68, 30);
        btnFilterOffline.Text = "Offline";
        btnFilterOffline.FlatStyle = FlatStyle.Flat;
        btnFilterOffline.UseVisualStyleBackColor = true;
        btnFilterOffline.Click += (s, e) => SetFilterMode(ResultFilterMode.Offline);

        pnlTop.Controls.AddRange(new Control[] {
            lblInputHeader, pnlInputWrapper, btnLoadFile, lblTargetInfo,
            btnStart, btnCancel, btnSettings, btnExport, btnAbout,
            lblFilter, btnFilterAll, btnFilterOnline, btnFilterOffline
        });

        // 
        // pnlGridContainer (Provides balanced 25px margin on both sides of table)
        // 
        pnlGridContainer.Dock = DockStyle.Fill;
        pnlGridContainer.Padding = new Padding(25, 2, 25, 8);
        pnlGridContainer.BackColor = SystemColors.Control;

        // 
        // gridResults
        // 
        gridResults.Dock = DockStyle.Fill;
        gridResults.BackgroundColor = Color.White;
        gridResults.BorderStyle = BorderStyle.FixedSingle;
        gridResults.GridColor = Color.FromArgb(230, 230, 230);
        pnlGridContainer.Controls.Add(gridResults);

        // 
        // statusStrip
        // 
        statusStrip.Items.AddRange(new ToolStripItem[] {
            lblStatus, progressBar, lblElapsed, lblVersionStrip
        });

        lblStatus.Spring = true;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;

        progressBar.Size = new Size(160, 16);
        progressBar.Style = ProgressBarStyle.Continuous;

        lblElapsed.Size = new Size(95, 17);
        lblElapsed.TextAlign = ContentAlignment.MiddleRight;
        lblElapsed.Text = "Elapsed: 00:00";

        lblVersionStrip.Size = new Size(60, 17);
        lblVersionStrip.TextAlign = ContentAlignment.MiddleRight;
        lblVersionStrip.ForeColor = Color.DarkSlateGray;
        lblVersionStrip.Text = "v1.1.0";

        // 
        // Context Menu (Only contains IP actions)
        // 
        menuHttp.Click += menuHttp_Click;
        menuHttps.Click += menuHttps_Click;
        menuVnc.Click += menuVnc_Click;
        menuExplorer.Click += menuExplorer_Click;
        menuCopyIp.Click += menuCopyIp_Click;
        menuCopyRow.Click += menuCopyRow_Click;

        contextMenu.Items.AddRange(new ToolStripItem[] {
            menuHttp,
            menuHttps,
            menuVnc,
            menuExplorer,
            sep1,
            menuCopyIp,
            menuCopyRow
        });

        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(960, 560);
        MinimumSize = new Size(820, 420);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "IPScan - Standalone Network IP Scanner";
        AcceptButton = btnStart;
        CancelButton = btnCancel;
        KeyPreview = true;

        Controls.Add(pnlGridContainer);
        Controls.Add(pnlTop);
        Controls.Add(statusStrip);
    }
}
