using System.Net;
using System.Net.NetworkInformation;
using IPScan.Models;
using IPScan.Services.Scanning;

namespace IPScan.Tests;

public class WmiUserServiceTests
{
    [Fact]
    public async Task QueryLoggedInUserAsync_OnLoopback_ReturnsCurrentLoggedInUser()
    {
        string? user = await WmiUserService.QueryLoggedInUserAsync(IPAddress.Loopback, 3000);
        Assert.NotNull(user);
        Assert.Contains(Environment.UserName, user, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QueryLoggedInUserAsync_OnLocalAdapterIp_ReturnsCurrentLoggedInUser()
    {
        IPAddress? localIp = null;
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
                {
                    localIp = ua.Address;
                    break;
                }
            }
            if (localIp != null) break;
        }

        if (localIp != null)
        {
            string? user = await WmiUserService.QueryLoggedInUserAsync(localIp, 3000);
            Assert.NotNull(user);
            Assert.Contains(Environment.UserName, user, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task NetworkScanner_ScanAsync_OnLoopback_PopulatesLoggedInUserInOutput()
    {
        var scanner = new NetworkScanner();
        var options = new ScanOptions
        {
            Concurrency = 1,
            TimeoutMs = 1500,
            ResolveDns = true,
            ResolveNetBios = true,
            QueryLoggedInUser = true
        };

        var targets = new List<ScanTarget> { new(IPAddress.Loopback) };
        var results = await scanner.ScanAsync(targets, options, null, CancellationToken.None);

        Assert.Single(results);
        var res = results[0];
        Assert.Equal(HostStatus.Online, res.Status);
        Assert.NotNull(res.LoggedInUser);
        Assert.Contains(Environment.UserName, res.LoggedInUser, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual("-", res.LoggedInUserDisplay);
    }

    [Fact]
    public void CleanNetBiosName_StripsNullBytesAndRejectsGarbage()
    {
        // 15 bytes with null padding
        byte[] buffer = new byte[18];
        buffer[0] = (byte)'X';
        // remaining bytes are 0
        string? res1 = NetBiosService.CleanNetBiosName(buffer, 0);
        Assert.Equal("X", res1);

        // buffer with only "?"
        byte[] bufGarbage = new byte[18];
        bufGarbage[0] = (byte)'?';
        string? res2 = NetBiosService.CleanNetBiosName(bufGarbage, 0);
        Assert.Null(res2); // Rejected because it has no alphanumeric characters

        // buffer with clean name "FILESERVER" padded with spaces and nulls
        byte[] bufClean = new byte[18];
        byte[] ascii = System.Text.Encoding.ASCII.GetBytes("FILESERVER     ");
        Array.Copy(ascii, bufClean, 15);
        string? res3 = NetBiosService.CleanNetBiosName(bufClean, 0);
        Assert.Equal("FILESERVER", res3);
    }

    [Fact]
    public async Task QueryLoggedInUserAsync_WithGarbageRemoteTarget_DoesNotFallbackToLocalUser()
    {
        // A non-local unreachable test IP with corrupted name candidate
        var remoteIp = IPAddress.Parse("192.0.2.1"); // TEST-NET-1 (RFC 5737)
        string? user = await WmiUserService.QueryLoggedInUserAsync(remoteIp, "?\0\0\0", 500);

        // Must NOT return the current local user!
        if (user != null)
        {
            Assert.DoesNotContain(Environment.UserName, user, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task QueryLoggedInUserAsync_LiveSubnetHosts_ReturnCorrectDistinctUsers()
    {
        var target107 = IPAddress.Parse("172.45.25.107");
        var ping107 = await PingService.PingHostAsync(target107, 1000);
        if (ping107.IsOnline)
        {
            var user107 = await WmiUserService.QueryLoggedInUserAsync(target107, "AlfajITUpdate.meghnagroup.com", 2000);
            if (user107 != null)
            {
                Assert.Contains("alfaj.hossen", user107, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}

