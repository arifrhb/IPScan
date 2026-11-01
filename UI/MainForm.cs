using System.ComponentModel;
using System.Diagnostics;
using IPScan.Models;
using IPScan.Services.Export;
using IPScan.Services.External;
using IPScan.Services.Input;
using IPScan.Services.Scanning;

namespace IPScan.UI;

public partial class MainForm : Form
{
    private readonly ScanOptions _options = new();
    private readonly NetworkScanner _scanner = new();
    private readonly List<ScanResult> _allResults = new();
    private readonly BindingList<ScanResult> _visibleResults = new();

    private CancellationTokenSource? _scanCts;
    private readonly Stopwatch _stopwatch = new();
    private System.Windows.Forms.Timer? _elapsedTimer;

    // Accessible colors compliant with WCAG contrast guidelines
    private static readonly Color ColorOnlineBack = Color.FromArgb(232, 245, 233); // Soft light green
    private static readonly Color ColorOnlineFore = Color.FromArgb(27, 94, 32);    // Dark green
    private static readonly Color ColorOfflineBack = Color.FromArgb(250, 250, 250); // Soft neutral gray
    private static readonly Color ColorOfflineFore = Color.FromArgb(97, 97, 97);    // Muted dark gray

    public MainForm()
    {
        InitializeComponent();
        LoadAppIcon();
        ConfigureGrid();
        InitializeState();
    }

    private void LoadAppIcon()
    {
        AppIconHelper.ApplyToForm(this);
    }

    private bool _txtInputFocused = false;
    private bool _txtInputHovered = false;
    private ResultFilterMode _currentFilterMode = ResultFilterMode.Both;
    private ResultFilterMode CurrentFilterMode => _currentFilterMode;

    private string _sortColumnName = ResultSorter.ColumnIp;
    private SortOrder _sortOrder = SortOrder.Ascending;
    public string CurrentSortColumn => _sortColumnName;
    public SortOrder CurrentSortOrder => _sortOrder;

    public void SetSortOrder(string columnName, SortOrder order)
    {
        _sortColumnName = columnName;
        _sortOrder = order;
        RefreshFilteredView();
    }

    public void SetFilterMode(ResultFilterMode mode)
    {
        _currentFilterMode = mode;
        UpdateFilterButtonStyles();
        RefreshFilteredView();
    }

    private void UpdateFilterButtonStyles()
    {
        ApplyFilterButtonStyle(btnFilterAll, _currentFilterMode == ResultFilterMode.Both);
        ApplyFilterButtonStyle(btnFilterOnline, _currentFilterMode == ResultFilterMode.Online);
        ApplyFilterButtonStyle(btnFilterOffline, _currentFilterMode == ResultFilterMode.Offline);
    }

