using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace IPScan.Services.Scanning;

/// <summary>
/// Resolves hardware (MAC) addresses using Windows SendARP for local subnet devices
/// and local network adapter inspection for local/loopback IPs.
/// Formats MAC addresses standardly as AA-BB-CC-DD-EE-FF (or with colons).
/// </summary>
public static class MacAddressService
{
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(uint destIP, uint srcIP, byte[] pMacAddr, ref uint phyAddrLen);

    private const int NO_ERROR = 0;

    /// <summary>
    /// Attempts to retrieve the hardware (MAC) address for the given IPv4 address.
    /// Returns the formatted MAC address in AA-BB-CC-DD-EE-FF format, or null if unreachable or remote.
    /// </summary>
    public static async Task<string?> GetMacAddressAsync(
        IPAddress address,
        int timeoutMs = 1000,
        CancellationToken cancellationToken = default)
    {
        if (address == null || address.AddressFamily != AddressFamily.InterNetwork)
        {
            return null;
        }

        if (cancellationToken.IsCancellationRequested) return null;

        // Check if the address matches loopback (127.0.0.1) or an active adapter on this host
        string? localMac = TryGetLocalInterfaceMac(address);
        if (localMac != null)
        {
            return localMac;
        }

        // ARP is a Layer-2 broadcast protocol valid only on the local subnet.
        // If the target is on a remote routed network, ARP cannot discover its MAC address directly.
        if (!IsOnLocalSubnet(address))
        {
            return null;
        }

        try
        {
            return await Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested) return null;

                byte[] ipBytes = address.GetAddressBytes();
                // Windows IPAddr is in network byte order in memory; on little-endian x86/x64:
                uint destIP = (uint)(ipBytes[0] | (ipBytes[1] << 8) | (ipBytes[2] << 16) | (ipBytes[3] << 24));

                byte[] macAddr = new byte[6];
                uint macAddrLen = (uint)macAddr.Length;

                int ret = SendARP(destIP, 0, macAddr, ref macAddrLen);

                if (ret == NO_ERROR && macAddrLen == 6 && !macAddr.All(b => b == 0))
                {
                    // Filter out proxy-ARP gateway responses for non-gateway hosts across routers
                    if (IsProxyArpGatewayResponse(address, macAddr))
                    {
                        return null;
                    }

                    return FormatMacAddress(macAddr);
                }

                return null;
            }, cancellationToken).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (TimeoutException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Formats a 6-byte MAC address into standard uppercase "AA-BB-CC-DD-EE-FF" format (or with colons).
    /// </summary>
    public static string FormatMacAddress(byte[] bytes, char separator = '-')
    {
        if (bytes == null || bytes.Length < 6) return string.Empty;
        return string.Format("{0:X2}{6}{1:X2}{6}{2:X2}{6}{3:X2}{6}{4:X2}{6}{5:X2}",
            bytes[0], bytes[1], bytes[2], bytes[3], bytes[4], bytes[5], separator);
    }

    /// <summary>
    /// Determines whether the specified IPv4 address resides on a local subnet attached to an active network interface.
    /// </summary>
    public static bool IsOnLocalSubnet(IPAddress address)
    {
        if (address == null || address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        try
        {
            byte[] targetBytes = address.GetAddressBytes();
            uint targetIp = (uint)(targetBytes[0] | (targetBytes[1] << 8) | (targetBytes[2] << 16) | (targetBytes[3] << 24));

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                var ipProps = nic.GetIPProperties();
                foreach (var uni in ipProps.UnicastAddresses)
                {
                    if (uni.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (uni.IPv4Mask == null) continue;

                    byte[] adapterBytes = uni.Address.GetAddressBytes();
                    byte[] maskBytes = uni.IPv4Mask.GetAddressBytes();

                    uint adapterIp = (uint)(adapterBytes[0] | (adapterBytes[1] << 8) | (adapterBytes[2] << 16) | (adapterBytes[3] << 24));
                    uint mask = (uint)(maskBytes[0] | (maskBytes[1] << 8) | (maskBytes[2] << 16) | (maskBytes[3] << 24));

                    if (mask != 0 && (targetIp & mask) == (adapterIp & mask))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Defensive: if adapter inspection fails, allow SendARP to proceed
            return true;
        }

        return false;
    }

    private static bool IsProxyArpGatewayResponse(IPAddress address, byte[] macAddr)
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                var ipProps = nic.GetIPProperties();

                foreach (var gw in ipProps.GatewayAddresses)
                {
                    if (gw.Address.AddressFamily == AddressFamily.InterNetwork && !gw.Address.Equals(address))
                    {
                        // Check if gateway's MAC matches the returned MAC
                        byte[] gwIpBytes = gw.Address.GetAddressBytes();
                        uint gwDestIP = (uint)(gwIpBytes[0] | (gwIpBytes[1] << 8) | (gwIpBytes[2] << 16) | (gwIpBytes[3] << 24));
                        byte[] gwMac = new byte[6];
                        uint gwMacLen = (uint)gwMac.Length;

                        if (SendARP(gwDestIP, 0, gwMac, ref gwMacLen) == NO_ERROR && gwMacLen == 6)
                        {
                            if (gwMac.SequenceEqual(macAddr))
                            {
                                return true; // Proxy-ARP detected: non-gateway host returned the gateway's MAC
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore inspection errors
        }

        return false;
    }

    private static string? TryGetLocalInterfaceMac(IPAddress address)
    {
        try
        {
            bool isLoopback = IPAddress.IsLoopback(address);

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;

                var ipProps = nic.GetIPProperties();
                var physAddr = nic.GetPhysicalAddress();
                byte[] macBytes = physAddr.GetAddressBytes();
                if (macBytes == null || macBytes.Length != 6) continue;

                if (isLoopback)
                {
                    // For loopback (127.0.0.1), return the primary physical operational adapter's MAC
                    if (nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        return FormatMacAddress(macBytes);
                    }
                }
                else
                {
                    foreach (var uni in ipProps.UnicastAddresses)
                    {
                        if (uni.Address.AddressFamily == AddressFamily.InterNetwork && uni.Address.Equals(address))
                        {
                            return FormatMacAddress(macBytes);
                        }
                    }
                }
            }
        }
        catch
        {
            // Defensive: suppress adapter permission / WMI query issues
        }

        return null;
    }
}
