using System.Net;
using IPScan.Models;

namespace IPScan.Tests;

public class FilterTests
{
    private readonly List<ScanResult> _testResults = new()
    {
        new ScanResult
        {
            Address = IPAddress.Parse("192.168.1.1"),
            Status = HostStatus.Online,
            RoundTripTimeMs = 2
        },
        new ScanResult
        {
            Address = IPAddress.Parse("192.168.1.2"),
            Status = HostStatus.Offline,
            RoundTripTimeMs = -1
        },
        new ScanResult
        {
            Address = IPAddress.Parse("192.168.1.3"),
            Status = HostStatus.Online,
            RoundTripTimeMs = 5
        },
        new ScanResult
        {
            Address = IPAddress.Parse("192.168.1.4"),
            Status = HostStatus.Error,
            RoundTripTimeMs = -1
        }
    };

    [Fact]
    public void FilterMode_Both_ReturnsAllTargets()
    {
        var filter = ResultFilterMode.Both;
        var filtered = _testResults.Where(r => filter.Matches(r.Status)).ToList();

        Assert.Equal(4, filtered.Count);
        Assert.Equal("Both", filter.ToDisplayName());
    }

    [Fact]
    public void FilterMode_Online_ReturnsOnlyOnlineTargets()
    {
        var filter = ResultFilterMode.Online;
        var filtered = _testResults.Where(r => filter.Matches(r.Status)).ToList();

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, r => Assert.Equal(HostStatus.Online, r.Status));
        Assert.Equal("Online", filter.ToDisplayName());
    }

    [Fact]
    public void FilterMode_Offline_ReturnsOnlyOfflineAndErrorTargets()
    {
        var filter = ResultFilterMode.Offline;
        var filtered = _testResults.Where(r => filter.Matches(r.Status)).ToList();

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, r => Assert.NotEqual(HostStatus.Online, r.Status));
        Assert.Equal("Offline", filter.ToDisplayName());
    }

    [Theory]
    [InlineData(HostStatus.Online, ResultFilterMode.Both, true)]
    [InlineData(HostStatus.Offline, ResultFilterMode.Both, true)]
    [InlineData(HostStatus.Online, ResultFilterMode.Online, true)]
    [InlineData(HostStatus.Offline, ResultFilterMode.Online, false)]
    [InlineData(HostStatus.Online, ResultFilterMode.Offline, false)]
    [InlineData(HostStatus.Offline, ResultFilterMode.Offline, true)]
    [InlineData(HostStatus.Error, ResultFilterMode.Offline, true)]
    public void ResultFilterExtensions_Matches_EvaluatesCorrectly(HostStatus status, ResultFilterMode mode, bool expected)
    {
        Assert.Equal(expected, mode.Matches(status));
    }
}

