using System;

namespace Asv.IO.Device;

public class ExampleDeviceId(string @class, byte targetId)
    : DeviceId(@class),
        IEquatable<ExampleDeviceId>
{
    public bool Equals(ExampleDeviceId? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return base.Equals(other) && TargetId == other.TargetId;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ExampleDeviceId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), TargetId);
    }

    public static bool operator ==(ExampleDeviceId? left, ExampleDeviceId? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ExampleDeviceId? left, ExampleDeviceId? right)
    {
        return !Equals(left, right);
    }

    public byte TargetId { get; } = targetId;

    public override string AsString() => $"{DeviceClass}.{TargetId}";
}
