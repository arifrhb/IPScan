namespace IPScan.Models;

/// <summary>
/// Specifies the display filter for scan results in the view.
/// </summary>
public enum ResultFilterMode
{
    Both = 0,
    Online = 1,
    Offline = 2
}

public static class ResultFilterExtensions
{
    public static bool Matches(this ResultFilterMode filter, HostStatus status) => filter switch
    {
        ResultFilterMode.Online => status == HostStatus.Online,
        ResultFilterMode.Offline => status != HostStatus.Online, // Includes Offline and Error
        _ => true // Both: includes all hosts
    };

    public static string ToDisplayName(this ResultFilterMode filter) => filter switch
    {
        ResultFilterMode.Both => "Both",
        ResultFilterMode.Online => "Online",
        ResultFilterMode.Offline => "Offline",
        _ => "Both"
    };

    public static string ToDetailedName(this ResultFilterMode filter, int onlineCount, int offlineCount) => filter switch
    {
        ResultFilterMode.Both => $"Both ({onlineCount + offlineCount})",
        ResultFilterMode.Online => $"Online ({onlineCount})",
        ResultFilterMode.Offline => $"Offline ({offlineCount})",
        _ => "Both"
    };
}

