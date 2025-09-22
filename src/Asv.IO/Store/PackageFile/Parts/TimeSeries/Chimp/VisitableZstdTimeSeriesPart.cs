using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using ZLogger;

namespace Asv.IO;

public class VisitableZstdTimeSeriesPart(
    Uri uriPart,
    string contentType,
    uint flushEvery,
    AsvFileContext context,
    CompressionOption compression = CompressionOption.Maximum,
    bool useZstdForBatch = true
) : AsvFilePart(context)
{
    private readonly Dictionary<string, ChimpTableEncoder> _files = new();

    public void Write(VisitableRecord record)
    {
        using (Context.Lock.EnterScope())
        {
            EnsureWriteAccess();
            if (_files.TryGetValue(record.Id, out var file) == false)
            {
                if (file == null)
                {
                    var uri = new Uri(uriPart + record.Id, UriKind.Relative);
                    if (Context.Package.PartExists(uri))
                    {
                        Context.Package.DeletePart(uri);
                    }
                    var part = Context.Package.CreatePart(uri, contentType, compression);
                    var stream = part.GetStream();
                    file = AddToDispose(
                        new ChimpTableEncoder(
                            record,
                            stream,
                            flushEvery,
                            Context.Logger,
                            useZstdForBatch
                        )
                    );
                    _files.Add(record.Id, file);
                }
            }

            file.Append(record);
        }
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
                Context.Logger.ZLogWarning(
                    $"No factory found for record with id={idString} of part {uriPart}"
                );
                continue;
            }

            // ReSharper disable once NullableWarningSuppressionIsUsed
            using var reader = new ChimpTableDecoder(
                idString,
                () => factory(idString)!.Value,
                stream,
                flushEvery,
                Context.Logger
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
