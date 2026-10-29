using System.Net;
using System.Reflection;
using IPScan.Services.Scanning;

namespace IPScan.Tests;

public class NetBiosServiceTests
{
    [Fact]
    public void BuildNodeStatusRequest_ProducesExact50ByteRfc1002Packet()
    {
        // Extract the private BuildNodeStatusRequest method via reflection to verify packet conformity
        var method = typeof(NetBiosService).GetMethod("BuildNodeStatusRequest",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        var packet = method.Invoke(null, null) as byte[];

        Assert.NotNull(packet);
        Assert.Equal(50, packet.Length);

        // Header: Transaction ID
        Assert.Equal(0x81, packet[0]);
        Assert.Equal(0x42, packet[1]);

        // Questions: 1
        Assert.Equal(0x00, packet[4]);
        Assert.Equal(0x01, packet[5]);

        // Name length: 32
        Assert.Equal(0x20, packet[12]);

        // Standard Wildcard encoding: "CK" followed by 30 'A's
        Assert.Equal((byte)'C', packet[13]);
        Assert.Equal((byte)'K', packet[14]);
        for (int i = 15; i < 45; i++)
        {
            Assert.Equal((byte)'A', packet[i]);
        }

        // Null terminator
        Assert.Equal(0x00, packet[45]);

        // Question Type: 0x0021 (NBSTAT)
        Assert.Equal(0x00, packet[46]);
        Assert.Equal(0x21, packet[47]);

        // Question Class: 0x0001 (IN)
        Assert.Equal(0x00, packet[48]);
        Assert.Equal(0x01, packet[49]);
    }

    [Fact]
    public async Task QueryNetBiosNameAsync_HandlesLoopbackOrUnreachableSafelyWithoutThrowing()
    {
        // NetBIOS query to loopback or unresponsive address should return null gracefully
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        string? result = await NetBiosService.QueryNetBiosNameAsync(IPAddress.Loopback, 100, cts.Token);
        // Does not throw and returns either null or local netbios name
    }
}

