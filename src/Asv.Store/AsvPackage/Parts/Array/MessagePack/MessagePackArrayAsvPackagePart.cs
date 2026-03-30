using System.IO.Packaging;
using Asv.IO;
using MessagePack;

namespace Asv.Store;

public class MessagePackArrayAsvPackagePart<TRow>(
    Uri path,
    AsvPackageContext context,
    AsvPackagePart? parent,
    string contentType = "application/msgpack+array",
    CompressionOption compression = CompressionOption.Maximum
) : ArrayAsvPackagePart<TRow>(path, context, parent, contentType, compression)
{
    protected override async ValueTask InternalRead(
        Stream stream,
        Action<TRow> visitor,
        CancellationToken cancel
    )
    {
        using var streamReader = new MessagePackStreamReader(stream);
        while (await streamReader.ReadAsync(cancel) is { } msgpack)
        {
            visitor(MessagePackSerializer.Deserialize<TRow>(msgpack, cancellationToken: cancel));
        }
    }

    protected override ValueTask InternalWrite(
        Stream stream,
        IEnumerable<TRow> values,
        CancellationToken cancel
    )
    {
        foreach (var value in values)
        {
            MessagePackSerializer.Serialize(stream, value, cancellationToken: cancel);
        }
        return ValueTask.CompletedTask;
    }
}
