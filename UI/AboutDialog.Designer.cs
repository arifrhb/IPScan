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
        btnClose = new Button();

        var pnlHeader = new Panel();
        var pnlDivider = new Panel();

        // 
        // pnlHeader
        // 
        pnlHeader.BackColor = Color.FromArgb(15, 23, 42); // Deep slate
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Height = 80;

        picIcon.Location = new Point(16, 12);
        picIcon.Size = new Size(56, 56);
        picIcon.SizeMode = PictureBoxSizeMode.Zoom;

        lblTitle.Location = new Point(85, 16);
        lblTitle.Size = new Size(360, 26);
        lblTitle.Font = new Font(Font.FontFamily, 14f, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.Text = "IPScan";

        lblVersion.Location = new Point(86, 44);
        lblVersion.Size = new Size(360, 20);
        lblVersion.Font = new Font(Font.FontFamily, 9f, FontStyle.Regular);
        lblVersion.ForeColor = Color.FromArgb(56, 189, 248); // Cyan
        lblVersion.Text = "Version 1.1.0";

        pnlHeader.Controls.AddRange(new Control[] { picIcon, lblTitle, lblVersion });

        // 
        // Details
        // 
        lblDeveloper.Location = new Point(25, 95);
        lblDeveloper.Size = new Size(420, 20);
        lblDeveloper.Font = new Font(Font.FontFamily, 9f, FontStyle.Bold);
        lblDeveloper.Text = "Developer: Md. Arif Uddin (IPScan Security Tools)";

        lblCopyright.Location = new Point(25, 118);
        lblCopyright.Size = new Size(420, 20);
        lblCopyright.ForeColor = Color.FromArgb(71, 85, 105);
        lblCopyright.Text = "Copyright © 2026 IPScan Contributors. All rights reserved.";

        lblFramework.Location = new Point(25, 140);
        lblFramework.Size = new Size(420, 20);
        lblFramework.ForeColor = Color.FromArgb(71, 85, 105);
        lblFramework.Text = "Runtime: .NET 9.0";

        pnlDivider.Location = new Point(25, 166);
        pnlDivider.Size = new Size(410, 1);
        pnlDivider.BackColor = Color.FromArgb(226, 232, 240);

        lblDescription.Location = new Point(25, 178);
        lblDescription.Size = new Size(410, 38);
        lblDescription.Text = "A lightweight, standalone desktop IP scanner designed for fast host discovery, NetBIOS resolution, and authorized network assessment.";

        lblDisclaimer.Location = new Point(25, 222);
        lblDisclaimer.Size = new Size(410, 36);
        lblDisclaimer.Font = new Font(Font.FontFamily, 8f, FontStyle.Italic);
        lblDisclaimer.ForeColor = Color.FromArgb(100, 116, 139);
        lblDisclaimer.Text = "Authorized Use Notice: For diagnostic and inventory use only on networks you own or have written permission to assess.";

        lnkGitHub.Location = new Point(25, 270);
        lnkGitHub.Size = new Size(160, 22);
        lnkGitHub.Text = "Project Documentation";
        lnkGitHub.LinkClicked += lnkGitHub_LinkClicked;

        btnClose.Location = new Point(345, 265);
        btnClose.Size = new Size(90, 30);
        btnClose.Text = "Close";
        btnClose.UseVisualStyleBackColor = true;
        btnClose.Click += btnClose_Click;

        // 
        // AboutDialog
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(460, 310);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "About IPScan";
        CancelButton = btnClose;
        AcceptButton = btnClose;

        Controls.AddRange(new Control[] {
            pnlHeader, lblDeveloper, lblCopyright, lblFramework,
            pnlDivider, lblDescription, lblDisclaimer, lnkGitHub, btnClose
        });
    }
}

