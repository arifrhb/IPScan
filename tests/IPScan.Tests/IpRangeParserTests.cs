using System.Net;
using IPScan.Services.Input;

namespace IPScan.Tests;

public class IpRangeParserTests
{
    [Fact]
    public void Parse_SingleValidIp_ReturnsSingleTarget()
    {
        var result = IpRangeParser.Parse("192.168.201.1");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Targets);
        Assert.Equal("192.168.201.1", result.Targets[0].ToString());
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_CidrSlash24_Returns254UsableHosts()
    {
        var result = IpRangeParser.Parse("192.168.201.0/24");

        Assert.True(result.IsSuccess);
        Assert.Equal(254, result.Targets.Count);
        Assert.Equal("192.168.201.1", result.Targets[0].ToString());
        Assert.Equal("192.168.201.254", result.Targets[^1].ToString());
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WildcardPlaceholder_TreatsXAsSubnet()
    {
        // "A CIDR subnet, such as 192.168.201.x/24. Treat the x as a placeholder and scan the full /24 network."
        var result = IpRangeParser.Parse("192.168.201.x/24");

        Assert.True(result.IsSuccess);
        Assert.Equal(254, result.Targets.Count);
        Assert.Equal("192.168.201.1", result.Targets[0].ToString());
        Assert.Equal("192.168.201.254", result.Targets[^1].ToString());
    }

    [Fact]
    public void Parse_DashRangeShort_ReturnsExactTargets()
    {
        // "192.168.201.1-254"
        var result = IpRangeParser.Parse("192.168.201.1-254");

        Assert.True(result.IsSuccess);
        Assert.Equal(254, result.Targets.Count);
        Assert.Equal("192.168.201.1", result.Targets[0].ToString());
        Assert.Equal("192.168.201.254", result.Targets[^1].ToString());
    }

    [Fact]
    public void Parse_DashRangeMultipleSubnets_ReturnsAllTargets()
    {
        // "192.168.201.1-254, 192.168.101.1-254"
        var result = IpRangeParser.Parse("192.168.201.1-254, 192.168.101.1-254");

        Assert.True(result.IsSuccess);
        Assert.Equal(508, result.Targets.Count);
        Assert.Contains(result.Targets, t => t.ToString() == "192.168.201.1");
        Assert.Contains(result.Targets, t => t.ToString() == "192.168.201.254");
        Assert.Contains(result.Targets, t => t.ToString() == "192.168.101.1");
        Assert.Contains(result.Targets, t => t.ToString() == "192.168.101.254");
    }

    [Fact]
    public void Parse_FullIpDashRange_ReturnsRange()
    {
        var result = IpRangeParser.Parse("192.168.1.10-192.168.1.20");

        Assert.True(result.IsSuccess);
        Assert.Equal(11, result.Targets.Count);
        Assert.Equal("192.168.1.10", result.Targets[0].ToString());
        Assert.Equal("192.168.1.20", result.Targets[^1].ToString());
    }

    [Fact]
    public void Parse_CommaSeparatedMixture_DeduplicatesAndSortsNumerically()
    {
        // "A comma-separated mixture of IPs and CIDR subnets, such as 192.168.201.1, 192.168.201.100, 192.168.201.150, 192.168.201.x/24."
        string input = "192.168.201.1, 192.168.201.100, 192.168.201.150, 192.168.201.x/24";
        var result = IpRangeParser.Parse(input);

        Assert.True(result.IsSuccess);
        // The /24 subnet already contains 192.168.201.1, 100, and 150
        Assert.Equal(254, result.Targets.Count);
        Assert.Equal(3, result.DuplicateCount);
        Assert.Equal("192.168.201.1", result.Targets[0].ToString());
    }

    [Fact]
    public void Parse_InvalidIp_ReturnsClearErrorMessage()
    {
        var result = IpRangeParser.Parse("192.168.300.1");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("not a recognized valid IP address"));
    }

    [Fact]
    public void Parse_InvalidCidrPrefix_ReturnsClearError()
    {
        var result = IpRangeParser.Parse("192.168.1.0/35");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("outside the safe supported range"));
    }

    [Fact]
    public void Parse_UnboundedLargeCidr_SafeguardsAgainstRunawayScans()
    {
        // Scanning /8 would mean 16 million hosts, which must be blocked by safeguard
        var result = IpRangeParser.Parse("10.0.0.0/8");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("safeguard prevents accidental unbounded scans"));
    }

    [Fact]
    public void Parse_EmptyOrWhitespace_ReturnsError()
    {
        var result = IpRangeParser.Parse("   ");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Input cannot be empty"));
    }
}

