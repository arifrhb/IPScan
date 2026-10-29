using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using IPScan.Models;
using IPScan.Services.Scanning;
using Xunit;

namespace IPScan.Tests;

public class ResultSorterTests
{
    [Fact]
    public void Sort_ByIp_SortsNumericallyNotAlphabetically_AscendingAndDescending()
    {
        var list = new List<ScanResult>
        {
            new() { Address = IPAddress.Parse("172.45.25.100") },
            new() { Address = IPAddress.Parse("172.45.25.2") },
            new() { Address = IPAddress.Parse("172.45.25.10") },
            new() { Address = IPAddress.Parse("172.45.25.1") }
        };

        var asc = ResultSorter.Sort(list, ResultSorter.ColumnIp, SortOrder.Ascending).ToList();
        Assert.Equal("172.45.25.1", asc[0].IpString);
        Assert.Equal("172.45.25.2", asc[1].IpString);
        Assert.Equal("172.45.25.10", asc[2].IpString);
        Assert.Equal("172.45.25.100", asc[3].IpString);

        var desc = ResultSorter.Sort(list, ResultSorter.ColumnIp, SortOrder.Descending).ToList();
        Assert.Equal("172.45.25.100", desc[0].IpString);
        Assert.Equal("172.45.25.10", desc[1].IpString);
        Assert.Equal("172.45.25.2", desc[2].IpString);
        Assert.Equal("172.45.25.1", desc[3].IpString);
    }

    [Fact]
    public void Sort_ByStatus_OrdersOnlineOfflineErrorCorrectly()
    {
        var list = new List<ScanResult>
        {
            new() { Address = IPAddress.Parse("10.0.0.3"), Status = HostStatus.Offline },
            new() { Address = IPAddress.Parse("10.0.0.1"), Status = HostStatus.Online },
            new() { Address = IPAddress.Parse("10.0.0.2"), Status = HostStatus.Error }
        };

        var asc = ResultSorter.Sort(list, ResultSorter.ColumnStatus, SortOrder.Ascending).ToList();
        Assert.Equal(HostStatus.Online, asc[0].Status);
        Assert.Equal(HostStatus.Offline, asc[1].Status);
        Assert.Equal(HostStatus.Error, asc[2].Status);

        var desc = ResultSorter.Sort(list, ResultSorter.ColumnStatus, SortOrder.Descending).ToList();
        Assert.Equal(HostStatus.Error, desc[0].Status);
        Assert.Equal(HostStatus.Offline, desc[1].Status);
        Assert.Equal(HostStatus.Online, desc[2].Status);
    }

    [Fact]
    public void Sort_ByLatency_SortsResponsiveHostsFirst_ThenOfflineHosts()
    {
        var list = new List<ScanResult>
        {
            new() { Address = IPAddress.Parse("10.0.0.4"), Status = HostStatus.Offline, RoundTripTimeMs = -1 },
            new() { Address = IPAddress.Parse("10.0.0.1"), Status = HostStatus.Online, RoundTripTimeMs = 15 },
            new() { Address = IPAddress.Parse("10.0.0.2"), Status = HostStatus.Online, RoundTripTimeMs = 0 },
            new() { Address = IPAddress.Parse("10.0.0.3"), Status = HostStatus.Online, RoundTripTimeMs = 2 }
        };

        var asc = ResultSorter.Sort(list, ResultSorter.ColumnLatency, SortOrder.Ascending).ToList();
        Assert.Equal("10.0.0.2", asc[0].IpString); // 0 ms
        Assert.Equal("10.0.0.3", asc[1].IpString); // 2 ms
        Assert.Equal("10.0.0.1", asc[2].IpString); // 15 ms
        Assert.Equal("10.0.0.4", asc[3].IpString); // offline

        var desc = ResultSorter.Sort(list, ResultSorter.ColumnLatency, SortOrder.Descending).ToList();
        Assert.Equal("10.0.0.1", desc[0].IpString); // 15 ms
        Assert.Equal("10.0.0.3", desc[1].IpString); // 2 ms
        Assert.Equal("10.0.0.2", desc[2].IpString); // 0 ms
        Assert.Equal("10.0.0.4", desc[3].IpString); // offline
    }

    [Fact]
    public void Sort_ByHostname_SortsAlphabeticallyWithEmptiesAtEnd()
    {
        var list = new List<ScanResult>
        {
            new() { Address = IPAddress.Parse("10.0.0.3"), Hostname = "zeta.corp.local" },
            new() { Address = IPAddress.Parse("10.0.0.4"), Hostname = null },
            new() { Address = IPAddress.Parse("10.0.0.1"), Hostname = "alpha.corp.local" },
            new() { Address = IPAddress.Parse("10.0.0.2"), Hostname = " " }
        };

        var asc = ResultSorter.Sort(list, ResultSorter.ColumnHostname, SortOrder.Ascending).ToList();
        Assert.Equal("alpha.corp.local", asc[0].Hostname);
        Assert.Equal("zeta.corp.local", asc[1].Hostname);
        Assert.True(string.IsNullOrWhiteSpace(asc[2].Hostname));
        Assert.True(string.IsNullOrWhiteSpace(asc[3].Hostname));

        var desc = ResultSorter.Sort(list, ResultSorter.ColumnHostname, SortOrder.Descending).ToList();
        Assert.Equal("zeta.corp.local", desc[0].Hostname);
        Assert.Equal("alpha.corp.local", desc[1].Hostname);
        Assert.True(string.IsNullOrWhiteSpace(desc[2].Hostname));
        Assert.True(string.IsNullOrWhiteSpace(desc[3].Hostname));
    }

    [Fact]
    public void Sort_ByUser_SortsAlphabeticallyWithEmptiesAtEnd()
    {
        var list = new List<ScanResult>
        {
            new() { Address = IPAddress.Parse("10.0.0.3"), LoggedInUser = @"CORP\charlie" },
            new() { Address = IPAddress.Parse("10.0.0.1"), LoggedInUser = @"CORP\alice" },
            new() { Address = IPAddress.Parse("10.0.0.2"), LoggedInUser = null }
        };

        var asc = ResultSorter.Sort(list, ResultSorter.ColumnUser, SortOrder.Ascending).ToList();
        Assert.Equal(@"CORP\alice", asc[0].LoggedInUser);
        Assert.Equal(@"CORP\charlie", asc[1].LoggedInUser);
        Assert.Null(asc[2].LoggedInUser);

        var desc = ResultSorter.Sort(list, ResultSorter.ColumnUser, SortOrder.Descending).ToList();
        Assert.Equal(@"CORP\charlie", desc[0].LoggedInUser);
        Assert.Equal(@"CORP\alice", desc[1].LoggedInUser);
        Assert.Null(desc[2].LoggedInUser);
    }
}

