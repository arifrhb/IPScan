using System.Net;
using System.Text.RegularExpressions;
using IPScan.Models;

namespace IPScan.Services.Input;

public sealed class ParseResult
{
    public IReadOnlyList<ScanTarget> Targets { get; }
    public IReadOnlyList<string> Errors { get; }
    public int DuplicateCount { get; }

    public bool IsSuccess => Errors.Count == 0 && Targets.Count > 0;

    public ParseResult(IReadOnlyList<ScanTarget> targets, IReadOnlyList<string> errors, int duplicateCount = 0)
    {
        Targets = targets ?? Array.Empty<ScanTarget>();
        Errors = errors ?? Array.Empty<string>();
        DuplicateCount = duplicateCount;
    }
}

/// <summary>
/// Parses and validates IP input in single, CIDR, dash-range, and comma-separated formats.
/// Supports placeholders such as 192.168.201.x/24 and ranges like 192.168.201.1-254.
/// </summary>
public static class IpRangeParser
{
    // Maximum allowable targets to prevent accidental runaway memory allocation or network saturation
    public const int MaxAllowedTargets = 65536; // /16 maximum
    public const int MinAllowedCidrPrefix = 16;

    /// <summary>
    /// Parses a raw input string containing single IPs, CIDR ranges, dash ranges, or comma-separated lists.
    /// </summary>
    public static ParseResult Parse(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return new ParseResult(Array.Empty<ScanTarget>(), new[] { "Input cannot be empty. Please enter an IP address, range, or CIDR subnet." });
        }

        var tokens = rawInput.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(t => t.Trim())
                             .Where(t => !string.IsNullOrWhiteSpace(t) && !t.StartsWith("#"))
                             .ToList();

        if (tokens.Count == 0)
        {
            return new ParseResult(Array.Empty<ScanTarget>(), new[] { "No valid IP addresses or ranges were found in the input." });
        }

        var errors = new List<string>();
        var targetSet = new HashSet<uint>();
        int totalEncountered = 0;

        foreach (var rawToken in tokens)
        {
            var token = rawToken.Trim();
            if (string.IsNullOrEmpty(token)) continue;

            // 1. CIDR notation, e.g. 192.168.1.0/24 or 192.168.1.x/24
            if (token.Contains('/'))
            {
                var cidrResult = ParseCidrToken(token);
                if (cidrResult.Error != null)
                {
                    errors.Add(cidrResult.Error);
                }
                else if (cidrResult.Addresses != null)
                {
                    foreach (var addr in cidrResult.Addresses)
                    {
                        totalEncountered++;
                        if (targetSet.Count >= MaxAllowedTargets)
                        {
                            errors.Add($"Scan target limit exceeded: Maximum allowed is {MaxAllowedTargets:N0} targets to prevent accidental unbounded scans.");
                            break;
                        }
                        targetSet.Add(addr);
                    }
                }
            }
            // 2. Dash range, e.g. 192.168.201.1-254 or 192.168.201.1-192.168.201.254
            else if (token.Contains('-'))
            {
                var rangeResult = ParseDashRangeToken(token);
                if (rangeResult.Error != null)
                {
                    errors.Add(rangeResult.Error);
                }
                else if (rangeResult.Addresses != null)
                {
                    foreach (var addr in rangeResult.Addresses)
                    {
                        totalEncountered++;
                        if (targetSet.Count >= MaxAllowedTargets)
                        {
                            errors.Add($"Scan target limit exceeded: Maximum allowed is {MaxAllowedTargets:N0} targets.");
                            break;
                        }
                        targetSet.Add(addr);
                    }
                }
            }
            // 3. Shorthand wildcard placeholder, e.g. 192.168.1.x without slash
            else if (token.Contains('x', StringComparison.OrdinalIgnoreCase) || token.Contains('*'))
            {
                var shorthand = token + "/24";
                var cidrResult = ParseCidrToken(shorthand);
                if (cidrResult.Error != null)
                {
                    errors.Add(cidrResult.Error);
                }
                else if (cidrResult.Addresses != null)
                {
                    foreach (var addr in cidrResult.Addresses)
                    {
                        totalEncountered++;
                        if (targetSet.Count >= MaxAllowedTargets)
                        {
                            errors.Add($"Scan target limit exceeded: Maximum allowed is {MaxAllowedTargets:N0} targets.");
                            break;
                        }
                        targetSet.Add(addr);
                    }
                }
            }
            // 4. Single IP address
            else
            {
                var singleResult = ParseSingleIp(token);
                if (singleResult.Error != null)
                {
                    errors.Add(singleResult.Error);
                }
                else if (singleResult.Address.HasValue)
                {
                    totalEncountered++;
                    if (targetSet.Count >= MaxAllowedTargets)
                    {
                        errors.Add($"Scan target limit exceeded: Maximum allowed is {MaxAllowedTargets:N0} targets.");
                    }
                    else
                    {
                        targetSet.Add(singleResult.Address.Value);
                    }
                }
            }
        }

