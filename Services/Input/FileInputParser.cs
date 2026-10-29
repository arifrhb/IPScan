namespace IPScan.Services.Input;

/// <summary>
/// Reads IP targets from a text file, filtering empty lines and comments (#).
/// </summary>
public static class FileInputParser
{
    public static async Task<ParseResult> ParseFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new ParseResult(Array.Empty<Models.ScanTarget>(), new[] { "File path cannot be empty." });
        }

        if (!File.Exists(filePath))
        {
            return new ParseResult(Array.Empty<Models.ScanTarget>(), new[] { $"File not found: '{filePath}'." });
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
            var validLines = lines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                .ToList();

            if (validLines.Count == 0)
            {
                return new ParseResult(Array.Empty<Models.ScanTarget>(), new[] { $"The file '{Path.GetFileName(filePath)}' contains no valid IP addresses or CIDR subnets (only comments or blank lines)." });
            }

            string combined = string.Join(Environment.NewLine, validLines);
            return IpRangeParser.Parse(combined);
        }
        catch (Exception ex)
        {
            return new ParseResult(Array.Empty<Models.ScanTarget>(), new[] { $"Error reading file '{filePath}': {ex.Message}" });
        }
    }
}

