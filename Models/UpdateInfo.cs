namespace IPScan.Models;

/// <summary>
/// Identifies whether the running binary is framework-dependent (lightweight) or self-contained (standalone).
/// </summary>
public enum BuildFlavor
{
    Lite,
    Standalone
}

public static class BuildFlavorExtensions
{
    public static string ToDisplayName(this BuildFlavor flavor) => flavor switch
    {
        BuildFlavor.Lite => "Lightweight (Framework-Dependent)",
        BuildFlavor.Standalone => "Standalone (Self-Contained)",
        _ => "Lightweight"
    };

    public static string ToPreferredAssetName(this BuildFlavor flavor) => flavor switch
    {
        BuildFlavor.Lite => "IPScan-win-x64-lite.exe",
        BuildFlavor.Standalone => "IPScan-win-x64-standalone.exe",
        _ => "IPScan-win-x64-lite.exe"
    };
}

/// <summary>
/// Metadata describing the latest GitHub release and whether an update is available.
/// </summary>
public sealed record UpdateInfo(
    Version CurrentVersion,
    Version LatestVersion,
    string TagName,
    string ReleaseName,
    string ReleaseNotes,
    string ReleasePageUrl,
    string AssetName,
    string DownloadUrl,
    long AssetSizeBytes,
    string? ExpectedSha256,
    BuildFlavor BuildFlavor,
    bool IsUpdateAvailable)
{
    public string FormattedAssetSize
    {
        get
        {
            if (AssetSizeBytes <= 0) return "Unknown size";
            double mb = AssetSizeBytes / (1024.0 * 1024.0);
            if (mb >= 1.0) return $"{mb:F1} MB";
            double kb = AssetSizeBytes / 1024.0;
            return $"{kb:F0} KB";
        }
    }
}

/// <summary>
/// Progress report emitted while downloading an update package.
/// </summary>
public sealed record UpdateDownloadProgress(long BytesReceived, long? TotalBytes)
{
    public int? Percentage => TotalBytes.HasValue && TotalBytes.Value > 0
        ? (int)Math.Clamp(BytesReceived * 100L / TotalBytes.Value, 0L, 100L)
        : null;
}

