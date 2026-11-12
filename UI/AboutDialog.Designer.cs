namespace IPScan.UI;

partial class AboutDialog
{
    private System.ComponentModel.IContainer components = null!;
    private PictureBox picIcon = null!;
    private Label lblTitle = null!;
    private Label lblVersion = null!;
    private Label lblDeveloper = null!;
    private Label lblCopyright = null!;
    private Label lblFramework = null!;
    private Label lblDescription = null!;
    private Label lblDisclaimer = null!;
    private LinkLabel lnkGitHub = null!;
    private Button btnCheckUpdate = null!;
    private Button btnClose = null!;

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
        picIcon = new PictureBox();
        lblTitle = new Label();
        lblVersion = new Label();
        lblDeveloper = new Label();
        lblCopyright = new Label();
        lblFramework = new Label();
        lblDescription = new Label();
        lblDisclaimer = new Label();
        lnkGitHub = new LinkLabel();
        btnCheckUpdate = new Button();
        btnClose = new Button();
        pnlHeader = new Panel();
        pnlDivider = new Panel();
        ((System.ComponentModel.ISupportInitialize)picIcon).BeginInit();
        pnlHeader.SuspendLayout();
        SuspendLayout();
        // 
        // picIcon
        // 
        picIcon.Location = new Point(16, 12);
        picIcon.Name = "picIcon";
        picIcon.Size = new Size(56, 56);
        picIcon.SizeMode = PictureBoxSizeMode.Zoom;
        picIcon.TabIndex = 0;
        picIcon.TabStop = false;
        // 
        // lblTitle
        // 
        lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.Location = new Point(85, 16);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(360, 26);
        lblTitle.TabIndex = 1;
        lblTitle.Text = "IPScan";
        // 
        // lblVersion
        // 
        lblVersion.Font = new Font("Segoe UI", 9F);
        lblVersion.ForeColor = Color.FromArgb(56, 189, 248);
        lblVersion.Location = new Point(86, 44);
        lblVersion.Name = "lblVersion";
        lblVersion.Size = new Size(360, 20);
        lblVersion.TabIndex = 2;
        lblVersion.Text = "Version 1.1.0";
        // 
        // lblDeveloper
        // 
        lblDeveloper.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblDeveloper.Location = new Point(25, 95);
        lblDeveloper.Name = "lblDeveloper";
        lblDeveloper.Size = new Size(420, 20);
        lblDeveloper.TabIndex = 1;
        lblDeveloper.Text = "Developer: Md. Arif Uddin";
        // 
        // lblCopyright
        // 
        lblCopyright.ForeColor = Color.FromArgb(71, 85, 105);
        lblCopyright.Location = new Point(25, 118);
        lblCopyright.Name = "lblCopyright";
        lblCopyright.Size = new Size(420, 20);
        lblCopyright.TabIndex = 2;
        lblCopyright.Text = "Copyright © 2026 IPScan Contributors. All rights reserved.";
        // 
        // lblFramework
        // 
        lblFramework.ForeColor = Color.FromArgb(71, 85, 105);
        lblFramework.Location = new Point(25, 140);
        lblFramework.Name = "lblFramework";
        lblFramework.Size = new Size(420, 20);
        lblFramework.TabIndex = 3;
        lblFramework.Text = "Runtime: .NET 10.0 LTS";
        // 
        // lblDescription
        // 
        lblDescription.Location = new Point(25, 178);
        lblDescription.Name = "lblDescription";
        lblDescription.Size = new Size(410, 38);
        lblDescription.TabIndex = 5;
        lblDescription.Text = "A lightweight, standalone desktop IP scanner designed for fast host discovery, NetBIOS resolution, and authorized network assessment.";
        // 
        // lblDisclaimer
        // 
        lblDisclaimer.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
        lblDisclaimer.ForeColor = Color.FromArgb(100, 116, 139);
        lblDisclaimer.Location = new Point(25, 222);
        lblDisclaimer.Name = "lblDisclaimer";
        lblDisclaimer.Size = new Size(410, 36);
        lblDisclaimer.TabIndex = 6;
        lblDisclaimer.Text = "Authorized Use Notice: For diagnostic and inventory use only on networks you own or have written permission to assess.";
        // 
        // lnkGitHub
        // 
        lnkGitHub.Location = new Point(25, 270);
        lnkGitHub.Name = "lnkGitHub";
        lnkGitHub.Size = new Size(165, 22);
        lnkGitHub.TabIndex = 7;
        lnkGitHub.TabStop = true;
        lnkGitHub.Text = "GitHub Releases";
        lnkGitHub.LinkClicked += lnkGitHub_LinkClicked;
        // 
        // btnCheckUpdate
        // 
        btnCheckUpdate.Location = new Point(200, 265);
        btnCheckUpdate.Name = "btnCheckUpdate";
        btnCheckUpdate.Size = new Size(138, 30);
        btnCheckUpdate.TabIndex = 8;
        btnCheckUpdate.Text = "Check for Updates";
        btnCheckUpdate.UseVisualStyleBackColor = true;
        btnCheckUpdate.Click += btnCheckUpdate_Click;
        // 
        // btnClose
        // 
        btnClose.Location = new Point(345, 265);
        btnClose.Name = "btnClose";
        btnClose.Size = new Size(90, 30);
        btnClose.TabIndex = 9;
        btnClose.Text = "Close";
        btnClose.UseVisualStyleBackColor = true;
        btnClose.Click += btnClose_Click;
        // 
        // pnlHeader
        // 
        pnlHeader.BackColor = Color.FromArgb(15, 23, 42);
        pnlHeader.Controls.Add(picIcon);
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblVersion);
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Location = new Point(0, 0);
        pnlHeader.Name = "pnlHeader";
        pnlHeader.Size = new Size(460, 80);
        pnlHeader.TabIndex = 0;
        // 
        // pnlDivider
        // 
        pnlDivider.BackColor = Color.FromArgb(226, 232, 240);
        pnlDivider.Location = new Point(25, 166);
        pnlDivider.Name = "pnlDivider";
        pnlDivider.Size = new Size(410, 1);
        pnlDivider.TabIndex = 4;
        // 
        // AboutDialog
        // 
        AcceptButton = btnClose;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnClose;
        ClientSize = new Size(460, 310);
        Controls.Add(pnlHeader);
        Controls.Add(lblDeveloper);
        Controls.Add(lblCopyright);
        Controls.Add(lblFramework);
        Controls.Add(pnlDivider);
        Controls.Add(lblDescription);
        Controls.Add(lblDisclaimer);
        Controls.Add(lnkGitHub);
        Controls.Add(btnCheckUpdate);
        Controls.Add(btnClose);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AboutDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "About IPScan";
        ((System.ComponentModel.ISupportInitialize)picIcon).EndInit();
        pnlHeader.ResumeLayout(false);
        ResumeLayout(false);
    }

    private Panel pnlHeader;
    private Panel pnlDivider;
}

