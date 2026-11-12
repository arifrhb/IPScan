using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IPScan.Models;

namespace IPScan.Services.Updates;

/// <summary>
/// Checks GitHub Releases for newer IPScan builds, downloads and verifies updates,
/// and applies in-place self-replacement for standalone/lightweight single-file executables.
/// </summary>
public sealed class UpdateService : IDisposable
{
    public const string GitHubOwner = "arifrhb";
    public const string GitHubRepo = "IPScan";
    public const string ReleasesPageUrl = $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases";
    public const string LatestReleaseApiUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public UpdateService(HttpClient? httpClient = null)
    {
        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            _ownsHttpClient = true;
        }

        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"IPScan/{GetCurrentVersionString()}");
        }
    }

    public static Version GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var infoAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (infoAttr != null && TryParseSemanticVersion(infoAttr.InformationalVersion, out var parsedInfo, out _) && parsedInfo != null)
        {
            return parsedInfo;
        }

        var asmVer = assembly.GetName().Version;
        if (asmVer != null)
        {
            return NormalizeVersion(asmVer);
        }

        return new Version(1, 0, 0);
    }

    public static string GetCurrentVersionString()
    {
        var v = GetCurrentVersion();
        return $"{v.Major}.{v.Minor}.{Math.Max(0, v.Build)}";
    }

    /// <summary>
    /// Detects whether the current executable is running as a lightweight (framework-dependent)
    /// or standalone (self-contained) build.
    /// </summary>
    public static BuildFlavor DetectCurrentBuildFlavor()
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var meta in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (string.Equals(meta.Key, "IPScanBuildFlavor", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(meta.Value, "standalone", StringComparison.OrdinalIgnoreCase))
                    return BuildFlavor.Standalone;
                if (string.Equals(meta.Value, "lite", StringComparison.OrdinalIgnoreCase))
                    return BuildFlavor.Lite;
            }
        }

        try
        {
            string runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
            string sharedMarker = $"{Path.DirectorySeparatorChar}shared{Path.DirectorySeparatorChar}Microsoft.NETCore.App";
            if (runtimeDir.Contains(sharedMarker, StringComparison.OrdinalIgnoreCase))
            {
                return BuildFlavor.Lite;
            }
        }
        catch
        {
            // Fall through to default
        }

        return BuildFlavor.Standalone;
    }

    /// <summary>
    /// Queries GitHub Releases for the latest stable release.
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdateAsync(
        Version? currentVersionOverride = null,
        BuildFlavor? flavorOverride = null,
        CancellationToken cancellationToken = default)
    {
        Version currentVersion = currentVersionOverride != null
            ? NormalizeVersion(currentVersionOverride)
            : GetCurrentVersion();
        BuildFlavor flavor = flavorOverride ?? DetectCurrentBuildFlavor();

        using var checkCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        checkCts.CancelAfter(TimeSpan.FromSeconds(15));

        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApiUrl);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, checkCts.Token);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync(checkCts.Token);

        var parsed = ParseReleaseJson(json, currentVersion, flavor, out string? checksumUrl);
        if (parsed == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(checksumUrl) && IsTrustedHttpsUrl(checksumUrl))
        {
            try
            {
                using var sumRequest = new HttpRequestMessage(HttpMethod.Get, checksumUrl);
                using var sumResponse = await _httpClient.SendAsync(sumRequest, checkCts.Token);
                if (sumResponse.IsSuccessStatusCode)
                {
                    string sumsText = await sumResponse.Content.ReadAsStringAsync(checkCts.Token);
                    string? sha256 = ParseSha256Sums(sumsText, parsed.AssetName);
                    if (!string.IsNullOrWhiteSpace(sha256))
                    {
                        parsed = parsed with { ExpectedSha256 = sha256 };
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Non-fatal: release may not have readable SHA256SUMS.txt
            }
        }

        return parsed;
    }

    /// <summary>
    /// Downloads the update asset into a temporary directory and validates its PE header and SHA-256 checksum.
    /// </summary>
    public async Task<string> DownloadAndVerifyUpdateAsync(
        UpdateInfo update,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (!IsTrustedHttpsUrl(update.DownloadUrl))
        {
            throw new InvalidOperationException("Update download URL must use HTTPS.");
        }

        string safeFileName = Path.GetFileName(update.AssetName);
        if (string.IsNullOrWhiteSpace(safeFileName) || !safeFileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            safeFileName = "IPScan-update.exe";
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "IPScanUpdate", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string destinationPath = Path.Combine(tempDir, safeFileName);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, update.DownloadUrl);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength ?? (update.AssetSizeBytes > 0 ? update.AssetSizeBytes : null);

            await using (var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                byte[] buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    totalRead += bytesRead;
                    progress?.Report(new UpdateDownloadProgress(totalRead, totalBytes));
                }
            }

            if (!IsPeExecutableHeaderValid(destinationPath))
            {
                throw new InvalidDataException("Downloaded file is not a valid Windows executable.");
            }

            if (!string.IsNullOrWhiteSpace(update.ExpectedSha256))
            {
                bool hashValid = await VerifyFileSha256Async(destinationPath, update.ExpectedSha256, cancellationToken);
                if (!hashValid)
                {
                    throw new InvalidDataException("SHA-256 verification failed for the downloaded update.");
                }
            }

            return destinationPath;
        }
        catch
        {
            TryDeleteDirectory(tempDir);
            throw;
        }
    }

    /// <summary>
    /// Schedules a detached PowerShell updater that waits for the current process to exit,
    /// swaps the executable in-place, cleans up temp files, and starts the updated application.
    /// </summary>
    public static void ApplyUpdateAndRestart(string verifiedExePath, string? targetExePathOverride = null)
    {
        if (string.IsNullOrWhiteSpace(verifiedExePath) || !File.Exists(verifiedExePath))
        {
            throw new FileNotFoundException("Verified update file was not found.", verifiedExePath);
        }

        string? targetExePath = targetExePathOverride ?? Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(targetExePath) || !File.Exists(targetExePath))
        {
            throw new InvalidOperationException("Unable to determine the current executable path for self-update.");
        }

        if (targetExePath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("In-place self-update is only supported when running the compiled IPScan.exe executable.");
        }

        string tempDir = Path.GetDirectoryName(verifiedExePath) ?? Path.GetTempPath();
        string scriptPath = Path.Combine(tempDir, "apply_update.ps1");
        string scriptContent = BuildUpdaterPowerShellScript(Environment.ProcessId, verifiedExePath, targetExePath, tempDir);
        File.WriteAllText(scriptPath, scriptContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        string targetDir = Path.GetDirectoryName(targetExePath) ?? string.Empty;
        bool requiresElevation = !string.IsNullOrEmpty(targetDir) && !CanWriteToDirectory(targetDir);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        if (requiresElevation)
        {
            psi.Verb = "runas";
        }

        Process.Start(psi);
    }

    /// <summary>
    /// Parses a GitHub Releases API JSON response and selects the matching release asset.
    /// </summary>
    public static UpdateInfo? ParseReleaseJson(
        string json,
        Version currentVersion,
        BuildFlavor flavor,
        out string? checksumAssetUrl)
    {
        checksumAssetUrl = null;
        if (string.IsNullOrWhiteSpace(json)) return null;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("draft", out var draftProp) && draftProp.GetBoolean())
        {
            return null;
        }

        bool isReleasePrerelease = root.TryGetProperty("prerelease", out var preProp) && preProp.GetBoolean();
        if (isReleasePrerelease)
        {
            return null;
        }

        string tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? string.Empty : string.Empty;
        if (!TryParseSemanticVersion(tagName, out Version? latestVersion, out bool tagPrerelease) || latestVersion == null)
        {
            return null;
        }

        string releaseName = root.TryGetProperty("name", out var nameProp) && !string.IsNullOrWhiteSpace(nameProp.GetString())
            ? nameProp.GetString()!
            : tagName;
        string releaseNotes = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? string.Empty : string.Empty;
        string htmlUrl = root.TryGetProperty("html_url", out var urlProp) && !string.IsNullOrWhiteSpace(urlProp.GetString())
            ? urlProp.GetString()!
            : ReleasesPageUrl;

        if (!root.TryGetProperty("assets", out var assetsProp) || assetsProp.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var assets = new List<(string Name, string Url, long Size)>();
        foreach (var asset in assetsProp.EnumerateArray())
        {
            string? name = asset.TryGetProperty("name", out var aName) ? aName.GetString() : null;
            string? url = asset.TryGetProperty("browser_download_url", out var aUrl) ? aUrl.GetString() : null;
            long size = asset.TryGetProperty("size", out var aSize) && aSize.TryGetInt64(out long s) ? s : 0L;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) continue;

            if (string.Equals(name, "SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase))
            {
                checksumAssetUrl = url;
                continue;
            }

            assets.Add((name, url, size));
        }

        var selected = SelectBestExecutableAsset(assets, flavor);
        if (selected == null)
        {
            return null;
        }

        bool updateAvailable = IsNewerVersion(currentVersion, latestVersion, tagPrerelease);

        return new UpdateInfo(
            CurrentVersion: NormalizeVersion(currentVersion),
            LatestVersion: latestVersion,
            TagName: tagName,
            ReleaseName: releaseName,
            ReleaseNotes: releaseNotes,
            ReleasePageUrl: htmlUrl,
            AssetName: selected.Value.Name,
            DownloadUrl: selected.Value.Url,
            AssetSizeBytes: selected.Value.Size,
            ExpectedSha256: null,
            BuildFlavor: flavor,
            IsUpdateAvailable: updateAvailable);
    }

    internal static (string Name, string Url, long Size)? SelectBestExecutableAsset(
        IReadOnlyList<(string Name, string Url, long Size)> assets,
        BuildFlavor flavor)
    {
        var exeAssets = assets
            .Where(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (exeAssets.Count == 0) return null;

        string preferredExact = flavor.ToPreferredAssetName();
        var exact = exeAssets.FirstOrDefault(a => string.Equals(a.Name, preferredExact, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(exact.Name)) return exact;

        string[] flavorKeywords = flavor == BuildFlavor.Lite
            ? ["lite", "framework-dependent", "framework_dependent"]
            : ["standalone", "self-contained", "self_contained"];

        foreach (string kw in flavorKeywords)
        {
            var match = exeAssets.FirstOrDefault(a => a.Name.Contains(kw, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(match.Name)) return match;
        }

        // Fallback to plain IPScan.exe if only a single generic exe was uploaded
        var generic = exeAssets.FirstOrDefault(a => string.Equals(a.Name, "IPScan.exe", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(generic.Name)) return generic;

        return exeAssets.Count == 1 ? exeAssets[0] : null;
    }

    public static bool TryParseSemanticVersion(string? raw, out Version? version, out bool isPrerelease)
    {
        version = null;
        isPrerelease = false;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        string cleaned = raw.Trim();
        // Strip informational suffix like "1.2.1 (Release Build)" or metadata "+commit"
        int spaceIdx = cleaned.IndexOf(' ');
        if (spaceIdx > 0) cleaned = cleaned[..spaceIdx];

        int plusIdx = cleaned.IndexOf('+');
        if (plusIdx > 0) cleaned = cleaned[..plusIdx];

        if (cleaned.StartsWith('v') || cleaned.StartsWith('V'))
        {
            cleaned = cleaned[1..];
        }

        int dashIdx = cleaned.IndexOf('-');
        if (dashIdx > 0)
        {
            isPrerelease = true;
            cleaned = cleaned[..dashIdx];
        }

        string[] parts = cleaned.Split('.');
        if (parts.Length < 2 || parts.Length > 4) return false;

        if (!int.TryParse(parts[0], out int major) || major < 0) return false;
        if (!int.TryParse(parts[1], out int minor) || minor < 0) return false;

        int build = 0;
        if (parts.Length >= 3 && (!int.TryParse(parts[2], out build) || build < 0)) return false;

        int revision = 0;
        if (parts.Length == 4 && (!int.TryParse(parts[3], out revision) || revision < 0)) return false;

        version = parts.Length == 4 && revision > 0
            ? new Version(major, minor, build, revision)
            : new Version(major, minor, build);
        return true;
    }

    public static bool IsNewerVersion(Version current, Version latest, bool latestIsPrerelease = false)
    {
        if (latestIsPrerelease) return false;
        return NormalizeVersion(latest) > NormalizeVersion(current);
    }

    private static Version NormalizeVersion(Version v)
    {
        int build = Math.Max(0, v.Build);
        int rev = Math.Max(0, v.Revision);
        return rev > 0 ? new Version(v.Major, v.Minor, build, rev) : new Version(v.Major, v.Minor, build);
    }

    public static string? ParseSha256Sums(string content, string targetAssetName)
    {
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(targetAssetName))
            return null;

        string targetFileName = Path.GetFileName(targetAssetName.Trim());
        foreach (string rawLine in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string line = rawLine.Trim();
            if (line.Length < 66 || line.StartsWith('#')) continue;

            int firstSpace = line.IndexOfAny([' ', '\t']);
            if (firstSpace != 64) continue;

            string hash = line[..64];
            if (!IsHexString(hash)) continue;

            string filePart = line[(firstSpace + 1)..].Trim().TrimStart('*');
            string fileName = Path.GetFileName(filePart);
            if (string.Equals(fileName, targetFileName, StringComparison.OrdinalIgnoreCase))
            {
                return hash.ToLowerInvariant();
            }
        }

        return null;
    }

    public static async Task<bool> VerifyFileSha256Async(string filePath, string expectedSha256, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath) || string.IsNullOrWhiteSpace(expectedSha256))
            return false;

        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        byte[] hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        string actual = Convert.ToHexString(hashBytes);
        return string.Equals(actual, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPeExecutableHeaderValid(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 64) return false;
            return fs.ReadByte() == 'M' && fs.ReadByte() == 'Z';
        }
        catch
        {
            return false;
        }
    }

    public static bool CanWriteToDirectory(string directoryPath)
    {
        try
        {
            string probe = Path.Combine(directoryPath, $".ipscan_write_test_{Guid.NewGuid():N}.tmp");
            using (File.Create(probe)) { }
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string BuildUpdaterPowerShellScript(int targetPid, string downloadedExePath, string targetExePath, string tempDirPath)
    {
        string Esc(string s) => s.Replace("'", "''");

        var sb = new StringBuilder();
        sb.AppendLine("$ErrorActionPreference = 'Stop'");
        sb.AppendLine($"$targetPid = {targetPid}");
        sb.AppendLine($"$source = '{Esc(downloadedExePath)}'");
        sb.AppendLine($"$dest = '{Esc(targetExePath)}'");
        sb.AppendLine($"$tempDir = '{Esc(tempDirPath)}'");
        sb.AppendLine("$backup = \"$dest.bak\"");
        sb.AppendLine();
        sb.AppendLine("for ($i = 0; $i -lt 60; $i++) {");
        sb.AppendLine("    $proc = Get-Process -Id $targetPid -ErrorAction SilentlyContinue");
        sb.AppendLine("    if (-not $proc) { break }");
        sb.AppendLine("    Start-Sleep -Milliseconds 500");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("$replaced = $false");
        sb.AppendLine("for ($attempt = 0; $attempt -lt 15; $attempt++) {");
        sb.AppendLine("    try {");
        sb.AppendLine("        if (Test-Path -LiteralPath $dest) {");
        sb.AppendLine("            Move-Item -LiteralPath $dest -Destination $backup -Force");
        sb.AppendLine("        }");
        sb.AppendLine("        Copy-Item -LiteralPath $source -Destination $dest -Force");
        sb.AppendLine("        if (Test-Path -LiteralPath $backup) {");
        sb.AppendLine("            Remove-Item -LiteralPath $backup -Force -ErrorAction SilentlyContinue");
        sb.AppendLine("        }");
        sb.AppendLine("        $replaced = $true");
        sb.AppendLine("        break");
        sb.AppendLine("    } catch {");
        sb.AppendLine("        if ((Test-Path -LiteralPath $backup) -and (-not (Test-Path -LiteralPath $dest))) {");
        sb.AppendLine("            Move-Item -LiteralPath $backup -Destination $dest -Force -ErrorAction SilentlyContinue");
        sb.AppendLine("        }");
        sb.AppendLine("        Start-Sleep -Milliseconds 500");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("if ($replaced) {");
        sb.AppendLine("    Start-Process -FilePath $dest");
        sb.AppendLine("}");
        sb.AppendLine("Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue");
        return sb.ToString();
    }

    private static bool IsTrustedHttpsUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static bool IsHexString(string value)
    {
        foreach (char c in value)
        {
            bool isHex = (c >= '0' && c <= '9') ||
                         (c >= 'a' && c <= 'f') ||
                         (c >= 'A' && c <= 'F');
            if (!isHex) return false;
        }
        return true;
    }

    private static void TryDeleteDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}

