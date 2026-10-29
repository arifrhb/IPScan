using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace IPScan.Services.Scanning;

/// <summary>
/// Multi-tiered discovery engine for logged-in usernames on Windows networks.
/// Utilizes local environment fast-path and authenticated remote WMI
/// (Win32_ComputerSystem and interactive desktop session explorer.exe owner).
/// </summary>
public static class WmiUserService
{
    private static readonly Lazy<HashSet<IPAddress>> LocalAddresses = new(GetLocalAddresses);

    private static HashSet<IPAddress> GetLocalAddresses()
    {
        var set = new HashSet<IPAddress> { IPAddress.Loopback, IPAddress.IPv6Loopback };
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    set.Add(ua.Address);
                }
            }
        }
        catch
        {
            // Ignore
        }
        return set;
    }

    public static bool IsLocalAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return true;
        return LocalAddresses.Value.Contains(address);
    }

    public static Task<string?> QueryLoggedInUserAsync(
        IPAddress address,
        int timeoutMs,
        CancellationToken cancellationToken = default)
        => QueryLoggedInUserAsync(address, null, null, timeoutMs, cancellationToken);

    public static Task<string?> QueryLoggedInUserAsync(
        IPAddress address,
        string? hostName = null,
        int timeoutMs = 2000,
        CancellationToken cancellationToken = default)
        => QueryLoggedInUserAsync(address, hostName, null, timeoutMs, cancellationToken);

    public static async Task<string?> QueryLoggedInUserAsync(
        IPAddress address,
        string? dnsHostName = null,
        string? netBiosName = null,
        int timeoutMs = 2000,
        CancellationToken cancellationToken = default)
    {
        // Tier 1: Local machine / loopback fast-path
        if (IsLocalAddress(address))
        {
            try
            {
                string domain = Environment.UserDomainName;
                string user = Environment.UserName;
                if (!string.IsNullOrWhiteSpace(domain) && !string.Equals(domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{domain}\\{user}";
                }
                return user;
            }
            catch
            {
                return Environment.UserName;
            }
        }

        // Remote host discovery
        return await Task.Run(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return null;

                var targets = new List<string>();

                // 1. Clean DNS Hostname (Kerberos domain authentication)
                if (IsValidRemoteTarget(dnsHostName))
                {
                    targets.Add(dnsHostName!.Trim());
                }

                // 2. Clean NetBIOS Computer Name (Kerberos/NetBIOS resolution)
                if (IsValidRemoteTarget(netBiosName) && !targets.Contains(netBiosName!.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    targets.Add(netBiosName!.Trim());
                }

                // 3. Raw IP Address (NTLM fallback)
                string ipStr = address.ToString();
                if (!targets.Contains(ipStr, StringComparer.OrdinalIgnoreCase))
                {
                    targets.Add(ipStr);
                }

                // Authenticated WMI over RPC/DCOM
                foreach (var target in targets)
                {
                    if (cancellationToken.IsCancellationRequested) return null;
                    string? wmiUser = TryQueryHostWmi(target, timeoutMs, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(wmiUser))
                    {
                        return wmiUser;
                    }
                }
            }
            catch
            {
                // Defensive catch to guarantee no crashes
            }

            return null;
        }, cancellationToken);
    }

    private static bool IsValidRemoteTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target)) return false;
        target = target.Trim();
        if (target.Length < 2) return false;
        if (target.Contains('\0')) return false;

        // Discard local aliases so remote queries never accidentally query localhost
        if (target == "." ||
            target.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            target.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            target.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Must have at least one alphanumeric character
        bool hasAlphanumeric = false;
        foreach (char c in target)
        {
            if (char.IsLetterOrDigit(c))
            {
                hasAlphanumeric = true;
                break;
            }
        }

        return hasAlphanumeric;
    }

    private static string? TryQueryHostWmi(string target, int timeoutMs, CancellationToken cancellationToken)
    {
        if (!IsValidRemoteTarget(target)) return null;

        try
        {
            if (cancellationToken.IsCancellationRequested) return null;

            var connOptions = new ConnectionOptions
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMs),
                EnablePrivileges = true,
                Impersonation = ImpersonationLevel.Impersonate,
                Authentication = AuthenticationLevel.PacketPrivacy
            };

            var scope = new ManagementScope($@"\\{target}\root\cimv2", connOptions);
            scope.Connect();

            if (!scope.IsConnected) return null;

            // CRITICAL SAFEGUARD:
            // Ensure Windows WMI didn't normalize a malformed or unresolved target back to localhost ("." / local machine)
            string serverPart = scope.Path?.Server ?? string.Empty;
            if (serverPart == "." ||
                serverPart.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                serverPart.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                serverPart.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                return null; // Reject local PC fallback on remote queries
            }

            // Strategy A: Win32_ComputerSystem.UserName
            var query = new SelectQuery("Win32_ComputerSystem");
            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                searcher.Options.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
                using var results = searcher.Get();
                foreach (ManagementObject obj in results)
                {
                    if (cancellationToken.IsCancellationRequested) return null;
                    var user = obj["UserName"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(user))
                    {
                        return user;
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested) return null;

            // Strategy B: Owner of active interactive explorer.exe desktop session
            var procQuery = new SelectQuery("Win32_Process", "Name = 'explorer.exe'");
            using (var procSearcher = new ManagementObjectSearcher(scope, procQuery))
            {
                procSearcher.Options.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
                using var procResults = procSearcher.Get();
                foreach (ManagementObject proc in procResults)
                {
                    if (cancellationToken.IsCancellationRequested) return null;
                    var outParams = proc.InvokeMethod("GetOwner", null, null);
                    if (outParams != null)
                    {
                        string? u = outParams["User"]?.ToString();
                        string? d = outParams["Domain"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(u))
                        {
                            return !string.IsNullOrWhiteSpace(d) ? $"{d}\\{u}" : u;
                        }
                    }
                }
            }
        }
        catch
        {
            // Host unreachable, firewall blocked, or access denied
        }

        return null;
    }
}
