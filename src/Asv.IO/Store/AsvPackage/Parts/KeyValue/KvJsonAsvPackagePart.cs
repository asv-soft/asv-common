using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using Newtonsoft.Json;
using ZLogger;

namespace Asv.IO;

public class KvJsonAsvPackagePart(
    Uri uriPart,
    string contentType,
    CompressionOption compression,
    AsvPackageContext context
) : AsvPackagePart(context)
{
    private const string StaticHeader0 =
        "|====================================================================== |";
    private const string StaticHeader1 =
        "| This file contains key/value in JSON format. Do not edit it manually. |";
    private const string StaticHeader2 =
        "| See more at https://github.com/asv-soft/asv-common                    |";

    public void Save(params IEnumerable<KeyValuePair<string, string>> values)
    {
        using (Context.Lock.EnterScope())
        {
            EnsureWriteAccess();
            if (Context.Package.PartExists(uriPart))
            {
                Context.Logger.ZLogWarning(
                    $"Redefining k/v pairs in the record file for the part {uriPart}"
                );
                Context.Package.DeletePart(uriPart);
            }
            var part = Context.Package.CreatePart(uriPart, contentType, compression);
            using var stream = part.GetStream();
            using var writer = new StreamWriter(stream);
            using var jsonWriter = new JsonTextWriter(writer) { CloseOutput = true };
            jsonWriter.WriteComment(StaticHeader0);
            jsonWriter.WriteRaw("\n");
            jsonWriter.WriteComment(StaticHeader1);
            jsonWriter.WriteRaw("\n");
            jsonWriter.WriteComment(StaticHeader2);
            jsonWriter.WriteRaw("\n");
            jsonWriter.WriteComment(StaticHeader0);
            jsonWriter.WriteRaw("\n");

            jsonWriter.WriteStartObject();
            var cnt = 0;
            foreach (var param in values)
            {
                jsonWriter.WritePropertyName(param.Key);
                jsonWriter.WriteValue(param.Value);
                jsonWriter.WriteRaw("\n");
                cnt++;
            }

            jsonWriter.WriteEndObject();
            jsonWriter.Flush();
            writer.Flush();
            stream.Flush();
            Context.Logger.ZLogDebug($"Written '{cnt}' k/v pairs for the part {uriPart}");
        }
    }

    public void Load(Action<KeyValuePair<string, string>> visitor)
    {
        using (Context.Lock.EnterScope())
        {
            EnsureReadAccess();
            if (!Context.Package.PartExists(uriPart))
            {
                Context.Logger.ZLogWarning(
                    $"No k/v pairs defined in the file for the part {uriPart}"
                );
                return;
            }

            var part = Context.Package.GetPart(uriPart);
            using var stream = part.GetStream();
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader) { SupportMultipleContent = true };

            try
            {
                // Ищем корневой объект и обрабатываем только его
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType != JsonToken.StartObject)
                    {
                        // Пропускаем мусорные корневые сущности (массивы/скаляры/комментарии)
                        TrySkip(jsonReader);
                        continue;
                    }

                    // Внутри объекта ожидаем PropertyName -> Value
                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType == JsonToken.EndObject)
                        {
                            return;
                        }

                        if (jsonReader.TokenType != JsonToken.PropertyName)
                        {
                            // Нестандартный токен внутри объекта — аккуратно пропустим
                            TrySkip(jsonReader);
                            continue;
                        }

                        var name = jsonReader.Value as string ?? string.Empty;

                        if (!jsonReader.Read())
                        {
                            Context.Logger.ZLogWarning(
                                $"Unexpected end of JSON for {uriPart}, property '{name}' has no value"
                            );
                            return;
                        }

                        // Пытаемся прочитать скаляр как строку (числа/bool/date приводим к строке InvariantCulture)
                        if (!TryReadScalarAsString(jsonReader, out var valueStr))
                        {
                            Context.Logger.ZLogWarning(
                                $"Skipping param {name}: value is not a scalar (token={jsonReader.TokenType})"
                            );

                            // если значение — контейнер, аккуратно пропустим его содержимое
                            TrySkip(jsonReader);
                            continue;
                        }

                        try
                        {
                            visitor(new KeyValuePair<string, string>(name, valueStr));
                        }
                        catch (Exception e)
                        {
                            Context.Logger.ZLogError(
                                $"Failed to parse param {name} with value '{valueStr}' from {uriPart}: {e.Message}"
                            );

                            // продолжаем со следующими полями
                        }
                    }

                    // если вышли из цикла не встретив EndObject — это странно, но выйдем
                    return;
                }
            }
            catch (JsonReaderException jre)
            {
                Context.Logger.ZLogError(
                    $"Malformed JSON in {uriPart}: {jre.Message} at line {jre.LineNumber}, pos {jre.LinePosition}"
                );
            }
        }
    }

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

    private static bool TryReadScalarAsString(JsonTextReader r, out string value)
    {
        switch (r.TokenType)
        {
            case JsonToken.String:
                value = (string?)r.Value ?? string.Empty;
                return true;
            case JsonToken.Integer:
            case JsonToken.Float:
            case JsonToken.Boolean:
            case JsonToken.Date:
                value = Convert.ToString(r.Value, CultureInfo.InvariantCulture) ?? string.Empty;
                return true;
            case JsonToken.Null:
                value = string.Empty;
                return false;
            default:
                value = string.Empty;
                return false;
        }
    }

    public override void Flush()
    {
        // nothing
    }
}
