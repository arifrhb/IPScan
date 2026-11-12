using System.Net;
using System.Security.Cryptography;
using System.Text;
using IPScan.Models;
using IPScan.Services.Updates;
using Xunit;

namespace IPScan.Tests;

public sealed class UpdateServiceTests
{
    [Theory]
    [InlineData("v1.2.2", 1, 2, 2, false)]
    [InlineData("1.2.2", 1, 2, 2, false)]
    [InlineData("V2.0.10", 2, 0, 10, false)]
    [InlineData("1.2.1 (Release Build)", 1, 2, 1, false)]
    [InlineData("v1.3.0+abc123", 1, 3, 0, false)]
    [InlineData("v1.3.0-beta.1", 1, 3, 0, true)]
    public void TryParseSemanticVersion_ParsesValidVersions(string input, int major, int minor, int patch, bool expectedPrerelease)
    {
        bool ok = UpdateService.TryParseSemanticVersion(input, out Version? version, out bool isPrerelease);

        Assert.True(ok);
        Assert.NotNull(version);
        Assert.Equal(major, version!.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(patch, version.Build);
        Assert.Equal(expectedPrerelease, isPrerelease);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("latest")]
    [InlineData("v1")]
    public void TryParseSemanticVersion_RejectsInvalidInputs(string? input)
    {
        bool ok = UpdateService.TryParseSemanticVersion(input, out Version? version, out _);
        Assert.False(ok);
        Assert.Null(version);
    }

    [Theory]
    [InlineData("1.2.1", "1.2.2", false, true)]
    [InlineData("1.2.1", "1.3.0", false, true)]
    [InlineData("1.2.1", "2.0.0", false, true)]
    [InlineData("1.2.1", "1.2.1", false, false)]
    [InlineData("1.2.2", "1.2.1", false, false)]
    [InlineData("1.2.1", "1.2.2", true, false)]
    public void IsNewerVersion_ComparesCorrectly(string currentStr, string latestStr, bool prerelease, bool expected)
    {
        var current = Version.Parse(currentStr);
        var latest = Version.Parse(latestStr);
        Assert.Equal(expected, UpdateService.IsNewerVersion(current, latest, prerelease));
    }

    [Theory]
    [InlineData(BuildFlavor.Lite, "IPScan-win-x64-lite.exe")]
    [InlineData(BuildFlavor.Standalone, "IPScan-win-x64-standalone.exe")]
    public void ParseReleaseJson_SelectsMatchingFlavorAssetAndExtractsMetadata(BuildFlavor flavor, string expectedAsset)
    {
        string json = """
        {
          "tag_name": "v1.2.2",
          "name": "IPScan v1.2.2",
          "draft": false,
          "prerelease": false,
          "html_url": "https://github.com/arifrhb/IPScan/releases/tag/v1.2.2",
          "body": "- Added auto-updater\n- Bug fixes",
          "assets": [
            {
              "name": "IPScan-win-x64-lite.exe",
              "size": 1048576,
              "browser_download_url": "https://github.com/arifrhb/IPScan/releases/download/v1.2.2/IPScan-win-x64-lite.exe"
            },
            {
              "name": "IPScan-win-x64-standalone.exe",
              "size": 73400320,
              "browser_download_url": "https://github.com/arifrhb/IPScan/releases/download/v1.2.2/IPScan-win-x64-standalone.exe"
            },
            {
              "name": "SHA256SUMS.txt",
              "size": 200,
              "browser_download_url": "https://github.com/arifrhb/IPScan/releases/download/v1.2.2/SHA256SUMS.txt"
            }
          ]
        }
        """;

        var info = UpdateService.ParseReleaseJson(json, new Version(1, 2, 1), flavor, out string? checksumUrl);

        Assert.NotNull(info);
        Assert.True(info!.IsUpdateAvailable);
        Assert.Equal(new Version(1, 2, 2), info.LatestVersion);
        Assert.Equal(expectedAsset, info.AssetName);
        Assert.Equal("https://github.com/arifrhb/IPScan/releases/download/v1.2.2/SHA256SUMS.txt", checksumUrl);
        Assert.Contains("Added auto-updater", info.ReleaseNotes);
    }

    [Fact]
    public void ParseReleaseJson_IgnoresDraftAndPrerelease()
    {
        string draftJson = """
        {
          "tag_name": "v1.2.2",
          "draft": true,
          "prerelease": false,
          "assets": [
            { "name": "IPScan-win-x64-lite.exe", "size": 1000, "browser_download_url": "https://example.com/a.exe" }
          ]
        }
        """;

        string preJson = """
        {
          "tag_name": "v1.2.2",
          "draft": false,
          "prerelease": true,
          "assets": [
            { "name": "IPScan-win-x64-lite.exe", "size": 1000, "browser_download_url": "https://example.com/a.exe" }
          ]
        }
        """;

        Assert.Null(UpdateService.ParseReleaseJson(draftJson, new Version(1, 2, 1), BuildFlavor.Lite, out _));
        Assert.Null(UpdateService.ParseReleaseJson(preJson, new Version(1, 2, 1), BuildFlavor.Lite, out _));
    }

    [Fact]
    public void ParseSha256Sums_FindsMatchingHashRegardlessOfPrefixOrBinaryStar()
    {
        const string liteHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        const string standaloneHash = "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210";

        string content = $"""
        # checksums
        {liteHash}  IPScan-win-x64-lite.exe
        {standaloneHash} *./release-assets/IPScan-win-x64-standalone.exe
        """;

        Assert.Equal(liteHash, UpdateService.ParseSha256Sums(content, "IPScan-win-x64-lite.exe"));
        Assert.Equal(standaloneHash, UpdateService.ParseSha256Sums(content, "IPScan-win-x64-standalone.exe"));
        Assert.Null(UpdateService.ParseSha256Sums(content, "missing.exe"));
    }

    [Fact]
    public async Task CheckAndDownloadUpdate_VerifiesValidSha256AndRejectsTamperedPayload()
    {
        byte[] fakePeBytes = new byte[128];
        fakePeBytes[0] = (byte)'M';
        fakePeBytes[1] = (byte)'Z';
        for (int i = 2; i < fakePeBytes.Length; i++) fakePeBytes[i] = (byte)(i % 251);

        string validHash = Convert.ToHexString(SHA256.HashData(fakePeBytes)).ToLowerInvariant();
        string releaseJson = $$"""
        {
          "tag_name": "v1.3.0",
          "name": "IPScan v1.3.0",
          "draft": false,
          "prerelease": false,
          "html_url": "https://github.com/arifrhb/IPScan/releases/tag/v1.3.0",
          "body": "Release notes",
          "assets": [
            {
              "name": "IPScan-win-x64-lite.exe",
              "size": {{fakePeBytes.Length}},
              "browser_download_url": "https://github.com/arifrhb/IPScan/releases/download/v1.3.0/IPScan-win-x64-lite.exe"
            },
            {
              "name": "SHA256SUMS.txt",
              "size": 100,
              "browser_download_url": "https://github.com/arifrhb/IPScan/releases/download/v1.3.0/SHA256SUMS.txt"
            }
          ]
        }
        """;

        string sumsContent = $"{validHash}  IPScan-win-x64-lite.exe\n";

        var handler = new StubHttpMessageHandler(req =>
        {
            string url = req.RequestUri!.ToString();
            if (url == UpdateService.LatestReleaseApiUrl)
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releaseJson, Encoding.UTF8, "application/json") };
            if (url.EndsWith("SHA256SUMS.txt", StringComparison.Ordinal))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(sumsContent, Encoding.UTF8, "text/plain") };
            if (url.EndsWith("IPScan-win-x64-lite.exe", StringComparison.Ordinal))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(fakePeBytes) };

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var httpClient = new HttpClient(handler);
        using var service = new UpdateService(httpClient);