        int duplicateCount = totalEncountered - targetSet.Count;
        var sortedTargets = targetSet.OrderBy(u => u)
                                     .Select(u => new ScanTarget(ScanTarget.FromUint32(u)))
                                     .ToList();

        return new ParseResult(sortedTargets, errors, duplicateCount);
    }

    private static (List<uint>? Addresses, string? Error) ParseDashRangeToken(string token)
    {
        // Pattern A: Short octet range, e.g. 192.168.201.1-254 or 10.0.0.50-100
        var shortMatch = Regex.Match(token, @"^(\d{1,3}\.\d{1,3}\.\d{1,3})\.(\d{1,3})\s*-\s*(\d{1,3})$");
        if (shortMatch.Success)
        {
            string prefix = shortMatch.Groups[1].Value;
            if (!int.TryParse(shortMatch.Groups[2].Value, out int start) ||
                !int.TryParse(shortMatch.Groups[3].Value, out int end))
            {
                return (null, $"Invalid range numbers in '{token}'.");
            }

            if (start < 0 || start > 255 || end < 0 || end > 255)
            {
                return (null, $"Range octets in '{token}' must be between 0 and 255.");
            }

            int min = Math.Min(start, end);
            int max = Math.Max(start, end);
            var list = new List<uint>(max - min + 1);

            for (int i = min; i <= max; i++)
            {
                string ipStr = $"{prefix}.{i}";
                if (IPAddress.TryParse(ipStr, out var ip))
                {
                    list.Add(ScanTarget.ToUint32(ip));
                }
                else
                {
                    return (null, $"Invalid IP base in '{token}'.");
                }
            }

            return (list, null);
        }

        // Pattern B: Full IP range, e.g. 192.168.201.1 - 192.168.201.254
        var parts = token.Split('-', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            if (IPAddress.TryParse(parts[0], out var ip1) && IPAddress.TryParse(parts[1], out var ip2))
            {
                if (ip1.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                    ip2.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return (null, $"Only IPv4 ranges are supported in '{token}'.");
                }

                uint u1 = ScanTarget.ToUint32(ip1);
                uint u2 = ScanTarget.ToUint32(ip2);
                uint min = Math.Min(u1, u2);
                uint max = Math.Max(u1, u2);
                long count = (long)max - min + 1;

                if (count > MaxAllowedTargets)
                {
                    return (null, $"Range '{token}' spans {count:N0} targets, exceeding the safe limit of {MaxAllowedTargets:N0}.");
                }

                var list = new List<uint>((int)count);
                for (uint u = min; u <= max; u++)
                {
                    list.Add(u);
                }
                return (list, null);
            }
        }

        return (null, $"Invalid IP range format: '{token}'. Expected format like 192.168.201.1-254 or 192.168.201.1-192.168.201.254.");
    }

    private static (List<uint>? Addresses, string? Error) ParseCidrToken(string token)
    {
        var parts = token.Split('/');
        if (parts.Length != 2)
        {
            return (null, $"Invalid CIDR notation '{token}'. Format must be IP/prefix, e.g. 192.168.1.0/24 or 192.168.1.x/24.");
        }

        string ipPart = parts[0].Trim();
        string prefixPart = parts[1].Trim();

        if (!int.TryParse(prefixPart, out int prefix))
        {
            return (null, $"Invalid CIDR prefix length '{prefixPart}' in '{token}'. Prefix must be an integer.");
        }

        if (prefix < MinAllowedCidrPrefix || prefix > 32)
        {
            return (null, $"CIDR prefix /{prefix} in '{token}' is outside the safe supported range (/{MinAllowedCidrPrefix} to /32). This safeguard prevents accidental unbounded scans of massive networks.");
        }

        string normalizedIp = Regex.Replace(ipPart, @"[xX\*]", "0");

        if (!IPAddress.TryParse(normalizedIp, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return (null, $"Invalid IPv4 base address '{ipPart}' in CIDR '{token}'.");
        }

        uint numeric = ScanTarget.ToUint32(ip);
        uint mask = prefix == 0 ? 0 : 0xFFFFFFFFu << (32 - prefix);
        uint networkBase = numeric & mask;
        uint hostCount = 1u << (32 - prefix);

        var list = new List<uint>((int)Math.Min(hostCount, MaxAllowedTargets + 1));

        if (prefix == 32)
        {
            list.Add(networkBase);
        }
        else if (prefix == 31)
        {
            list.Add(networkBase);
            list.Add(networkBase + 1);
        }
        else
        {
            for (uint i = 1; i < hostCount - 1; i++)
            {
                list.Add(networkBase + i);
            }
        }

        return (list, null);
    }

    private static (uint? Address, string? Error) ParseSingleIp(string token)
    {
        if (!IPAddress.TryParse(token, out var ip))
        {
            return (null, $"'{token}' is not a recognized valid IP address, range, or CIDR notation.");
        }

        if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return (null, $"'{token}' is an IPv6 address. This scanner currently focuses on IPv4 networks.");
        }

        return (ScanTarget.ToUint32(ip), null);
    }
}
