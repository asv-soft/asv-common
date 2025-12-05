using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Threading.Tasks;
using R3;
using ZLogger;

namespace Asv.IO;

public class VisitableTimeSeriesAsvPackagePart(
    Uri uriPart,
    string contentType,
    uint flushEvery,
    AsvPackageContext context,
    CompressionOption compression = CompressionOption.Maximum,
    bool useZstdForBatch = true,
    AsvPackagePart? parent = null
) : AsvPackagePart(context, parent)
{
    private readonly Dictionary<string, ChimpTableEncoder> _files = new();

    public void Write(TableRow record)
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
                    file = new ChimpTableEncoder(
                        record,
                        stream,
                        flushEvery,
                        Context.Logger,
                        useZstdForBatch
                    ).AddTo(ref DisposableBag);
                    _files.Add(record.Id, file);
                }
            }

            file.Append(record);
        }
    }

    public void Read(
        Action<(TableRow, object)> visitor,
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

    public override IEnumerable<AsvPackagePart> GetChildren()
    {
        return [];
    }

    public override void InternalFlush()
    {
        foreach (var value in _files.Values)
        {
            value.Flush();
        }
    }
}
