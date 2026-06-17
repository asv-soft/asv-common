namespace Asv.Store;

internal static class ChimpTimestampFormat
{
    public static ReadOnlySpan<byte> Version1Header => "ASVTS1"u8;

    public static bool TryReadVersion1Header(
        ReadOnlyMemory<byte> payload,
        out ReadOnlyMemory<byte> body
    )
    {
        var header = Version1Header;
        if (payload.Length >= header.Length && payload.Span[..header.Length].SequenceEqual(header))
        {
            body = payload[header.Length..];
            return true;
        }

        body = payload;
        return false;
    }
}
