using System;

namespace Asv.IO;

public class ProtocolInfo(string id, string name) : IEquatable<ProtocolInfo>
{
    public string Id { get; } = id;
    public string Name { get; } = name;

    public override string ToString()
    {
        return $"{Name}[{Id}]";
    }

    public bool Equals(ProtocolInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id && Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((ProtocolInfo)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }

    public static bool operator ==(ProtocolInfo? left, ProtocolInfo? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ProtocolInfo? left, ProtocolInfo? right)
    {
        return !Equals(left, right);
    }
}
