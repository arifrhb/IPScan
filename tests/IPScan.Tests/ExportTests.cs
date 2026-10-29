using System.Net;
using IPScan.Models;
using IPScan.Services.Export;

namespace IPScan.Tests;

public class ExportTests
{
    [Fact]
    public async Task TxtExporter_CreatesReadableFileWithSummaryAndHeaders()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"scan_test_{Guid.NewGuid():N}.txt");
        try
        {
            var results = new List<ScanResult>
            {
                new()
                {
                    Address = IPAddress.Parse("192.168.1.1"),
                    Status = HostStatus.Online,
                    RoundTripTimeMs = 2,
                    Hostname = "router.local",
                    NetBiosName = "ROUTER",
                    LoggedInUser = null
                },
                new()
                {
                    Address = IPAddress.Parse("192.168.1.2"),
                    Status = HostStatus.Offline,
                    RoundTripTimeMs = -1,
                    StatusMessage = "Request timed out"
                }
            };

            var exporter = new TxtExporter();
            await exporter.ExportAsync(tempFile, results);

            Assert.True(File.Exists(tempFile));
            string content = await File.ReadAllTextAsync(tempFile);

            Assert.Contains("IPScan - Network Scan Report", content);
            Assert.Contains("192.168.1.1", content);
            Assert.Contains("Online", content);
            Assert.Contains("router.local", content);
            Assert.Contains("192.168.1.2", content);
            Assert.Contains("Offline", content);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ExcelExporter_CreatesValidXlsxFile()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"scan_test_{Guid.NewGuid():N}.xlsx");
        try
        {
            var results = new List<ScanResult>
            {
                new()
                {
                    Address = IPAddress.Parse("10.0.0.1"),
                    Status = HostStatus.Online,
                    RoundTripTimeMs = 5,
                    Hostname = "gateway.corp",
                    NetBiosName = "GATEWAY",
                    LoggedInUser = @"CORP\admin"
                }
            };

            var exporter = new ExcelExporter();
            await exporter.ExportAsync(tempFile, results);

            Assert.True(File.Exists(tempFile));
            var fileInfo = new FileInfo(tempFile);
            Assert.True(fileInfo.Length > 0);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ScanResult_AccessibleStatus_ConveysStateInTextWithoutColorAlone()
    {
        var online = new ScanResult { Status = HostStatus.Online };
        var offline = new ScanResult { Status = HostStatus.Offline };

        Assert.Equal("[+] Online", online.AccessibleStatus);
        Assert.Equal("[-] Offline", offline.AccessibleStatus);
    }
}