        var update = await service.CheckForUpdateAsync(new Version(1, 2, 1), BuildFlavor.Lite);
        Assert.NotNull(update);
        Assert.True(update!.IsUpdateAvailable);
        Assert.Equal(validHash, update.ExpectedSha256);

        string downloadedPath = await service.DownloadAndVerifyUpdateAsync(update);
        try
        {
            Assert.True(File.Exists(downloadedPath));
            Assert.Equal(fakePeBytes.Length, new FileInfo(downloadedPath).Length);
        }
        finally
        {
            string? dir = Path.GetDirectoryName(downloadedPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        // Now verify tampered hash fails cleanly
        var tamperedUpdate = update with { ExpectedSha256 = new string('0', 64) };
        await Assert.ThrowsAsync<InvalidDataException>(() => service.DownloadAndVerifyUpdateAsync(tamperedUpdate));
    }

    [Fact]
    public void BuildUpdaterPowerShellScript_EscapesSingleQuotesInPaths()
    {
        string script = UpdateService.BuildUpdaterPowerShellScript(
            12345,
            @"C:\Temp\User's Folder\IPScan.exe",
            @"D:\Tools\O'Brien\IPScan.exe",
            @"C:\Temp\User's Folder");

        Assert.Contains("$targetPid = 12345", script);
        Assert.Contains("User''s Folder", script);
        Assert.Contains("O''Brien", script);
        Assert.Contains("Start-Process -FilePath $dest", script);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}

