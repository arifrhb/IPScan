using System.Net;
using System.Net.Sockets;

namespace IPScan.Services.Scanning;

/// <summary>
/// Performs reverse DNS lookup with timeout safeguards.
/// </summary>
public static class DnsService
{
    public static async Task<string?> ResolveHostnameAsync(IPAddress address, int timeoutMs = 1500, CancellationToken cancellationToken = default)
    {
        try
        {
            var dnsTask = Dns.GetHostEntryAsync(address);
            var completedTask = await Task.WhenAny(dnsTask, Task.Delay(timeoutMs, cancellationToken));

            if (completedTask == dnsTask)
            {
                var entry = await dnsTask;
                if (!string.IsNullOrWhiteSpace(entry.HostName))
                {
                    return entry.HostName;
                }
            }
        }
        catch (SocketException)
        {
            // Expected when reverse PTR record does not exist
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch
        {
            // Defensive catch to ensure UI never crashes on DNS hiccups
        }

        return null;
    }
}