    private static void ApplyFilterButtonStyle(Button btn, bool isActive)
    {
        if (isActive)
        {
            btn.BackColor = Color.FromArgb(0, 120, 215);
            btn.ForeColor = Color.White;
            btn.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 180);
            btn.FlatAppearance.BorderSize = 1;
            btn.Font = new Font(btn.Font.FontFamily, 9f, FontStyle.Bold);
        }
        else
        {
            btn.BackColor = Color.FromArgb(242, 244, 246);
            btn.ForeColor = Color.FromArgb(40, 40, 40);
            btn.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 220);
            btn.FlatAppearance.BorderSize = 1;
            btn.Font = new Font(btn.Font.FontFamily, 9f, FontStyle.Regular);
        }
    }

    private void InitializeState()
    {
        btnCancel.Enabled = false;
        btnExport.Enabled = false;
        lblStatus.Text = "Ready. Enter an IP range (e.g. 192.168.1.1-254) or IP address and click Start Scan.";
        lblTargetInfo.Text = "0 targets";

        var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        lblVersionStrip.Text = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v1.2.0";

        ActiveControl = txtInput;

        _elapsedTimer = new System.Windows.Forms.Timer { Interval = 500 };
        _elapsedTimer.Tick += (s, e) =>
        {
            if (_stopwatch.IsRunning)
            {
                lblElapsed.Text = $"Elapsed: {_stopwatch.Elapsed:mm\\:ss}";
            }
        };

        // Modern input box layout and focus painting
        pnlInputWrapper.Resize += (s, e) => LayoutInputBox();
        LayoutInputBox();

        pnlInputWrapper.Click += (s, e) => txtInput.Focus();
        pnlInputWrapper.MouseEnter += (s, e) => { _txtInputHovered = true; pnlInputWrapper.Invalidate(); };
        pnlInputWrapper.MouseLeave += (s, e) => { _txtInputHovered = false; pnlInputWrapper.Invalidate(); };
        txtInput.MouseEnter += (s, e) => { _txtInputHovered = true; pnlInputWrapper.Invalidate(); };
        txtInput.MouseLeave += (s, e) => { _txtInputHovered = false; pnlInputWrapper.Invalidate(); };

        pnlInputWrapper.Paint += (s, pe) =>
        {
            pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pnlInputWrapper.Width - 1, pnlInputWrapper.Height - 1);
            int radius = 4;
            using var path = CreateRoundedRectanglePath(rect, radius);

            using var bgBrush = new SolidBrush(Color.White);
            pe.Graphics.FillPath(bgBrush, path);

            Color borderColor = _txtInputFocused
                ? Color.FromArgb(0, 120, 215)
                : (_txtInputHovered ? Color.FromArgb(160, 166, 175) : Color.FromArgb(204, 209, 216));

            using var pen = new Pen(borderColor, _txtInputFocused ? 1.5f : 1f);
            pe.Graphics.DrawPath(pen, path);
        };

        txtInput.Enter += (s, e) => { _txtInputFocused = true; pnlInputWrapper.Invalidate(); };
        txtInput.Leave += (s, e) => { _txtInputFocused = false; pnlInputWrapper.Invalidate(); };
        txtInput.TextChanged += TxtInput_TextChanged;
        txtInput.KeyDown += TxtInput_KeyDown;

        UpdateFilterButtonStyles();
        UpdateSortGlyphs();

        // Automatically configure VNC viewer if installed
        SetupVncViewerOnStartup();

        // Enable drag & drop for text files containing IPs line by line
        AllowDrop = true;
        DragEnter += MainForm_DragEnter;
        DragDrop += MainForm_DragDrop;
        txtInput.AllowDrop = true;
        txtInput.DragEnter += MainForm_DragEnter;
        txtInput.DragDrop += MainForm_DragDrop;
    }

    private void SetupVncViewerOnStartup()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.CustomVncViewerPath))
            {
                string? detected = VncDetector.DetectVncViewer();
                if (!string.IsNullOrWhiteSpace(detected) && File.Exists(detected))
                {
                    _options.CustomVncViewerPath = detected;
                }
            }
        }
        catch
        {
            // Defensive catch to guarantee startup stability
        }
    }

    private void MainForm_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect = DragDropEffects.Copy;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    private async void MainForm_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            string file = files[0];
            if (File.Exists(file))
            {
                await ProcessLoadedFileAsync(file);
            }
        }
    }

    private void ConfigureGrid()
    {
        gridResults.AutoGenerateColumns = false;
        gridResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridResults.MultiSelect = false;
        gridResults.ReadOnly = true;
        gridResults.AllowUserToAddRows = false;
        gridResults.AllowUserToDeleteRows = false;
        gridResults.AllowUserToResizeRows = false;
        gridResults.RowHeadersVisible = false;
        gridResults.EnableHeadersVisualStyles = true;
        gridResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        // Double buffering to eliminate flicker during fast live row additions
        typeof(DataGridView).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
            null, gridResults, new object[] { true });

        gridResults.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn
            {
                Name = "ColStatus",
                HeaderText = "Status",
                DataPropertyName = nameof(ScanResult.AccessibleStatus),
                FillWeight = 11,
                MinimumWidth = 75,
                SortMode = DataGridViewColumnSortMode.Programmatic
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColIp",
                HeaderText = "IP Address",
                DataPropertyName = nameof(ScanResult.IpString),
                FillWeight = 14,
                MinimumWidth = 105,
                SortMode = DataGridViewColumnSortMode.Programmatic
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColLatency",
                HeaderText = "Latency / State",
                DataPropertyName = nameof(ScanResult.LatencyString),
                FillWeight = 14,
                MinimumWidth = 105,
                SortMode = DataGridViewColumnSortMode.Programmatic
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColHostname",
                HeaderText = "Hostname (DNS)",
                DataPropertyName = nameof(ScanResult.HostnameDisplay),
                FillWeight = 20,
                MinimumWidth = 125,
                SortMode = DataGridViewColumnSortMode.Programmatic
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColNetBios",
                HeaderText = "NetBIOS Name",
                DataPropertyName = nameof(ScanResult.NetBiosDisplay),
                FillWeight = 20,
                MinimumWidth = 125,
                SortMode = DataGridViewColumnSortMode.Programmatic
            },
            new DataGridViewTextBoxColumn
            {
                Name = "ColUser",
                HeaderText = "Logged-in User (WMI)",
                DataPropertyName = nameof(ScanResult.LoggedInUserDisplay),
                FillWeight = 21,
                MinimumWidth = 135,
                SortMode = DataGridViewColumnSortMode.Programmatic
            }
        });

        gridResults.DataSource = _visibleResults;
        gridResults.CellFormatting += GridResults_CellFormatting;
        gridResults.MouseDown += GridResults_MouseDown;
        gridResults.ColumnHeaderMouseClick += GridResults_ColumnHeaderMouseClick;
        gridResults.DataBindingComplete += (s, e) => UpdateSortGlyphs();
    }

    private void GridResults_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= gridResults.Rows.Count) return;

        var row = gridResults.Rows[e.RowIndex];
        if (row.DataBoundItem is ScanResult result)
        {
            if (result.Status == HostStatus.Online)
            {
                row.DefaultCellStyle.BackColor = ColorOnlineBack;
                row.DefaultCellStyle.ForeColor = ColorOnlineFore;
                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 230, 201);
                row.DefaultCellStyle.SelectionForeColor = Color.Black;
            }
            else
            {
                row.DefaultCellStyle.BackColor = ColorOfflineBack;
                row.DefaultCellStyle.ForeColor = ColorOfflineFore;
                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 224, 224);
                row.DefaultCellStyle.SelectionForeColor = Color.Black;
            }
        }
    }

    private void TxtInput_TextChanged(object? sender, EventArgs e)
    {
        UpdateTargetCountPreview();
    }

    private void TxtInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true; // Prevent Windows error beep on Enter in single-line TextBox
            if (btnStart.Enabled)
            {
                btnStart_Click(btnStart, EventArgs.Empty);
            }
        }
    }

    private void UpdateTargetCountPreview()
    {
        string text = txtInput.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            lblTargetInfo.Text = "0 targets";
            return;
        }

        var parseResult = IpRangeParser.Parse(text);
        if (parseResult.Targets.Count > 0)
        {
            string duplicateNote = parseResult.DuplicateCount > 0 ? $" ({parseResult.DuplicateCount} duplicates filtered)" : string.Empty;
            lblTargetInfo.Text = $"{parseResult.Targets.Count:N0} targets ready{duplicateNote}";
            lblTargetInfo.ForeColor = Color.DarkGreen;
        }
        else
        {
            lblTargetInfo.Text = "Invalid IP or range";
            lblTargetInfo.ForeColor = Color.DarkRed;
        }
    }

    private async void btnStart_Click(object? sender, EventArgs e)
    {
        string rawInput = txtInput.Text.Trim();
        if (string.IsNullOrEmpty(rawInput))
        {
            MessageBox.Show(this, "Please enter an IP range (e.g. 192.168.1.1-254), IP address, or upload a file.",
                "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtInput.Focus();
            return;
        }

        var parseResult = IpRangeParser.Parse(rawInput);

        if (!parseResult.IsSuccess)
        {
            string errorDetails = string.Join(Environment.NewLine + "• ", parseResult.Errors);
            MessageBox.Show(this,
                $"Could not parse the IP input:\n\n• {errorDetails}\n\nSupported formats:\n- IP Range: 192.168.1.1-254\n- Single IP: 192.168.1.1\n- Subnet: 192.168.1.0/24\n- Comma-separated: 192.168.1.1, 192.168.1.50",
                "Input Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var targets = parseResult.Targets;

        // Large scan warning check
        if (targets.Count > _options.LargeScanWarningThreshold)
        {
            var confirm = MessageBox.Show(this,
                $"You are about to scan {targets.Count:N0} IP targets.\n\n" +
                $"Scanning large ranges requires more network time and bandwidth.\n" +
                $"Current Concurrency: {_options.Concurrency} threads | Timeout: {_options.TimeoutMs} ms.\n\n" +
                "Are you authorized to assess this network, and do you wish to proceed?",
                "Confirm Large Scan",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }
        }

        // By default, every scan report shows in IP address order wise
        _sortColumnName = ResultSorter.ColumnIp;
        _sortOrder = SortOrder.Ascending;
        UpdateSortGlyphs();

        // Reset previous state
        _allResults.Clear();
        _visibleResults.Clear();
        progressBar.Value = 0;
        progressBar.Maximum = targets.Count;

        btnStart.Enabled = false;
        btnCancel.Enabled = true;
        btnLoadFile.Enabled = false;
        btnExport.Enabled = false;
        txtInput.ReadOnly = true;

        _scanCts = new CancellationTokenSource();
        _stopwatch.Restart();
        _elapsedTimer?.Start();

        lblStatus.Text = $"Scanning {targets.Count:N0} targets...";

        var progress = new Progress<ScanProgressReport>(report =>
        {
            if (report.LatestResult != null)
            {
                _allResults.Add(report.LatestResult);
                if (CurrentFilterMode.Matches(report.LatestResult.Status))
                {
                    InsertResultSorted(report.LatestResult);
                }
            }

            progressBar.Value = Math.Min(report.ScannedCount, progressBar.Maximum);
            UpdateStatusLabel(report.ScannedCount, report.TotalTargets, report.OnlineCount, report.OfflineCount);
        });

        try
        {
            await _scanner.ScanAsync(targets, _options, progress, _scanCts.Token);
            RefreshFilteredView();
            int online = _allResults.Count(r => r.Status == HostStatus.Online);
            int offline = _allResults.Count(r => r.Status == HostStatus.Offline);
            lblStatus.Text = _scanCts.IsCancellationRequested
                ? $"Scan cancelled. Scanned {_allResults.Count}/{targets.Count} targets ({_visibleResults.Count} visible in {CurrentFilterMode.ToDisplayName()} view)."
                : $"Scan complete. {_allResults.Count} targets scanned in {_stopwatch.Elapsed:mm\\:ss} (Online: {online}, Offline: {offline} | Showing: {_visibleResults.Count} in {CurrentFilterMode.ToDisplayName()} view).";
        }
        catch (OperationCanceledException)
        {
            RefreshFilteredView();
            lblStatus.Text = $"Scan cancelled. {_allResults.Count} targets collected ({_visibleResults.Count} visible).";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"An unexpected error occurred during scanning: {ex.Message}", "Scan Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Scan error.";
        }
        finally
        {
            _stopwatch.Stop();
            _elapsedTimer?.Stop();
            btnStart.Enabled = true;
            btnCancel.Enabled = false;
            btnLoadFile.Enabled = true;
            btnExport.Enabled = _allResults.Count > 0;
            txtInput.ReadOnly = false;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    private void UpdateStatusLabel(int scanned, int total, int online, int offline)
    {
        double pct = total > 0 ? (double)scanned / total * 100.0 : 0.0;
        lblStatus.Text = $"Scanned: {scanned}/{total} ({pct:0}%) | Online: {online} | Offline: {offline} | View: {CurrentFilterMode.ToDisplayName()} ({_visibleResults.Count} visible)";
    }

    private void btnCancel_Click(object? sender, EventArgs e)
    {
        if (_scanCts != null && !_scanCts.IsCancellationRequested)
        {
            lblStatus.Text = "Stopping scan... Please wait.";
            btnCancel.Enabled = false;
            try
            {
                _scanCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape && _scanCts != null && !_scanCts.IsCancellationRequested)
        {
            btnCancel_Click(btnCancel, EventArgs.Empty);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        LayoutInputBox();
        UpdateSortGlyphs();
        AdjustColumnWidths();
        ActiveControl = txtInput;
        txtInput.Focus();
        txtInput.Select(txtInput.Text.Length, 0);
    }

    /// <summary>
    /// Centers the text inside the input box vertically so that top and bottom whitespace
    /// around typed text is identical and perfectly balanced.
    /// </summary>
    private void LayoutInputBox()
    {
        if (pnlInputWrapper == null || txtInput == null) return;

        // In Windows EDIT control with Segoe UI 10pt, text glyphs render starting ~5px below txtInput.Top
        // and ending ~13px below txtInput.Top (height 9px).
        // To achieve identical top and bottom spacing around the text:
        // spaceAbove = txtInput.Top + 5
        // spaceBelow = clientHeight - 1 - (txtInput.Top + 13) = clientHeight - 14 - txtInput.Top
        // Setting spaceAbove == spaceBelow gives txtInput.Top = (clientHeight - 19) / 2.
        // For clientHeight = 30, this sets Top = 5, yielding 10px above and 11px below (diff <= 1).
        int top = Math.Max(1, (pnlInputWrapper.ClientSize.Height - 20) / 2);
        txtInput.Top = top;
        txtInput.Left = 10;
        txtInput.Width = Math.Max(10, pnlInputWrapper.ClientSize.Width - 20);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        AdjustColumnWidths();
    }

    /// <summary>
    /// Updates column layout when needed. With DataGridViewAutoSizeColumnsMode.Fill enabled,
    /// the grid automatically distributes available width proportionally across all columns
    /// and avoids displaying an unnecessary horizontal scrollbar until the viewport becomes too narrow.
    /// </summary>
    public void AdjustColumnWidths()
    {
        if (gridResults == null || gridResults.Columns.Count == 0 || !IsHandleCreated) return;

        gridResults.PerformLayout();
        gridResults.Invalidate();
    }

    private async void btnLoadFile_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Upload / Import IP Targets from Text File",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            await ProcessLoadedFileAsync(ofd.FileName);
        }
    }

    private async Task ProcessLoadedFileAsync(string filePath)
    {
        var result = await FileInputParser.ParseFileAsync(filePath);
        if (!result.IsSuccess)
        {
            string msg = string.Join(Environment.NewLine + "• ", result.Errors);
            MessageBox.Show(this, $"Failed to process file:\n\n• {msg}", "File Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        txtInput.Text = string.Join(", ", result.Targets.Select(t => t.ToString()));

        var promptScan = MessageBox.Show(this,
            $"Successfully loaded {result.Targets.Count:N0} target IP address(es) from '{Path.GetFileName(filePath)}'.\n\n" +
            "Would you like to start scanning for live hosts now?",
            "Targets Uploaded",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (promptScan == DialogResult.Yes)
        {
            btnStart_Click(this, EventArgs.Empty);
        }
    }

    private void btnSettings_Click(object? sender, EventArgs e)
    {
        using var settingsForm = new SettingsForm(_options);
        settingsForm.ShowDialog(this);
    }

    private void btnAbout_Click(object? sender, EventArgs e)
    {
        using var aboutDialog = new AboutDialog();
        aboutDialog.ShowDialog(this);
    }

    private List<ScanResult> GetCurrentFilteredResults()
    {
        return _allResults.Where(r => CurrentFilterMode.Matches(r.Status))
                          .OrderBy(r => ScanTarget.ToUint32(r.Address))
                          .ToList();
    }

    private async void btnExport_Click(object? sender, EventArgs e)
    {
        var exportResults = GetCurrentFilteredResults();
        if (exportResults.Count == 0)
        {
            MessageBox.Show(this, $"No {CurrentFilterMode.ToDisplayName()} scan results available to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string filterSuffix = CurrentFilterMode switch
        {
            ResultFilterMode.Online => "_Online",
            ResultFilterMode.Offline => "_Offline",
            _ => "_Both"
        };

        using var sfd = new SaveFileDialog
        {
            Title = $"Export {CurrentFilterMode.ToDisplayName()} Scan Results",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx|Text Document (*.txt)|*.txt",
            FilterIndex = 1,
            DefaultExt = "xlsx",
            FileName = $"IPScan_Report{filterSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}",
            OverwritePrompt = true
        };

        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                IResultExporter exporter = Path.GetExtension(sfd.FileName).ToLowerInvariant() switch
                {
                    ".xlsx" => new ExcelExporter(),
                    _ => new TxtExporter()
                };

                await exporter.ExportAsync(sfd.FileName, exportResults);
                MessageBox.Show(this, $"Results successfully exported ({exportResults.Count:N0} records, {CurrentFilterMode.ToDisplayName()} view):\n{sfd.FileName}", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to export scan results:\n{ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// Inserts a newly scanned result in sorted order into the visible results list,
    /// ensuring that live scans display in IP address order (or custom order) from the first target.
    /// </summary>
    private void InsertResultSorted(ScanResult result)
    {
        var comparer = new ScanResultComparer(_sortColumnName, _sortOrder);
        int low = 0;
        int high = _visibleResults.Count - 1;
        int insertIndex = _visibleResults.Count;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            int cmp = comparer.Compare(result, _visibleResults[mid]);
            if (cmp < 0)
            {
                insertIndex = mid;
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }

        _visibleResults.Insert(insertIndex, result);
    }

    private void RefreshFilteredView()
    {
        var selectedIp = GetSelectedResult()?.Address;

        _visibleResults.RaiseListChangedEvents = false;
        _visibleResults.Clear();

        var matches = _allResults.Where(r => CurrentFilterMode.Matches(r.Status));
        var sorted = ResultSorter.Sort(matches, _sortColumnName, _sortOrder);

        foreach (var item in sorted)
        {
            _visibleResults.Add(item);
        }

        _visibleResults.RaiseListChangedEvents = true;
        _visibleResults.ResetBindings();
        UpdateSortGlyphs();
        AdjustColumnWidths();

        if (selectedIp != null)
        {
            for (int i = 0; i < gridResults.Rows.Count; i++)
            {
                if (gridResults.Rows[i].DataBoundItem is ScanResult r && r.Address.Equals(selectedIp))
                {
                    gridResults.ClearSelection();
                    gridResults.Rows[i].Selected = true;
                    break;
                }
            }
        }

        if (_allResults.Count > 0 && !_stopwatch.IsRunning)
        {
            int online = _allResults.Count(r => r.Status == HostStatus.Online);
            int offline = _allResults.Count(r => r.Status == HostStatus.Offline);
            lblStatus.Text = $"Showing {_visibleResults.Count} of {_allResults.Count} hosts (Filter: {CurrentFilterMode.ToDisplayName()} | Online: {online}, Offline: {offline}).";
        }
    }

    private void GridResults_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.ColumnIndex >= gridResults.Columns.Count) return;

        var column = gridResults.Columns[e.ColumnIndex];
        if (string.Equals(column.Name, _sortColumnName, StringComparison.OrdinalIgnoreCase))
        {
            _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
        }
        else
        {
            _sortColumnName = column.Name;
            _sortOrder = SortOrder.Ascending;
        }

        RefreshFilteredView();
    }

    private void UpdateSortGlyphs()
    {
        foreach (DataGridViewColumn col in gridResults.Columns)
        {
            col.HeaderCell.SortGlyphDirection = string.Equals(col.Name, _sortColumnName, StringComparison.OrdinalIgnoreCase)
                ? _sortOrder
                : SortOrder.None;
        }
    }

    private void GridResults_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            var hit = gridResults.HitTest(e.X, e.Y);
            // Only show context menu when right-clicking directly on an IP address row (cell), not on header or empty space
            if (hit.Type == DataGridViewHitTestType.Cell && hit.RowIndex >= 0 && hit.RowIndex < gridResults.Rows.Count)
            {
                gridResults.ClearSelection();
                gridResults.Rows[hit.RowIndex].Selected = true;
                contextMenu.Show(gridResults, e.Location);
            }
        }
    }

    private ScanResult? GetSelectedResult()
    {
        if (gridResults.SelectedRows.Count > 0)
        {
            return gridResults.SelectedRows[0].DataBoundItem as ScanResult;
        }
        return null;
    }

    private void menuHttp_Click(object? sender, EventArgs e)
    {
        var res = GetSelectedResult();
        if (res == null) return;
        try
        {
            ExternalToolLauncher.OpenHttp(res.Address);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to open browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void menuHttps_Click(object? sender, EventArgs e)
    {
        var res = GetSelectedResult();
        if (res == null) return;
        try
        {
            ExternalToolLauncher.OpenHttps(res.Address);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to open browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void menuVnc_Click(object? sender, EventArgs e)
    {
        var res = GetSelectedResult();
        if (res == null) return;

        if (!ExternalToolLauncher.LaunchVnc(res.Address, _options.CustomVncViewerPath, out string? errorMessage))
        {
            MessageBox.Show(this, errorMessage, "VNC Viewer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void menuExplorer_Click(object? sender, EventArgs e)
    {
        var res = GetSelectedResult();
        if (res == null) return;
        try
        {
            ExternalToolLauncher.OpenSharedFolders(res.Address);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to open shared folder: {ex.Message}", "Explorer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void menuCopyIp_Click(object? sender, EventArgs e)
    {
        var res = GetSelectedResult();
        if (res == null) return;
        Clipboard.SetText(res.IpString);
    }

    private void menuCopyRow_Click(object? sender, EventArgs e)
    {
        var res = GetSelectedResult();
        if (res == null) return;
        string line = $"{res.IpString}\t{res.AccessibleStatus}\t{res.LatencyString}\t{res.HostnameDisplay}\t{res.NetBiosDisplay}\t{res.LoggedInUserDisplay}";
        Clipboard.SetText(line);
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
