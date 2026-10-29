using System.Net;
using IPScan.Models;

namespace IPScan.Services.Scanning;

/// <summary>
/// Coordinates asynchronous network discovery with bounded concurrency and immediate cancellation response.
/// </summary>
public sealed class NetworkScanner
{
    public async Task<List<ScanResult>> ScanAsync(
        IReadOnlyList<ScanTarget> targets,
        ScanOptions options,
        IProgress<ScanProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        if (targets == null || targets.Count == 0)
        {
            return new List<ScanResult>();
        }

        var results = new List<ScanResult>(targets.Count);
        int totalCount = targets.Count;
        int completedCount = 0;
        int onlineCount = 0;
        int offlineCount = 0;
        object lockObj = new();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, options.Concurrency),
            CancellationToken = cancellationToken
        };

        try
        {
            await Parallel.ForEachAsync(targets, parallelOptions, async (target, ct) =>
            {
                if (ct.IsCancellationRequested) return;

                var result = await ScanSingleTargetAsync(target.Address, options, ct);

                if (ct.IsCancellationRequested) return;

                lock (lockObj)
                {
                    results.Add(result);
                    completedCount++;
                    if (result.Status == HostStatus.Online)
                    {
                        onlineCount++;
                    }
                    else
                    {
                        offlineCount++;
                    }

                    progress?.Report(new ScanProgressReport
                    {
                        TotalTargets = totalCount,
                        ScannedCount = completedCount,
                        OnlineCount = onlineCount,
                        OfflineCount = offlineCount,
                        LatestResult = result
                    });
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Scanning was stopped promptly by the user
        }

        // Return all results collected up to the moment of cancellation
        lock (lockObj)
        {
            return results.OrderBy(r => ScanTarget.ToUint32(r.Address)).ToList();
        }
    }

    private static async Task<ScanResult> ScanSingleTargetAsync(
        IPAddress address,
        ScanOptions options,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new ScanResult
            {
                Address = address,
                Status = HostStatus.Offline,
                RoundTripTimeMs = -1,
                StatusMessage = "Scan stopped"
            };
        }

        // 1. Initial ICMP Echo check
        var pingResult = await PingService.PingHostAsync(address, options.TimeoutMs, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return new ScanResult
            {
                Address = address,
                Status = HostStatus.Offline,
                RoundTripTimeMs = -1,
                StatusMessage = "Scan stopped"
            };
        }

        if (!pingResult.IsOnline)
        {
            return new ScanResult
            {
                Address = address,
                Status = HostStatus.Offline,
                RoundTripTimeMs = -1,
                StatusMessage = pingResult.StatusMessage,
                Hostname = null,
                NetBiosName = null,
                LoggedInUser = null
            };
        }

        // 2. Host is online - resolve DNS and NetBIOS concurrently
        Task<string?> dnsTask = options.ResolveDns
            ? DnsService.ResolveHostnameAsync(address, options.TimeoutMs + 500, cancellationToken)
            : Task.FromResult<string?>(null);

        Task<NetBiosResult?> netBiosTask = options.ResolveNetBios
            ? NetBiosService.QueryNetBiosAsync(address, options.TimeoutMs, cancellationToken)
            : Task.FromResult<NetBiosResult?>(null);

        try
        {
            await Task.WhenAll(dnsTask, netBiosTask);
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new ScanResult
            {
                Address = address,
                Status = HostStatus.Online,
                RoundTripTimeMs = pingResult.RoundTripTimeMs,
                StatusMessage = "Scan stopped"
            };
        }

        string? hostname = dnsTask.IsCompletedSuccessfully ? await dnsTask : null;
        var netBios = netBiosTask.IsCompletedSuccessfully ? await netBiosTask : null;
        string? netBiosName = netBios?.FormattedName;

        // 3. Resolve Logged-in Username via authorized WMI and NetBIOS discovery
        string? loggedInUser = null;
        if (options.QueryLoggedInUser && !cancellationToken.IsCancellationRequested)
        {
            loggedInUser = await WmiUserService.QueryLoggedInUserAsync(
                address,
                dnsHostName: hostname,
                netBiosName: netBios?.ComputerName,
                timeoutMs: options.TimeoutMs + 500,
                cancellationToken: cancellationToken);

            // Fallback to NetBIOS discovered registered user name if WMI was unreachable or restricted
            if (string.IsNullOrWhiteSpace(loggedInUser) && !string.IsNullOrWhiteSpace(netBios?.LoggedInUser))
            {
                loggedInUser = netBios.LoggedInUser;
            }
        }

        return new ScanResult
        {
            Address = address,
            Status = HostStatus.Online,
            RoundTripTimeMs = pingResult.RoundTripTimeMs,
            StatusMessage = pingResult.StatusMessage,
            Hostname = hostname,
            NetBiosName = netBiosName,
            LoggedInUser = loggedInUser
        };
    }
}
