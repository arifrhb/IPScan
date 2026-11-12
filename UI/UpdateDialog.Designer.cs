namespace IPScan.UI;

partial class UpdateDialog
{
    private System.ComponentModel.IContainer components = null!;
    private Panel pnlHeader = null!;
    private Label lblTitle = null!;
    private Label lblSubtitle = null!;
    private Label lblPackageInfo = null!;
    private Label lblNotesHeader = null!;
    private TextBox txtReleaseNotes = null!;
    private ProgressBar progressBar = null!;
    private Label lblProgress = null!;
    private LinkLabel lnkGitHub = null!;
    private Button btnUpdate = null!;
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
        pnlHeader = new Panel();
        lblTitle = new Label();
        lblSubtitle = new Label();
        lblPackageInfo = new Label();
        lblNotesHeader = new Label();
        txtReleaseNotes = new TextBox();
        progressBar = new ProgressBar();
        lblProgress = new Label();
        lnkGitHub = new LinkLabel();
        btnUpdate = new Button();
        btnCancel = new Button();
        pnlHeader.SuspendLayout();
        SuspendLayout();

        //
        // pnlHeader
        //
        pnlHeader.BackColor = Color.FromArgb(15, 23, 42);
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSubtitle);
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Location = new Point(0, 0);
        pnlHeader.Name = "pnlHeader";
        pnlHeader.Size = new Size(520, 72);
        pnlHeader.TabIndex = 0;

        //
        // lblTitle
        //
        lblTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.Location = new Point(20, 12);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(480, 26);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "IPScan Update Available";

        //
        // lblSubtitle
        //
        lblSubtitle.Font = new Font("Segoe UI", 9F);
        lblSubtitle.ForeColor = Color.FromArgb(56, 189, 248);
        lblSubtitle.Location = new Point(20, 40);
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.Size = new Size(480, 20);
        lblSubtitle.TabIndex = 1;
        lblSubtitle.Text = "A new version is ready to download.";

        //
        // lblPackageInfo
        //
        lblPackageInfo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblPackageInfo.ForeColor = Color.FromArgb(30, 41, 59);
        lblPackageInfo.Location = new Point(20, 84);
        lblPackageInfo.Name = "lblPackageInfo";
        lblPackageInfo.Size = new Size(480, 20);
        lblPackageInfo.TabIndex = 1;
        lblPackageInfo.Text = "Package: IPScan-win-x64-lite.exe";

        //
        // lblNotesHeader
        //
        lblNotesHeader.Font = new Font("Segoe UI", 9F);
        lblNotesHeader.ForeColor = Color.FromArgb(71, 85, 105);
        lblNotesHeader.Location = new Point(20, 108);
        lblNotesHeader.Name = "lblNotesHeader";
        lblNotesHeader.Size = new Size(480, 18);
        lblNotesHeader.TabIndex = 2;
        lblNotesHeader.Text = "What's new in this release:";

        //
        // txtReleaseNotes
        //
        txtReleaseNotes.BackColor = Color.FromArgb(248, 250, 252);
        txtReleaseNotes.BorderStyle = BorderStyle.FixedSingle;
        txtReleaseNotes.Font = new Font("Segoe UI", 9F);
        txtReleaseNotes.Location = new Point(20, 130);
        txtReleaseNotes.Multiline = true;
        txtReleaseNotes.Name = "txtReleaseNotes";
        txtReleaseNotes.ReadOnly = true;
        txtReleaseNotes.ScrollBars = ScrollBars.Vertical;
        txtReleaseNotes.Size = new Size(480, 145);
        txtReleaseNotes.TabIndex = 3;

        //
        // progressBar
        //
        progressBar.Location = new Point(20, 286);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(480, 18);
        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.TabIndex = 4;

        //
        // lblProgress
        //
        lblProgress.Font = new Font("Segoe UI", 8.5F);
        lblProgress.ForeColor = Color.FromArgb(71, 85, 105);
        lblProgress.Location = new Point(20, 308);
        lblProgress.Name = "lblProgress";
        lblProgress.Size = new Size(480, 18);
        lblProgress.TabIndex = 5;
        lblProgress.Text = "Click 'Update Now' to download and install automatically.";

        //
        // lnkGitHub
        //
        lnkGitHub.AutoSize = true;
        lnkGitHub.Location = new Point(20, 342);
        lnkGitHub.Name = "lnkGitHub";
        lnkGitHub.Size = new Size(92, 15);
        lnkGitHub.TabIndex = 6;
        lnkGitHub.TabStop = true;
        lnkGitHub.Text = "View on GitHub";
        lnkGitHub.LinkClicked += lnkGitHub_LinkClicked;

        //
        // btnUpdate
        //
        btnUpdate.BackColor = Color.FromArgb(0, 120, 215);
        btnUpdate.FlatStyle = FlatStyle.Flat;
        btnUpdate.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnUpdate.ForeColor = Color.White;
        btnUpdate.Location = new Point(292, 335);
        btnUpdate.Name = "btnUpdate";
        btnUpdate.Size = new Size(112, 30);
        btnUpdate.TabIndex = 7;
        btnUpdate.Text = "Update Now";
        btnUpdate.UseVisualStyleBackColor = false;
        btnUpdate.Click += btnUpdate_Click;

        //
        // btnCancel
        //
        btnCancel.Location = new Point(410, 335);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(90, 30);
        btnCancel.TabIndex = 8;
        btnCancel.Text = "Later";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;

        //
        // UpdateDialog
        //
        AcceptButton = btnUpdate;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        CancelButton = btnCancel;
        ClientSize = new Size(520, 380);
        Controls.Add(pnlHeader);
        Controls.Add(lblPackageInfo);
        Controls.Add(lblNotesHeader);
        Controls.Add(txtReleaseNotes);
        Controls.Add(progressBar);
        Controls.Add(lblProgress);
        Controls.Add(lnkGitHub);
        Controls.Add(btnUpdate);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "UpdateDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Software Update - IPScan";
        pnlHeader.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}

