using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using IPScan.Models;

namespace IPScan.Services.Scanning;

/// <summary>
/// Provides high-performance, deterministic sorting for scan results across all table columns.
/// </summary>
public static class ResultSorter
{
    public const string ColumnStatus = "ColStatus";
    public const string ColumnIp = "ColIp";
    public const string ColumnLatency = "ColLatency";
    public const string ColumnHostname = "ColHostname";
    public const string ColumnNetBios = "ColNetBios";
    public const string ColumnUser = "ColUser";

    /// <summary>
    /// Compares two scan results based on the chosen column and direction.
    /// </summary>
    public static int Compare(ScanResult? x, ScanResult? y, string? columnName, SortOrder sortOrder)
    {
        var comparer = new ScanResultComparer(columnName, sortOrder);
        return comparer.Compare(x, y);
    }

    /// <summary>
    /// Sorts scan results by the specified column name and direction.
    /// </summary>
    public static IEnumerable<ScanResult> Sort(
        IEnumerable<ScanResult> source,
        string? columnName,
        SortOrder sortOrder)
    {
        if (source == null) return Enumerable.Empty<ScanResult>();
        var comparer = new ScanResultComparer(columnName, sortOrder);
        return source.OrderBy(r => r, comparer);
    }
}

/// <summary>
/// Implements deterministic IComparer for ScanResult.
/// </summary>
public sealed class ScanResultComparer : IComparer<ScanResult>
{
    private readonly string _columnName;
    private readonly SortOrder _sortOrder;

    public ScanResultComparer(string? columnName, SortOrder sortOrder)
    {
        _columnName = string.IsNullOrWhiteSpace(columnName) ? ResultSorter.ColumnIp : columnName;
        _sortOrder = sortOrder == SortOrder.None ? SortOrder.Ascending : sortOrder;
    }

    public int Compare(ScanResult? x, ScanResult? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return 1;
        if (y is null) return -1;

        bool desc = _sortOrder == SortOrder.Descending;

        switch (_columnName)
        {
            case ResultSorter.ColumnStatus:
            {
                int cmp = x.Status.CompareTo(y.Status);
                if (cmp != 0)
                {
                    return desc ? -cmp : cmp;
                }
                return ScanTarget.ToUint32(x.Address).CompareTo(ScanTarget.ToUint32(y.Address));
            }

            case ResultSorter.ColumnIp:
            {
                int cmp = ScanTarget.ToUint32(x.Address).CompareTo(ScanTarget.ToUint32(y.Address));
                return desc ? -cmp : cmp;
            }

            case ResultSorter.ColumnLatency:
            {
                bool xOnline = x.Status == HostStatus.Online && x.RoundTripTimeMs >= 0;
                bool yOnline = y.Status == HostStatus.Online && y.RoundTripTimeMs >= 0;

                if (xOnline && yOnline)
                {
                    int latCmp = x.RoundTripTimeMs.CompareTo(y.RoundTripTimeMs);
                    if (latCmp != 0)
                    {
                        return desc ? -latCmp : latCmp;
                    }
                }
                else if (xOnline && !yOnline)
                {
                    return -1;
                }
                else if (!xOnline && yOnline)
                {
                    return 1;
                }

                return ScanTarget.ToUint32(x.Address).CompareTo(ScanTarget.ToUint32(y.Address));
            }

            case ResultSorter.ColumnHostname:
                return CompareStringColumn(x.Hostname, y.Hostname, x, y, desc);

            case ResultSorter.ColumnNetBios:
                return CompareStringColumn(x.NetBiosName, y.NetBiosName, x, y, desc);

            case ResultSorter.ColumnUser:
                return CompareStringColumn(x.LoggedInUser, y.LoggedInUser, x, y, desc);

            default:
            {
                int defaultCmp = ScanTarget.ToUint32(x.Address).CompareTo(ScanTarget.ToUint32(y.Address));
                return desc ? -defaultCmp : defaultCmp;
            }
        }
    }

    private static int CompareStringColumn(string? strX, string? strY, ScanResult x, ScanResult y, bool desc)
    {
        bool xHas = !string.IsNullOrWhiteSpace(strX);
        bool yHas = !string.IsNullOrWhiteSpace(strY);

        if (xHas && yHas)
        {
            int strCmp = string.Compare(strX, strY, StringComparison.OrdinalIgnoreCase);
            if (strCmp != 0)
            {
                return desc ? -strCmp : strCmp;
            }
        }
        else if (xHas && !yHas)
        {
            return -1;
        }
        else if (!xHas && yHas)
        {
            return 1;
        }

        return ScanTarget.ToUint32(x.Address).CompareTo(ScanTarget.ToUint32(y.Address));
    }
}
