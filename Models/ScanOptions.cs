namespace IPScan.Models;

/// <summary>
/// User-configurable options controlling scan performance and discovery modules.
/// </summary>
public sealed class ScanOptions
{
    /// <summary>
    /// Maximum concurrent discovery operations.
    /// Default is 50 to balance speed and network socket usage.
    /// </summary>
    public int Concurrency { get; set; } = 50;

    /// <summary>
    /// Ping timeout in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Whether to perform reverse DNS lookups for online hosts.
    /// </summary>
    public bool ResolveDns { get; set; } = true;

    /// <summary>
    /// Whether to perform RFC 1001/1002 NetBIOS Name Service status queries on port 137.
    /// </summary>
    public bool ResolveNetBios { get; set; } = true;

    /// <summary>
    /// Whether to query logged-in username via authorized Windows discovery methods (WMI, NetAPI, and NetBIOS).
    /// Enabled by default.
    /// </summary>
    public bool QueryLoggedInUser { get; set; } = true;

    /// <summary>
    /// Number of targets that prompts a safety confirmation dialog.
    /// </summary>
    public int LargeScanWarningThreshold { get; set; } = 256;

    /// <summary>
    /// Configured path to a custom VNC viewer executable.
    /// </summary>
    public string? CustomVncViewerPath { get; set; }
}
