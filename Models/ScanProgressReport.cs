namespace IPScan.Models;

/// <summary>
/// Status update passed during an ongoing asynchronous scan.
/// </summary>
public sealed class ScanProgressReport
{
    public int TotalTargets { get; init; }
    public int ScannedCount { get; init; }
    public int OnlineCount { get; init; }
    public int OfflineCount { get; init; }
    public ScanResult? LatestResult { get; init; }
    public double ProgressPercentage => TotalTargets > 0 ? (double)ScannedCount / TotalTargets * 100.0 : 0.0;
}

