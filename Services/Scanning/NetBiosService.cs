using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IPScan.Services.Scanning;

public sealed class NetBiosResult
{
    public string? ComputerName { get; init; }
    public string? WorkgroupName { get; init; }
    public string? LoggedInUser { get; init; }

    public string? FormattedName => !string.IsNullOrEmpty(ComputerName)
        ? (!string.IsNullOrEmpty(WorkgroupName) ? $"{ComputerName} [{WorkgroupName}]" : ComputerName)
        : WorkgroupName;
}

/// <summary>
/// Performs RFC 1001/1002 NetBIOS Name Service (NBNS) Node Status queries over UDP port 137.
/// Extracts computer workstation, domain/workgroup, and registered user names safely.
/// </summary>
public static class NetBiosService
{
    private const int NetBiosPort = 137;

    public static async Task<string?> QueryNetBiosNameAsync(IPAddress address, int timeoutMs = 1200, CancellationToken cancellationToken = default)
    {
        var result = await QueryNetBiosAsync(address, timeoutMs, cancellationToken);
        return result?.FormattedName;
    }

    public static async Task<NetBiosResult?> QueryNetBiosAsync(IPAddress address, int timeoutMs = 1200, CancellationToken cancellationToken = default)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return null;
        }

        try
        {
            using var udp = new UdpClient();
            udp.Client.ReceiveTimeout = timeoutMs;
            udp.Client.SendTimeout = timeoutMs;

            byte[] request = BuildNodeStatusRequest();
            var endpoint = new IPEndPoint(address, NetBiosPort);

            // Send query
            await udp.SendAsync(request, request.Length, endpoint);

            // Wait for response with timeout
            var receiveTask = udp.ReceiveAsync();
            var completedTask = await Task.WhenAny(receiveTask, Task.Delay(timeoutMs, cancellationToken));

            if (completedTask == receiveTask)
            {
                var udpResult = await receiveTask;
                return ParseNodeStatusResponse(udpResult.Buffer);
            }
        }
        catch (SocketException)
        {
            // Port closed, host unreachable, or firewall blocked UDP 137
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch
        {
            // Defensive catch to prevent scan engine aborts
        }

        return null;
    }

    /// <summary>
    /// Builds a 50-byte RFC 1002 NetBIOS Node Status wildcard query packet.
    /// </summary>
    private static byte[] BuildNodeStatusRequest()
    {
        byte[] packet = new byte[50];

        // Header: Transaction ID
        packet[0] = 0x81;
        packet[1] = 0x42;
        // Flags: Standard query
        packet[2] = 0x00;
        packet[3] = 0x00;
        // Questions: 1
        packet[4] = 0x00;
        packet[5] = 0x01;
        // Answer RRs: 0, Authority RRs: 0, Additional RRs: 0 (bytes 6-11 already 0)

        // Question Name: 32 bytes encoded representation of '*' padded with nulls
        packet[12] = 0x20; // Length: 32
        // "CK" followed by 30 'A's
        packet[13] = (byte)'C';
        packet[14] = (byte)'K';
        for (int i = 15; i < 45; i++)
        {
            packet[i] = (byte)'A';
        }
        packet[45] = 0x00; // End of name

        // Question Type: 0x0021 (NBSTAT)
        packet[46] = 0x00;
        packet[47] = 0x21;

        // Question Class: 0x0001 (IN)
        packet[48] = 0x00;
        packet[49] = 0x01;

        return packet;
    }

    /// <summary>
    /// Parses an NBSTAT response packet and extracts workstation, domain, and registered user names.
    /// </summary>
    private static NetBiosResult? ParseNodeStatusResponse(byte[] buffer)
    {
        if (buffer == null || buffer.Length < 56) return null;

        try
        {
            int anCount = (buffer[6] << 8) | buffer[7];
            if (anCount < 1) return null;

            int offset = 12;

            // Skip question section
            while (offset < buffer.Length && buffer[offset] != 0)
            {
                offset += buffer[offset] + 1;
            }
            offset += 5; // skip null terminator, type (2 bytes), and class (2 bytes)

            if (offset >= buffer.Length) return null;

            // Skip Answer RR name (could be pointer 2 bytes or name)
            if ((buffer[offset] & 0xC0) == 0xC0)
            {
                offset += 2;
            }
            else
            {
                while (offset < buffer.Length && buffer[offset] != 0)
                {
                    offset += buffer[offset] + 1;
                }
                offset++;
            }

            // Skip Type (2), Class (2), TTL (4) -> 8 bytes
            offset += 8;
            if (offset + 2 >= buffer.Length) return null;

            // Data length (2 bytes)
            int dataLength = (buffer[offset] << 8) | buffer[offset + 1];
            offset += 2;

            if (offset >= buffer.Length) return null;

            // Number of names (1 byte)
            int nameCount = buffer[offset];
            offset += 1;

            string? computerName = null;
            string? workgroupName = null;
            var names03 = new List<string>();

            for (int i = 0; i < nameCount; i++)
            {
                if (offset + 18 > buffer.Length) break;

                string? cleanName = CleanNetBiosName(buffer, offset);
                byte nameType = buffer[offset + 15];
                byte flagsHigh = buffer[offset + 16];
                bool isGroup = (flagsHigh & 0x80) != 0;

                if (!string.IsNullOrEmpty(cleanName))
                {
                    // Name Type 0x00: Workstation service (unique) or Workgroup/Domain (group)
                    if (nameType == 0x00)
                    {
                        if (!isGroup && string.IsNullOrEmpty(computerName))
                        {
                            computerName = cleanName;
                        }
                        else if (isGroup && string.IsNullOrEmpty(workgroupName))
                        {
                            workgroupName = cleanName;
                        }
                    }
                    // Name Type 0x20: File Server service (backup for computer name)
                    else if (nameType == 0x20 && !isGroup && string.IsNullOrEmpty(computerName))
                    {
                        computerName = cleanName;
                    }
                    // Name Type 0x03: Messenger / Workstation service (registered for logged-on user)
                    else if (nameType == 0x03 && !isGroup)
                    {
                        names03.Add(cleanName);
                    }
                }

                offset += 18;
            }

            // Determine if any 0x03 name corresponds to a logged-in user
            string? loggedInUser = null;
            foreach (var n3 in names03)
            {
                if (!string.Equals(n3, computerName, StringComparison.OrdinalIgnoreCase) &&
                    !n3.EndsWith("$") && // Exclude machine accounts
                    !string.IsNullOrWhiteSpace(n3))
                {
                    loggedInUser = n3;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(computerName) || !string.IsNullOrEmpty(workgroupName) || !string.IsNullOrEmpty(loggedInUser))
            {
                return new NetBiosResult
                {
                    ComputerName = computerName,
                    WorkgroupName = workgroupName,
                    LoggedInUser = loggedInUser
                };
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Safely extracts and cleans a 15-byte NetBIOS name, stripping null bytes (\0),
    /// unprintable control characters, and rejecting garbage/punctuation-only values.
    /// </summary>
    public static string? CleanNetBiosName(byte[] buffer, int offset)
    {
        if (buffer == null || offset + 15 > buffer.Length) return null;

        var sb = new StringBuilder(15);
        for (int i = 0; i < 15; i++)
        {
            byte b = buffer[offset + i];
            if (b == 0) continue; // Skip null terminator/padding bytes
            if (b >= 32 && b <= 126) // Printable ASCII only
            {
                sb.Append((char)b);
            }
        }

        string cleaned = sb.ToString().Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) return null;

        // A valid NetBIOS name must have at least one alphanumeric character
        // Discard names that are pure punctuation/garbage (e.g., "?", "-", etc.)
        bool hasAlphanumeric = false;
        foreach (char c in cleaned)
        {
            if (char.IsLetterOrDigit(c))
            {
                hasAlphanumeric = true;
                break;
            }
        }

        return hasAlphanumeric ? cleaned : null;
    }
}
