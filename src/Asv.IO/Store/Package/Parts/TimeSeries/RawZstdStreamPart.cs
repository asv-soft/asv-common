using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;

namespace Asv.IO;

public record VisitableRecord(uint index, DateTime timestamp, string id, IVisitable data)
{
    public uint Index { get; init; } = index;
    public DateTime Timestamp { get; init; } = timestamp;
    public string Id { get; init; } = id;
    public IVisitable Data { get; init; } = data;
}

public class RawZstdStreamPart(
    Uri uriPart,
    string contentType,
    uint flushCount,
    AsvFileContext context
) : AsvFilePart(context)
{
    private readonly Dictionary<string, ChimpRecordEncoder> _files = new();

    public void Write(VisitableRecord record)
    {
        if (_files.TryGetValue(record.Id, out var file) == false)
        {
            if (file == null)
            {
                var uri = new Uri(uriPart + record.Id, UriKind.Relative);
                if (Context.Package.PartExists(uri))
                {
                    Context.Package.DeletePart(uri);
                }
                var part = Context.Package.CreatePart(uri, contentType, CompressionOption.Maximum);
                var stream = part.GetStream();
                file = AddToDispose(new ChimpRecordEncoder(record, stream, flushCount));
                _files.Add(record.Id, file);
            }
        }

        file.Append(record);
    }

    public void Read(
        Action<(VisitableRecord, object)> visitor,
        Func<string, (IVisitable, object)?> factory
    )
    {
        var items = Context
            .Package.GetParts()
            .Where(x => x.Uri.ToString().StartsWith(uriPart.ToString()));
        foreach (var item in items)
        {
            using var stream = item.GetStream();
            var idString = item.Uri.ToString().Substring(uriPart.ToString().Length);
            var msg = factory(idString);
            if (msg == null)
            {
                continue;
            }

            // ReSharper disable once NullableWarningSuppressionIsUsed
            using var reader = new ChimpRecordDecoder(
                idString,
                () => factory(idString)!.Value,
                stream
            );
            while (reader.Read(visitor)) { }
        }
    }

    public override void Flush()
    {
        foreach (var value in _files.Values)
        {
            value.Flush();
        }
    }
}
