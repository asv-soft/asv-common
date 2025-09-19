using System;
using System.IO;
using System.IO.Packaging;
using Newtonsoft.Json;
using ZLogger;

namespace Asv.IO;

public class KvChangesJsonPart(
    Uri uriPart,
    string contentType,
    CompressionOption compression,
    AsvFileContext context
) : AsvFilePart(context)
{
    private const string StaticHeader0 =
        "|============================================================================== |";
    private const string StaticHeader1 =
        "| This file contains key/value changes in JSONL format. Do not edit it manually. |";
    private const string StaticHeader2 =
        "| See more at https://github.com/asv-soft/asv-common                            |";

    private JsonTextWriter? _jsonWriter;

    public void Append(in KeyValueChange<string, string> change)
    {
        using (Context.Lock.EnterScope())
        {
            EnsureWriteAccess();
            if (_jsonWriter == null)
            {
                if (Context.Package.PartExists(uriPart))
                {
                    Context.Logger.ZLogWarning(
                        $"Redefining mavlink param changes in the record file for the part {uriPart}"
                    );
                    Context.Package.DeletePart(uriPart);
                }

                var part = Context.Package.CreatePart(uriPart, contentType, compression);
                var stream = part.GetStream();
                var writer = new StreamWriter(stream);
                _jsonWriter = new JsonTextWriter(writer)
                {
                    Formatting = Formatting.None,
                    CloseOutput = true,
                };
                AddToDispose(_jsonWriter);
                _jsonWriter.WriteComment(StaticHeader0);
                _jsonWriter.WriteRaw("\n");
                _jsonWriter.WriteComment(StaticHeader1);
                _jsonWriter.WriteRaw("\n");
                _jsonWriter.WriteComment(StaticHeader2);
                _jsonWriter.WriteRaw("\n");
                _jsonWriter.WriteComment(StaticHeader0);
                _jsonWriter.WriteRaw("\n");
            }

            _jsonWriter.WriteStartArray();
            _jsonWriter.WriteValue(change.Timestamp);
            _jsonWriter.WriteValue(change.Key);
            _jsonWriter.WriteValue(change.OldValue);
            _jsonWriter.WriteValue(change.NewValue);
            _jsonWriter.WriteEndArray();
            _jsonWriter.WriteRaw("\n");
            _jsonWriter.Flush();
        }
    }

    public void Load(ChangeDelegate visitor)
    {
        using (Context.Lock.EnterScope())
        {
            EnsureReadAccess();
            if (!Context.Package.PartExists(uriPart))
            {
                Context.Logger.ZLogWarning(
                    $"No mavlink param changes defined in the record file for the part {uriPart}"
                );
                return;
            }

            var part = Context.Package.GetPart(uriPart);
            using var stream = part.GetStream();
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader) { SupportMultipleContent = true };
            while (jsonReader.Read())
            {
                if (jsonReader.TokenType != JsonToken.StartArray)
                {
                    continue;
                }

                // Expect: [ <Date>, <string name>, <string old>, <string @new> ]
                if (!jsonReader.Read())
                {
                    continue;
                }

                DateTime timestamp;
                if (jsonReader.TokenType == JsonToken.Date)
                {
                    timestamp = (DateTime)jsonReader.Value!;
                }
                else if (
                    jsonReader.TokenType == JsonToken.String
                    && DateTime.TryParse((string?)jsonReader.Value, out var tsParsed)
                )
                {
                    timestamp = tsParsed;
                }
                else
                {
                    TrySkip(jsonReader);
                    continue;
                }

                if (!jsonReader.Read() || jsonReader.TokenType != JsonToken.String)
                {
                    TrySkip(jsonReader);
                    continue;
                }

                var name = (string)jsonReader.Value!;

                if (!jsonReader.Read() || jsonReader.TokenType != JsonToken.String)
                {
                    TrySkip(jsonReader);
                    continue;
                }

                var oldValueStr = (string)jsonReader.Value!;

                if (!jsonReader.Read() || jsonReader.TokenType != JsonToken.String)
                {
                    TrySkip(jsonReader);
                    continue;
                }

                var newValueStr = (string)jsonReader.Value!;

                try
                {
                    if (!jsonReader.Read() || jsonReader.TokenType != JsonToken.EndArray)
                    {
                        // malformed array, skip it
                        TrySkip(jsonReader);
                        continue;
                    }

                    visitor(
                        new KeyValueChange<string, string>(
                            timestamp,
                            name,
                            oldValueStr,
                            newValueStr
                        )
                    );
                }
                catch (Exception e)
                {
                    Context.Logger.ZLogError(
                        $"Failed to parse param change record for the part {uriPart}: {e.Message}"
                    );
                    TrySkip(jsonReader);
                }
            }
        }
    }

    public delegate void ChangeDelegate(in KeyValueChange<string, string> change);

    private static void TrySkip(JsonTextReader r)
    {
        try
        {
            r.Skip();
        }
        catch
        {
            /* ignore errors */
        }
    }

    public override void Flush()
    {
        _jsonWriter?.Flush();
    }
}

public readonly struct KeyValueChange<TKey, TValue>(
    DateTime timestamp,
    TKey key,
    TValue oldValue,
    TValue newValue
)
{
    public DateTime Timestamp { get; } = timestamp;
    public TKey Key { get; } = key;
    public TValue OldValue { get; } = oldValue;
    public TValue NewValue { get; } = newValue;
}
