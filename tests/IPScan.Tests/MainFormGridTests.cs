using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using IPScan.Models;
using IPScan.Services.Scanning;
using IPScan.UI;
using Xunit;

namespace IPScan.Tests;

public class MainFormGridTests
{
    private static Control? FindControlRecursive(Control parent, System.Func<Control, bool> predicate)
    {
        if (predicate(parent)) return parent;
        foreach (Control child in parent.Controls)
        {
            var found = FindControlRecursive(child, predicate);
            if (found != null) return found;
        }
        return null;
    }

    private static void RunOnStaThread(System.Action action)
    {
        System.Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (System.Exception ex)
            {
                threadEx = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadEx != null)
        {
            throw new System.Exception("STA thread failed", threadEx);
        }
    }

    [Fact]
    public void Grid_DoesNotShowHorizontalScrollbar_AtDefaultSizeWhenEmpty()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            var dgv = FindControlRecursive(form, c => c is DataGridView) as DataGridView;
            Assert.NotNull(dgv);

            var hScroll = dgv.Controls.OfType<HScrollBar>().FirstOrDefault();
            Assert.NotNull(hScroll);
            Assert.False(hScroll.Visible, "Horizontal scrollbar should not be visible when columns fit within window bounds.");

            form.Close();
        });
    }

    [Fact]
    public void Grid_DoesNotShowHorizontalScrollbar_WhenRowsRequireVerticalScrollbar()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            var dgv = FindControlRecursive(form, c => c is DataGridView) as DataGridView;
            Assert.NotNull(dgv);

            var results = new BindingList<ScanResult>();
            for (int i = 1; i <= 60; i++)
            {
                results.Add(new ScanResult
                {
                    Address = System.Net.IPAddress.Parse($"192.168.1.{i}"),
                    Status = HostStatus.Online,
                    RoundTripTimeMs = 5,
                    Hostname = $"host-{i:D3}.local",
                    NetBiosName = $"HOST-{i:D3}",
                    LoggedInUser = $@"CORP\user{i:D3}"
                });
            }
            dgv.DataSource = results;
            Application.DoEvents();

            var vScroll = dgv.Controls.OfType<VScrollBar>().FirstOrDefault();
            var hScroll = dgv.Controls.OfType<HScrollBar>().FirstOrDefault();

            Assert.NotNull(vScroll);
            Assert.NotNull(hScroll);
            Assert.True(vScroll.Visible, "Vertical scrollbar should be visible when row count exceeds viewport.");
            Assert.False(hScroll.Visible, "Horizontal scrollbar should not appear just because vertical scrollbar appears.");

            form.Close();
        });
    }

    [Fact]
    public void Grid_DoesNotShowHorizontalScrollbar_WhenFormIsEnlargedOrMaximized()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Size = new Size(1600, 900);
            form.Show();
            Application.DoEvents();

            var dgv = FindControlRecursive(form, c => c is DataGridView) as DataGridView;
            Assert.NotNull(dgv);

            var hScroll = dgv.Controls.OfType<HScrollBar>().FirstOrDefault();
            Assert.NotNull(hScroll);
            Assert.False(hScroll.Visible, "Horizontal scrollbar should remain hidden on large displays.");

            form.Close();
        });
    }

    [Fact]
    public void Grid_ShowsHorizontalScrollbar_WhenViewportIsTooSmallToFitMinimumColumnWidths()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            // Lower MinimumSize temporarily to test narrow display behavior
            form.MinimumSize = Size.Empty;
            form.Size = new Size(500, 400);
            form.Show();
            Application.DoEvents();

            var dgv = FindControlRecursive(form, c => c is DataGridView) as DataGridView;
            Assert.NotNull(dgv);

            var hScroll = dgv.Controls.OfType<HScrollBar>().FirstOrDefault();
            Assert.NotNull(hScroll);
            Assert.True(hScroll.Visible, "Horizontal scrollbar must appear when necessary to view columns below minimum width.");

            form.Close();
        });
    }

    [Fact]
    public void Grid_SortOrderAndGlyphs_UpdateOnColumnHeaderSort()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            var dgv = FindControlRecursive(form, c => c is DataGridView) as DataGridView;
            Assert.NotNull(dgv);

            // Default is IP Ascending
            Assert.Equal("ColIp", form.CurrentSortColumn);
            Assert.Equal(SortOrder.Ascending, form.CurrentSortOrder);
            Assert.Equal(SortOrder.Ascending, dgv.Columns["ColIp"]!.HeaderCell.SortGlyphDirection);

            // Change to Hostname Ascending
            form.SetSortOrder("ColHostname", SortOrder.Ascending);
            Application.DoEvents();

            Assert.Equal("ColHostname", form.CurrentSortColumn);
            Assert.Equal(SortOrder.Ascending, form.CurrentSortOrder);
            Assert.Equal(SortOrder.Ascending, dgv.Columns["ColHostname"]!.HeaderCell.SortGlyphDirection);
            Assert.Equal(SortOrder.None, dgv.Columns["ColIp"]!.HeaderCell.SortGlyphDirection);

            // Toggle to Hostname Descending
            form.SetSortOrder("ColHostname", SortOrder.Descending);
            Application.DoEvents();

            Assert.Equal("ColHostname", form.CurrentSortColumn);
            Assert.Equal(SortOrder.Descending, form.CurrentSortOrder);
            Assert.Equal(SortOrder.Descending, dgv.Columns["ColHostname"]!.HeaderCell.SortGlyphDirection);

            form.Close();
        });
    }

    [Fact]
    public void Grid_OnLoad_DefaultsToIpAddressOrder()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            var dgv = FindControlRecursive(form, c => c is DataGridView) as DataGridView;
            Assert.NotNull(dgv);

            // Verify default on load is IP Address order wise
            Assert.Equal("ColIp", form.CurrentSortColumn);
            Assert.Equal(SortOrder.Ascending, form.CurrentSortOrder);
            Assert.Equal(SortOrder.Ascending, dgv.Columns["ColIp"]!.HeaderCell.SortGlyphDirection);

            form.Close();
        });
    }

    [Fact]
    public void ResultSorter_Compare_OrdersIpAddressesNumerically()
    {
        var r1 = new ScanResult { Address = System.Net.IPAddress.Parse("172.45.25.2") };
        var r2 = new ScanResult { Address = System.Net.IPAddress.Parse("172.45.25.10") };

        int cmpAsc = ResultSorter.Compare(r1, r2, ResultSorter.ColumnIp, SortOrder.Ascending);
        Assert.True(cmpAsc < 0, "172.45.25.2 should be ordered before 172.45.25.10 in Ascending IP order");

        int cmpDesc = ResultSorter.Compare(r1, r2, ResultSorter.ColumnIp, SortOrder.Descending);
        Assert.True(cmpDesc > 0, "172.45.25.2 should be ordered after 172.45.25.10 in Descending IP order");
    }

    [Fact]
    public void InputBox_TextUpperAndBottomSpace_IsEqual()
    {
        RunOnStaThread(() =>
        {
            using var form = new MainForm();
            form.Show();
            Application.DoEvents();

            var tb = FindControlRecursive(form, c => c is TextBox) as TextBox;
            Assert.NotNull(tb);
            var pnl = tb.Parent as Panel;
            Assert.NotNull(pnl);

            tb.Text = "172.45.25.10";
            Application.DoEvents();

            using var bmp = new Bitmap(pnl.Width, pnl.Height);
            pnl.DrawToBitmap(bmp, new Rectangle(0, 0, pnl.Width, pnl.Height));

            int minY = int.MaxValue, maxY = -1;
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = tb.Left; x < tb.Left + 100; x++)
                {
                    var px = bmp.GetPixel(x, y);
                    if (px.R < 100 && px.G < 100 && px.B < 100)
                    {
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            int spaceAbove = minY;
            int spaceBelow = pnl.Height - 1 - maxY;
            int diff = System.Math.Abs(spaceAbove - spaceBelow);

            Assert.True(diff <= 1, $"Space above ({spaceAbove}px) and space below ({spaceBelow}px) must be equal (diff={diff}px)");

            form.Close();
        });
    }
}
