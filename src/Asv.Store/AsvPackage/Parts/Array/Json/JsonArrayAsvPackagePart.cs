using System.IO.Packaging;
using System.Text;
using Asv.IO;
using Newtonsoft.Json;

namespace Asv.Store;

public class JsonArrayAsvPackagePart<TRow>(
    Uri path,
    AsvPackageContext context,
    AsvPackagePart? parent,
    string contentType = "application/jsonl",
    CompressionOption compression = CompressionOption.Maximum,
    Encoding? encoding = null
) : ArrayAsvPackagePart<TRow>(path, context, parent, contentType, compression)
{
    private const string StaticHeader0 =
        "|============================================================================ |";
    private const string StaticHeader1 =
        "| This file contains table data in JSONL format. Do not edit it manually.     |";

    protected override async ValueTask InternalRead(
        Stream stream,
        Action<TRow> visitor,
        CancellationToken cancel
    )
    {
        await using var rdr = new JsonTextReader(
            new StreamReader(
                stream,
                encoding ?? JsonPackageSettings.DefaultEncoding,
                leaveOpen: true
            )
        )
        {
            SupportMultipleContent = true,
            CloseInput = false,
        };
        for (var i = 0; i < 3; i++)
        {
            if (await rdr.ReadAsync(cancel) == false || rdr.TokenType != JsonToken.Comment)
            {
                throw new JsonSerializationException(
                    "Expected a comment at the start of the file."
                );
            }
        }

        var index = 0;
        while (await rdr.ReadAsync(cancel))
        {
            if (rdr.TokenType != JsonToken.Comment)
            {
                throw new JsonSerializationException(
                    $"Expected a comment before row ={index: 0000}"
                );
            }

            var data = JsonPackageSettings.Serializer.Deserialize<TRow>(rdr);
            if (data == null)
            {
                throw new JsonSerializationException($"Failed to deserialize row={index: 0000}");
            }
            visitor(data);

            if (await rdr.ReadAsync(cancel) == false || rdr.TokenType != JsonToken.Comment)
            {
                throw new JsonSerializationException(
                    $"Expected a comment at the end of row={index: 0000}"
                );
            }

            index++;
        }
    }

    protected override ValueTask InternalWrite(
        Stream stream,
        IEnumerable<TRow> values,
        CancellationToken cancel
    )
    {
        using var sw = new JsonTextWriter(new StreamWriter(stream, encoding ?? Encoding.UTF8));
        sw.Formatting = Formatting.None;
        sw.CloseOutput = true;
        sw.WriteComment(StaticHeader0);
        sw.WriteRaw("\n");
        sw.WriteComment(StaticHeader1);
        sw.WriteRaw("\n");
        sw.WriteComment(StaticHeader0);
        sw.WriteRaw("\n");
        var index = 0;
        foreach (var value in values)
        {
            sw.WriteComment($"{index:0000}");
            JsonPackageSettings.Serializer.Serialize(sw, value);
            sw.WriteComment($"{index:0000}");
            sw.WriteRaw("\n");
            index++;
        }

        return ValueTask.CompletedTask;
    }
}
