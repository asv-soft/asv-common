using System;

namespace Asv.IO;

public abstract class DeviceId : IEquatable<DeviceId>
{
    public string DeviceClass { get; }
    public bool Equals(DeviceId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(DeviceClass, other.DeviceClass, StringComparison.InvariantCultureIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is DeviceId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.InvariantCultureIgnoreCase.GetHashCode(DeviceClass);
    }

    public static bool operator ==(DeviceId? left, DeviceId? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(DeviceId? left, DeviceId? right)
    {
        return !Equals(left, right);
    }

    protected DeviceId(string deviceClass)
    {
        DeviceClass = deviceClass;
    }

    
    public abstract string AsString();
    public override string ToString()
    {
        return AsString();
    }
}