using System.Net;

namespace IPScan.Models;

/// <summary>
/// Represents an IP address target to be scanned.
/// </summary>
public sealed class ScanTarget : IEquatable<ScanTarget>, IComparable<ScanTarget>
{
    public IPAddress Address { get; }
    public uint NumericAddress { get; }

    public ScanTarget(IPAddress address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        NumericAddress = ToUint32(address);
    }

    public static uint ToUint32(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        if (bytes.Length != 4)
        {
            return 0; // IPv6 fallback or non-IPv4
        }
        return unchecked(((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3]);
    }

    public static IPAddress FromUint32(uint value)
    {
        byte[] bytes = new byte[4]
        {
            (byte)((value >> 24) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF)
        };
        return new IPAddress(bytes);
    }

    public override string ToString() => Address.ToString();

    public bool Equals(ScanTarget? other)
    {
        if (other is null) return false;
        return NumericAddress == other.NumericAddress && Address.Equals(other.Address);
    }

    public override bool Equals(object? obj) => obj is ScanTarget target && Equals(target);

    public override int GetHashCode() => Address.GetHashCode();

    public int CompareTo(ScanTarget? other)
    {
        if (other is null) return 1;
        return NumericAddress.CompareTo(other.NumericAddress);
    }
}

