using MiniExcelLibs;
using IPScan.Models;

namespace IPScan.Services.Export;

/// <summary>
/// Exports scan results to a native Microsoft Excel (.xlsx) file using the lightweight MiniExcel library.
/// Requires zero Office/COM interop dependencies and has minimal memory overhead.
/// </summary>
public sealed class ExcelExporter : IResultExporter
{
    public string FileExtension => ".xlsx";
    public string FileFilter => "Excel Workbook (*.xlsx)|*.xlsx";

    public async Task ExportAsync(string filePath, IReadOnlyList<ScanResult> results, CancellationToken cancellationToken = default)
    {
        var exportRows = results.Select(r => new
        {
            IPAddress = r.IpString,
            Status = r.Status == HostStatus.Online ? "Online" : "Offline",
            RoundTripTime = r.Status == HostStatus.Online ? (r.RoundTripTimeMs >= 0 ? $"{r.RoundTripTimeMs} ms" : "< 1 ms") : "N/A",
            Hostname = !string.IsNullOrWhiteSpace(r.Hostname) ? r.Hostname : "-",
            NetBIOSName = !string.IsNullOrWhiteSpace(r.NetBiosName) ? r.NetBiosName : "-",
            LoggedInUser = !string.IsNullOrWhiteSpace(r.LoggedInUser) ? r.LoggedInUser : "-",
            StatusMessage = r.StatusMessage,
            Timestamp = r.ScannedAt.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();

        await MiniExcel.SaveAsAsync(filePath, exportRows, overwriteFile: true, cancellationToken: cancellationToken);
    }
}

