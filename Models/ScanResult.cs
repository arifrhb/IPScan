using System.Net;

namespace IPScan.Models;

public enum HostStatus
{
    Online,
    Offline,
    Error
}

/// <summary>
/// Contains the discovery outcome for an individual IP target.
/// </summary>
public sealed class ScanResult
{
    public IPAddress Address { get; init; } = IPAddress.None;
    public HostStatus Status { get; init; } = HostStatus.Offline;
    public long RoundTripTimeMs { get; init; } = -1;
    public string? Hostname { get; init; }
    public string? NetBiosName { get; init; }
    public string? LoggedInUser { get; init; }
    public string StatusMessage { get; init; } = string.Empty;
    public DateTime ScannedAt { get; init; } = DateTime.Now;

    public string IpString => Address.ToString();

    /// <summary>
    /// Explicit, accessible status indicator that conveys state clearly in text,
    /// so the UI does not rely on color alone.
    /// </summary>
    public string AccessibleStatus => Status switch
    {
        HostStatus.Online => "[+] Online",
        HostStatus.Offline => "[-] Offline",
        HostStatus.Error => "[!] Error",
        _ => "Unknown"
    };

    public string LatencyString => Status == HostStatus.Online
        ? (RoundTripTimeMs >= 0 ? $"{RoundTripTimeMs} ms" : "< 1 ms")
        : (string.IsNullOrWhiteSpace(StatusMessage) ? "No response" : StatusMessage);

    public string HostnameDisplay => !string.IsNullOrWhiteSpace(Hostname) ? Hostname : "-";
    public string NetBiosDisplay => !string.IsNullOrWhiteSpace(NetBiosName) ? NetBiosName : "-";
    public string LoggedInUserDisplay => !string.IsNullOrWhiteSpace(LoggedInUser) ? LoggedInUser : "-";
}

