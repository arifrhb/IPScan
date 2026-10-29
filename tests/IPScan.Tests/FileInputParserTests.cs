using IPScan.Services.Input;

namespace IPScan.Tests;

public class FileInputParserTests
{
    [Fact]
    public async Task ParseFileAsync_IgnoresCommentsAndBlanks()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            var content = new[]
            {
                "# Network configuration scan targets",
                "",
                "192.168.1.1",
                "   # This is another comment",
                "192.168.1.50",
                "",
                "# End of file"
            };

            await File.WriteAllLinesAsync(tempFile, content);

            var result = await FileInputParser.ParseFileAsync(tempFile);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Targets.Count);
            Assert.Equal("192.168.1.1", result.Targets[0].ToString());
            Assert.Equal("192.168.1.50", result.Targets[1].ToString());
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
    public async Task ParseFileAsync_MissingFile_ReturnsError()
    {
        string fakePath = Path.Combine(Path.GetTempPath(), "nonexistent_ip_file_123456.txt");
        var result = await FileInputParser.ParseFileAsync(fakePath);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("File not found"));
    }
}

