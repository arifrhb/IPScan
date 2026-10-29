using System.Net;
using System.Net.NetworkInformation;

namespace IPScan.Services.Scanning;

public sealed class PingResult
{
    public bool IsOnline { get; init; }
    public long RoundTripTimeMs { get; init; }
    public string StatusMessage { get; init; } = string.Empty;
}

/// <summary>
/// Handles standard ICMP echo requests with configurable timeouts.
/// </summary>
public static class PingService
{
    private static readonly byte[] PingBuffer = new byte[32]; // Standard 32-byte payload

    public static async Task<PingResult> PingHostAsync(IPAddress address, int timeoutMs, CancellationToken cancellationToken = default)
    {
        using var ping = new Ping();

        try
        {
            // Register cancellation token to cancel pending ping if requested
            using var reg = cancellationToken.Register(() =>
            {
                try { ping.SendAsyncCancel(); } catch { }
            });

            var reply = await ping.SendPingAsync(address, timeoutMs, PingBuffer);

            if (reply.Status == IPStatus.Success)
            {
                return new PingResult
                {
                    IsOnline = true,
                    RoundTripTimeMs = reply.RoundtripTime,
                    StatusMessage = "Success"
                };
            }

            return new PingResult
            {
                IsOnline = false,
                RoundTripTimeMs = -1,
                StatusMessage = FormatIpStatus(reply.Status)
            };
        }
        catch (OperationCanceledException)
        {
            return new PingResult
            {
                IsOnline = false,
                RoundTripTimeMs = -1,
                StatusMessage = "Scan cancelled"
            };
        }
        catch (PingException ex)
        {
            return new PingResult
            {
                IsOnline = false,
                RoundTripTimeMs = -1,
                StatusMessage = ex.InnerException?.Message ?? ex.Message
            };
        }
        catch (Exception ex)
        {
            return new PingResult
            {
                IsOnline = false,
                RoundTripTimeMs = -1,
                StatusMessage = ex.Message
            };
        }
    }

    private static string FormatIpStatus(IPStatus status) => status switch
    {
        IPStatus.TimedOut => "Request timed out",
        IPStatus.DestinationHostUnreachable => "Host unreachable",
        IPStatus.DestinationNetworkUnreachable => "Network unreachable",
        IPStatus.DestinationPortUnreachable => "Port unreachable",
        IPStatus.TimeExceeded => "TTL expired in transit",
        _ => status.ToString()
    };
}

