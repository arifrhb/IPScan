namespace IPScan.UI;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private Panel pnlTop = null!;
    private Label lblInputHeader = null!;
    private Panel pnlInputWrapper = null!;
    private TextBox txtInput = null!;
    private Button btnStart = null!;
    private Button btnCancel = null!;
    private Button btnLoadFile = null!;
    private Button btnSettings = null!;
    private Button btnExport = null!;
    private Button btnAbout = null!;
    private Button btnShortcut = null!;
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
    private ToolStripMenuItem menuHttp = null!;
    private ToolStripMenuItem menuHttps = null!;
    private ToolStripMenuItem menuVnc = null!;
    private ToolStripMenuItem menuExplorer = null!;
    private ToolStripSeparator sep1 = null!;
    private ToolStripMenuItem menuCopyIp = null!;
    private ToolStripMenuItem menuCopyMac = null!;
    private ToolStripMenuItem menuCopyRow = null!;
    private ToolTip toolTip = null!;

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
        toolTip = new ToolTip(components);
        pnlTop = new Panel();
        lblInputHeader = new Label();
        lblFilter = new Label();
        pnlInputWrapper = new Panel();
        txtInput = new TextBox();
        btnStart = new Button();
        btnCancel = new Button();
        btnLoadFile = new Button();
        btnSettings = new Button();
        btnExport = new Button();
        btnAbout = new Button();
        btnShortcut = new Button();
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
        menuHttp = new ToolStripMenuItem();
        menuHttps = new ToolStripMenuItem();
        menuVnc = new ToolStripMenuItem();
        menuExplorer = new ToolStripMenuItem();
        sep1 = new ToolStripSeparator();
        menuCopyIp = new ToolStripMenuItem();
        menuCopyMac = new ToolStripMenuItem();
        menuCopyRow = new ToolStripMenuItem();

        pnlTop.SuspendLayout();
        pnlInputWrapper.SuspendLayout();
        pnlGridContainer.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)gridResults).BeginInit();
        statusStrip.SuspendLayout();
        contextMenu.SuspendLayout();
        SuspendLayout();

        // 
        // pnlTop
        // 
        pnlTop.BackColor = SystemColors.Control;
        pnlTop.Controls.Add(lblInputHeader);
        pnlTop.Controls.Add(pnlInputWrapper);
        pnlTop.Controls.Add(btnLoadFile);
        pnlTop.Controls.Add(lblTargetInfo);
        pnlTop.Controls.Add(btnStart);
        pnlTop.Controls.Add(btnCancel);
        pnlTop.Controls.Add(btnSettings);
        pnlTop.Controls.Add(btnExport);
        pnlTop.Controls.Add(btnAbout);
        pnlTop.Controls.Add(btnShortcut);
        pnlTop.Controls.Add(lblFilter);
        pnlTop.Controls.Add(btnFilterAll);
        pnlTop.Controls.Add(btnFilterOnline);
        pnlTop.Controls.Add(btnFilterOffline);
        pnlTop.Dock = DockStyle.Top;
        pnlTop.Location = new Point(0, 0);
        pnlTop.Name = "pnlTop";
        pnlTop.Size = new Size(960, 88);
        pnlTop.TabIndex = 0;

        // 
        // lblInputHeader
        // 
        lblInputHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblInputHeader.Location = new Point(25, 15);
        lblInputHeader.Name = "lblInputHeader";
        lblInputHeader.Size = new Size(80, 20);
        lblInputHeader.TabIndex = 11;
        lblInputHeader.Text = "IP Range:";
        lblInputHeader.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // pnlInputWrapper
        // 
        pnlInputWrapper.BackColor = Color.White;
        pnlInputWrapper.Controls.Add(txtInput);
        pnlInputWrapper.Location = new Point(110, 10);
        pnlInputWrapper.Name = "pnlInputWrapper";
        pnlInputWrapper.Size = new Size(360, 30);
        pnlInputWrapper.TabIndex = 0;

        // 
        // txtInput
        // 
        txtInput.BorderStyle = BorderStyle.None;
        txtInput.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        txtInput.Location = new Point(10, 5);
        txtInput.Name = "txtInput";
        txtInput.PlaceholderText = "e.g. 192.168.1.1-254 or 192.168.1.1";
        txtInput.Size = new Size(340, 18);
        txtInput.TabIndex = 0;

        // 
        // btnLoadFile
        // 
        btnLoadFile.Location = new Point(480, 10);
        btnLoadFile.Name = "btnLoadFile";
        btnLoadFile.Size = new Size(85, 30);
        btnLoadFile.TabIndex = 1;
        btnLoadFile.Text = "Upload";
        btnLoadFile.UseVisualStyleBackColor = true;
        btnLoadFile.Click += btnLoadFile_Click;

        // 
        // lblTargetInfo
        // 
        lblTargetInfo.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        lblTargetInfo.Location = new Point(575, 15);
        lblTargetInfo.Name = "lblTargetInfo";
        lblTargetInfo.Size = new Size(250, 20);
        lblTargetInfo.TabIndex = 12;
        lblTargetInfo.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // btnStart
        // 
        btnStart.BackColor = Color.FromArgb(0, 120, 215);
        btnStart.FlatStyle = FlatStyle.Flat;
        btnStart.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnStart.ForeColor = Color.White;
        btnStart.Location = new Point(25, 48);
        btnStart.Name = "btnStart";
        btnStart.Size = new Size(90, 30);
        btnStart.TabIndex = 2;
        btnStart.Text = "Start Scan";
        btnStart.UseVisualStyleBackColor = false;
        btnStart.Click += btnStart_Click;

        // 
        // btnCancel
        // 
        btnCancel.Location = new Point(121, 48);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(82, 30);
        btnCancel.TabIndex = 3;
        btnCancel.Text = "Stop (Esc)";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;

        // 
        // btnSettings
        // 
        btnSettings.Location = new Point(209, 48);
        btnSettings.Name = "btnSettings";
        btnSettings.Size = new Size(75, 30);
        btnSettings.TabIndex = 4;
        btnSettings.Text = "Settings";
        btnSettings.UseVisualStyleBackColor = true;
        btnSettings.Click += btnSettings_Click;

        // 
        // btnExport
        // 
        btnExport.Location = new Point(290, 48);
        btnExport.Name = "btnExport";
        btnExport.Size = new Size(78, 30);
        btnExport.TabIndex = 5;
        btnExport.Text = "Export...";
        btnExport.UseVisualStyleBackColor = true;
        btnExport.Click += btnExport_Click;

        // 
        // btnAbout
        // 
        btnAbout.Location = new Point(374, 48);
        btnAbout.Name = "btnAbout";
        btnAbout.Size = new Size(68, 30);
        btnAbout.TabIndex = 6;
        btnAbout.Text = "About";
        btnAbout.UseVisualStyleBackColor = true;
        btnAbout.Click += btnAbout_Click;

        // 
        // btnShortcut
        // 
        btnShortcut.Location = new Point(448, 48);
        btnShortcut.Name = "btnShortcut";
        btnShortcut.Size = new Size(75, 30);
        btnShortcut.TabIndex = 7;
        btnShortcut.Text = "Shortcut";
        toolTip.SetToolTip(btnShortcut, "Create a Desktop shortcut");
        btnShortcut.UseVisualStyleBackColor = true;
        btnShortcut.Click += btnShortcut_Click;

        // 
        // lblFilter
        // 
        lblFilter.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblFilter.Location = new Point(538, 53);
        lblFilter.Name = "lblFilter";
        lblFilter.Size = new Size(46, 20);
        lblFilter.TabIndex = 13;
        lblFilter.Text = "Filter:";

        // 
        // btnFilterAll
        // 
        btnFilterAll.FlatStyle = FlatStyle.Flat;
        btnFilterAll.Location = new Point(586, 48);
        btnFilterAll.Name = "btnFilterAll";
        btnFilterAll.Size = new Size(58, 30);
        btnFilterAll.TabIndex = 8;
        btnFilterAll.Text = "All";
        btnFilterAll.UseVisualStyleBackColor = true;
        btnFilterAll.Click += btnFilterAll_Click;

        // 
        // btnFilterOnline
        // 
        btnFilterOnline.FlatStyle = FlatStyle.Flat;
        btnFilterOnline.Location = new Point(650, 48);
        btnFilterOnline.Name = "btnFilterOnline";
        btnFilterOnline.Size = new Size(68, 30);
        btnFilterOnline.TabIndex = 9;
        btnFilterOnline.Text = "Online";
        btnFilterOnline.UseVisualStyleBackColor = true;
        btnFilterOnline.Click += btnFilterOnline_Click;

        // 
        // btnFilterOffline
        // 
        btnFilterOffline.FlatStyle = FlatStyle.Flat;
        btnFilterOffline.Location = new Point(724, 48);
        btnFilterOffline.Name = "btnFilterOffline";
        btnFilterOffline.Size = new Size(68, 30);
        btnFilterOffline.TabIndex = 10;
        btnFilterOffline.Text = "Offline";
        btnFilterOffline.UseVisualStyleBackColor = true;
        btnFilterOffline.Click += btnFilterOffline_Click;

        // 
        // pnlGridContainer
        // 
        pnlGridContainer.BackColor = SystemColors.Control;
        pnlGridContainer.Controls.Add(gridResults);
        pnlGridContainer.Dock = DockStyle.Fill;
        pnlGridContainer.Location = new Point(0, 88);
        pnlGridContainer.Name = "pnlGridContainer";
        pnlGridContainer.Padding = new Padding(25, 2, 25, 8);
        pnlGridContainer.Size = new Size(960, 450);
        pnlGridContainer.TabIndex = 1;

        // 
        // gridResults
        // 
        gridResults.BackgroundColor = Color.White;
        gridResults.BorderStyle = BorderStyle.FixedSingle;
        gridResults.Dock = DockStyle.Fill;
        gridResults.GridColor = Color.FromArgb(230, 230, 230);
        gridResults.Location = new Point(25, 2);
        gridResults.Name = "gridResults";
        gridResults.Size = new Size(910, 440);
        gridResults.TabIndex = 0;

        // 
        // statusStrip
        // 
        statusStrip.Items.Add(lblStatus);
        statusStrip.Items.Add(progressBar);
        statusStrip.Items.Add(lblElapsed);
        statusStrip.Items.Add(lblVersionStrip);
        statusStrip.Location = new Point(0, 538);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(960, 22);
        statusStrip.TabIndex = 2;

        // 
        // lblStatus
        // 
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(630, 17);
        lblStatus.Spring = true;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // progressBar
        // 
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(160, 16);
        progressBar.Style = ProgressBarStyle.Continuous;

        // 
        // lblElapsed
        // 
        lblElapsed.Name = "lblElapsed";
        lblElapsed.Size = new Size(95, 17);
        lblElapsed.Text = "Elapsed: 00:00";
        lblElapsed.TextAlign = ContentAlignment.MiddleRight;

        // 
        // lblVersionStrip
        // 
        lblVersionStrip.ForeColor = Color.DarkSlateGray;
        lblVersionStrip.Name = "lblVersionStrip";
        lblVersionStrip.Size = new Size(60, 17);
        lblVersionStrip.Text = "v1.1.0";
        lblVersionStrip.TextAlign = ContentAlignment.MiddleRight;

        // 
        // contextMenu
        // 
        contextMenu.ImageScalingSize = new Size(16, 16);
        contextMenu.Items.Add(menuHttp);
        contextMenu.Items.Add(menuHttps);
        contextMenu.Items.Add(menuVnc);
        contextMenu.Items.Add(menuExplorer);
        contextMenu.Items.Add(sep1);
        contextMenu.Items.Add(menuCopyIp);
        contextMenu.Items.Add(menuCopyMac);
        contextMenu.Items.Add(menuCopyRow);
        contextMenu.Name = "contextMenu";
        contextMenu.Size = new Size(235, 164);
        contextMenu.Opening += ContextMenu_Opening;

        // 
        // menuHttp
        // 
        menuHttp.Name = "menuHttp";
        menuHttp.Size = new Size(234, 22);
        menuHttp.Text = "Open HTTP (http://<IP>)";
        menuHttp.Click += menuHttp_Click;

        // 
        // menuHttps
        // 
        menuHttps.Name = "menuHttps";
        menuHttps.Size = new Size(234, 22);
        menuHttps.Text = "Open HTTPS (https://<IP>)";
        menuHttps.Click += menuHttps_Click;

        // 
        // menuVnc
        // 
        menuVnc.Name = "menuVnc";
        menuVnc.Size = new Size(234, 22);
        menuVnc.Text = "Launch VNC Viewer";
        menuVnc.Click += menuVnc_Click;

        // 
        // menuExplorer
        // 
        menuExplorer.Name = "menuExplorer";
        menuExplorer.Size = new Size(234, 22);
        menuExplorer.Text = "Open Shared Folders (\\\\<IP>\\)";
        menuExplorer.Click += menuExplorer_Click;

        // 
        // sep1
        // 
        sep1.Name = "sep1";
        sep1.Size = new Size(231, 6);

        // 
        // menuCopyIp
        // 
        menuCopyIp.Name = "menuCopyIp";
        menuCopyIp.Size = new Size(234, 22);
        menuCopyIp.Text = "Copy IP Address";
        menuCopyIp.Click += menuCopyIp_Click;

        // 
        // menuCopyMac
        // 
        menuCopyMac.Name = "menuCopyMac";
        menuCopyMac.Size = new Size(234, 22);
        menuCopyMac.Text = "Copy MAC Address";
        menuCopyMac.Click += menuCopyMac_Click;

        // 
        // menuCopyRow
        // 
        menuCopyRow.Name = "menuCopyRow";
        menuCopyRow.Size = new Size(234, 22);
        menuCopyRow.Text = "Copy Row Details";
        menuCopyRow.Click += menuCopyRow_Click;

        // 
        // MainForm
        // 
        AcceptButton = btnStart;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(960, 560);
        Controls.Add(pnlGridContainer);
        Controls.Add(pnlTop);
        Controls.Add(statusStrip);
        KeyPreview = true;
        MinimumSize = new Size(820, 420);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "IPScan - Standalone Network IP Scanner";
        pnlTop.ResumeLayout(false);
        pnlInputWrapper.ResumeLayout(false);
        pnlInputWrapper.PerformLayout();
        pnlGridContainer.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)gridResults).EndInit();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        contextMenu.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
