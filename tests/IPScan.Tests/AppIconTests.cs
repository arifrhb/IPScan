using IPScan.UI;

namespace IPScan.Tests;

public class AppIconTests
{
    [Fact]
    public void GetAppIcon_ReturnsValidIconFromAssemblyResource()
    {
        var icon = AppIconHelper.GetAppIcon();

        Assert.NotNull(icon);
        Assert.True(icon.Width > 0);
        Assert.True(icon.Height > 0);
    }
}

