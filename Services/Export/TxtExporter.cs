using System.Text;
using IPScan.Models;

namespace IPScan.Services.Export;

/// <summary>
/// Exports scan results to a clean, human-readable text file with aligned columns.
/// </summary>
public sealed class TxtExporter : IResultExporter
{
    public string FileExtension => ".txt";
    public string FileFilter => "Text Document (*.txt)|*.txt";

    public async Task ExportAsync(string filePath, IReadOnlyList<ScanResult> results, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        int onlineCount = results.Count(r => r.Status == HostStatus.Online);
        int offlineCount = results.Count(r => r.Status == HostStatus.Offline);

        sb.AppendLine("=========================================================================================");
        sb.AppendLine("                               IPScan - Network Scan Report                              ");
        sb.AppendLine("=========================================================================================");
        sb.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Total Scanned Targets : {results.Count}");
        sb.AppendLine($"Online Hosts Found    : {onlineCount}");
        sb.AppendLine($"Offline / Unreachable : {offlineCount}");
        sb.AppendLine("-----------------------------------------------------------------------------------------");
        sb.AppendLine(string.Format("{0,-17} | {1,-10} | {2,-10} | {3,-24} | {4,-20} | {5}",
            "IP Address", "Status", "Latency", "Hostname", "NetBIOS Name", "Logged-in User"));
        sb.AppendLine("-----------------------------------------------------------------------------------------");

        foreach (var r in results)
        {
            string status = r.Status == HostStatus.Online ? "Online" : "Offline";
            string latency = r.Status == HostStatus.Online ? (r.RoundTripTimeMs >= 0 ? $"{r.RoundTripTimeMs} ms" : "<1 ms") : "-";
            string host = !string.IsNullOrWhiteSpace(r.Hostname) ? r.Hostname : "-";
            string netbios = !string.IsNullOrWhiteSpace(r.NetBiosName) ? r.NetBiosName : "-";
            string user = !string.IsNullOrWhiteSpace(r.LoggedInUser) ? r.LoggedInUser : "-";

            sb.AppendLine(string.Format("{0,-17} | {1,-10} | {2,-10} | {3,-24} | {4,-20} | {5}",
                r.IpString, status, latency, host, netbios, user));
        }

        sb.AppendLine("=========================================================================================");
        sb.AppendLine("End of report.");

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8, cancellationToken);
    }
}

