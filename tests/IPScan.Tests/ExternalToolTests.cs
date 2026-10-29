using System.Net;
using IPScan.Services.External;

namespace IPScan.Tests;

public class ExternalToolTests
{
    [Fact]
    public void LaunchVnc_WithNonExistentCustomPath_ReturnsHelpfulErrorMessage()
    {
        string fakePath = @"C:\NonExistent\FakeViewer.exe";
        bool launched = ExternalToolLauncher.LaunchVnc(IPAddress.Loopback, fakePath, out string? error);

        Assert.False(launched);
        Assert.NotNull(error);
        Assert.Contains("The configured VNC viewer path does not exist", error);
    }

    [Fact]
    public void DetectVncViewer_WithValidCustomFile_ReturnsPath()
    {
        string tempExe = Path.GetTempFileName();
        try
        {
            string? detected = VncDetector.DetectVncViewer(tempExe);
            Assert.Equal(tempExe, detected);
        }
        finally
        {
            if (File.Exists(tempExe))
            {
                File.Delete(tempExe);
            }
        }
    }
}
