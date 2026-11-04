using System.Drawing;
using System.IO;
using IPScan.UI;
using Xunit;

namespace IPScan.Tests;

public class MenuIconHelperTests
{
    [Theory]
    [InlineData(16)]
    [InlineData(20)]
    [InlineData(24)]
    [InlineData(32)]
    public void AllIcons_Render_With_Exact_Dimensions_And_NonEmptyPixels(int size)
    {
        var icons = new (string Name, Bitmap Bmp)[]
        {
            ("HTTP", MenuIconHelper.CreateHttpIcon(size)),
            ("HTTPS", MenuIconHelper.CreateHttpsIcon(size)),
            ("VNC", MenuIconHelper.CreateVncIcon(size)),
            ("SharedFolder", MenuIconHelper.CreateSharedFolderIcon(size)),
            ("CopyIp", MenuIconHelper.CreateCopyIpIcon(size)),
            ("CopyRow", MenuIconHelper.CreateCopyRowDetailsIcon(size))
        };

        foreach (var (name, bmp) in icons)
        {
            using (bmp)
            {
                Assert.NotNull(bmp);
                Assert.Equal(size, bmp.Width);
                Assert.Equal(size, bmp.Height);

                // Verify that icon has rendered visible (non-zero alpha) pixels
                bool hasVisiblePixels = false;
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        var pixel = bmp.GetPixel(x, y);
                        if (pixel.A > 0)
                        {
                            hasVisiblePixels = true;
                            break;
                        }
                    }
                    if (hasVisiblePixels) break;
                }

                Assert.True(hasVisiblePixels, $"{name} icon at size {size} should have visible pixels.");
            }
        }
    }

    [Fact]
    public void Icons_AreCached_AndReturnIndependentInstances()
    {
        using var icon1 = MenuIconHelper.CreateHttpIcon(16);
        using var icon2 = MenuIconHelper.CreateHttpIcon(16);

        Assert.NotNull(icon1);
        Assert.NotNull(icon2);
        Assert.NotSame(icon1, icon2); // Cloned instance so disposing one doesn't corrupt cache
        Assert.Equal(icon1.Width, icon2.Width);
        Assert.Equal(icon1.Height, icon2.Height);
    }

    [Fact]
    public void ExportIconsToArtifacts_ForVisualVerification()
    {
        string scratchDir = Path.Combine(
            @"C:\Users\arif.uddin\.gemini\antigravity\brain\7b8e17fe-8bb0-461e-b0e2-69e87b384f13\scratch");
        
        if (Directory.Exists(scratchDir))
        {
            int[] sizes = [16, 32];
            foreach (var size in sizes)
            {
                using var http = MenuIconHelper.CreateHttpIcon(size);
                using var https = MenuIconHelper.CreateHttpsIcon(size);
                using var vnc = MenuIconHelper.CreateVncIcon(size);
                using var shared = MenuIconHelper.CreateSharedFolderIcon(size);
                using var copyIp = MenuIconHelper.CreateCopyIpIcon(size);
                using var copyRow = MenuIconHelper.CreateCopyRowDetailsIcon(size);

                http.Save(Path.Combine(scratchDir, $"http_{size}.png"));
                https.Save(Path.Combine(scratchDir, $"https_{size}.png"));
                vnc.Save(Path.Combine(scratchDir, $"vnc_{size}.png"));
                shared.Save(Path.Combine(scratchDir, $"shared_{size}.png"));
                copyIp.Save(Path.Combine(scratchDir, $"copy_ip_{size}.png"));
                copyRow.Save(Path.Combine(scratchDir, $"copy_row_{size}.png"));
            }
        }
    }
}
