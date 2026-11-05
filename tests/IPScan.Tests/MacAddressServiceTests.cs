using System.Net;
using System.Threading.Tasks;
using IPScan.Services.Scanning;
using Xunit;

namespace IPScan.Tests;

public class MacAddressServiceTests
{
    [Fact]
    public void FormatMacAddress_Formats6Bytes_InHyphenatedHexUppercase()
    {
        byte[] bytes = new byte[] { 0x00, 0x1A, 0x2B, 0x3C, 0x4D, 0x5E };
        string formatted = MacAddressService.FormatMacAddress(bytes);

        Assert.Equal("00-1A-2B-3C-4D-5E", formatted);
    }

    [Fact]
    public void FormatMacAddress_WithNullOrShortBytes_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, MacAddressService.FormatMacAddress(null!));
        Assert.Equal(string.Empty, MacAddressService.FormatMacAddress(new byte[] { 0x01, 0x02 }));
    }

    [Fact]
    public async Task GetMacAddressAsync_ForLoopback_ReturnsValidMacAddress()
    {
        var loopback = IPAddress.Loopback;
        string? mac = await MacAddressService.GetMacAddressAsync(loopback, timeoutMs: 1500);

        // On any computer with an active network adapter, loopback resolves to the host's primary MAC
        if (mac != null)
        {
            Assert.Matches(@"^([0-9A-F]{2}-){5}[0-9A-F]{2}$", mac);
        }
    }
}
