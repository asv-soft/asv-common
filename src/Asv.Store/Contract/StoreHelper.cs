using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LiteDB;

namespace Asv.Store
{
    public static class StoreHelper
    {
        public static void SaveToCsv(this IStore store, string folderPath, string csvSeparator = ";", string objectNameSeparator = ".", IFormatProvider format = null)
        {
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var dictFolder = Path.Combine(folderPath, "dict");
            if (Directory.Exists(dictFolder)) Directory.Delete(dictFolder, true);
            Directory.CreateDirectory(dictFolder);
            foreach (var dictName in store.Dicts)
            {
                var dict = store.GetDict(dictName);
                dict.SaveToCsv(Path.Combine(dictFolder, $"{dictName}.csv"), csvSeparator, objectNameSeparator, format);
            }

            var textFolder = Path.Combine(folderPath, "text");
            if (Directory.Exists(textFolder)) Directory.Delete(textFolder, true);
            Directory.CreateDirectory(textFolder);
            foreach (var textName in store.Texts)
            {
                var text = store.GetText(textName);
                text.SaveToCsv(Path.Combine(textFolder, $"{textName}.csv"), csvSeparator, objectNameSeparator);
            }

            var doubleFolder = Path.Combine(folderPath, "double");
            if (Directory.Exists(doubleFolder)) Directory.Delete(doubleFolder, true);
            Directory.CreateDirectory(doubleFolder);
            foreach (var doubleName in store.DoubleSeries)
            {
                var doubleSeries = store.GetDoubleSeries<BsonValue>(doubleName);
                doubleSeries.SaveToCsv(Path.Combine(doubleFolder, $"{doubleName}.csv"), csvSeparator, objectNameSeparator, format);
            }
        }

        public static void SaveToCsv(this IKeyValueStore store, string filePath,string csvSeparator = ";", string objectNameSeparator = ".", IFormatProvider format = null)
        {
            format = format ?? CultureInfo.InvariantCulture;

            var names = store.Ids.Select(store.Read).SelectMany(_ => _.ToFlat(store.Name, objectNameSeparator, format)).Select(_ => _.Key).Distinct().ToArray();
            using (var sw = File.AppendText(filePath))
            {
                sw.Write("Name");
                sw.Write(csvSeparator);
                foreach (var name in names)
                {
                    sw.Write(name);
                    sw.Write(csvSeparator);
                }
                sw.WriteLine();
                foreach (var storeId in store.Ids)
                {
                    sw.Write(storeId);
                    sw.Write(csvSeparator);
                    var value = store.Read(storeId).ToFlat(store.Name, objectNameSeparator, format).ToArray();
                    foreach (var t in names)
                    {
                        foreach (var keyValuePair in value)
                        {
                            if (keyValuePair.Key == t)
                            {
                                sw.Write(keyValuePair.Value);
                            }
                        }
                        sw.Write(csvSeparator);
                    }
                    sw.WriteLine();
                }
            }
            
        }

        public static void SaveToCsv(this ITextStore store, string filePath, string csvSeparator = ";", string objectNameSeparator = ".")
        {
            using (var sw = File.AppendText(filePath))
            {
                sw.Write(nameof(TextMessage.Id));
                sw.Write(csvSeparator);
                sw.Write(nameof(TextMessage.Date));
                sw.Write(csvSeparator);
                sw.Write(nameof(TextMessage.IntTag));
                sw.Write(csvSeparator);
                sw.Write(nameof(TextMessage.StrTag));
                sw.Write(csvSeparator);
                sw.Write(nameof(TextMessage.Text));
                sw.Write(csvSeparator);
                sw.WriteLine();

                foreach (var textMessage in store.Find(new TextMessageQuery()))
                {
                    sw.Write(textMessage.Id);
                    sw.Write(csvSeparator);
                    sw.Write(textMessage.Date);
                    sw.Write(csvSeparator);
                    sw.Write(textMessage.IntTag.ToString("X2"));
                    sw.Write(csvSeparator);
                    sw.Write(textMessage.StrTag);
                    sw.Write(csvSeparator);
                    sw.Write(textMessage.Text);
                    sw.Write(csvSeparator);
                    sw.WriteLine();
                }
            }

            
        }

        public static void SaveToCsv(this ISeriesValueStore<double, BsonValue> series, string filePath, string csvSeparator = "\t", string objectNameSeparator = ".", IFormatProvider format = null)
        {
            format = format ?? CultureInfo.InvariantCulture;
            var names = series.Read(new SeriesQuery<double> { Skip = 0, Take = int.MaxValue }).SelectMany(_ =>_.Y.ToFlat(series.Name, objectNameSeparator, format)).Select(_ => _.Key).Distinct().ToArray();
            using (var sw = File.AppendText(filePath))
            {
                sw.Write("X");
                sw.Write(csvSeparator);
                foreach (var name in names)
                {
                    sw.Write(name);
                    sw.Write(csvSeparator);
                }
                sw.WriteLine();

                foreach (var seriesPoint in series.Read(new SeriesQuery<double> {Skip = 0, Take = int.MaxValue}))
                {
                    sw.Write(seriesPoint.X.ToString(format));
                    sw.Write(csvSeparator);
                    var value = seriesPoint.Y.ToFlat(series.Name, objectNameSeparator, format).ToArray();
                    foreach (var t in names)
                    {
                        foreach (var keyValuePair in value)
                        {
                            if (keyValuePair.Key == t)
                            {
                                sw.Write(keyValuePair.Value);
                            }
                        }
                        sw.Write(csvSeparator);
                    }
                    sw.WriteLine();
                }
            }
        }

        public static IEnumerable<KeyValuePair<string, string>> ToFlat(this BsonValue value, string name,
            string joinSymbol, IFormatProvider format)
        {
            if (value.IsDateTime) return PrintDateTime(name, value.AsDateTime, format);
            if (value.IsInt64) return PrintDateTime(name, value.AsInt64, format);
            if (value.IsDecimal) return PrintDateTime(name, value.AsDecimal, format);
            if (value.IsDocument) return value.AsDocument.ToFlat(name, joinSymbol, format);
            if (value.IsArray) return value.AsArray.ToFlat(name, joinSymbol, format);
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(name,value.ToString())
            };
        }

        private static IEnumerable<KeyValuePair<string, string>> PrintDateTime(string name, decimal value,
            IFormatProvider format)
        {
            yield return new KeyValuePair<string, string>(name, value.ToString("G", format));
        }

        private static IEnumerable<KeyValuePair<string, string>> PrintDateTime(string name, long value,IFormatProvider format)
        {
            yield return new KeyValuePair<string, string>(name, value.ToString(format));
        }

        private static IEnumerable<KeyValuePair<string, string>> PrintDateTime(string name, DateTime value, IFormatProvider format)
        {
            yield return new KeyValuePair<string, string>(name,value.ToString("G", format));  
        }

        public static IEnumerable<KeyValuePair<string, string>> ToFlat(this BsonDocument value, string name,
            string joinSymbol,IFormatProvider format)
        {
            foreach (var bsonValue in value.RawValue)
            {
                foreach (var items in ToFlat(bsonValue.Value, string.Concat(name, joinSymbol, bsonValue.Key), joinSymbol, format))
                {
                    yield return items;
                }
            }
        }

        public static IEnumerable<KeyValuePair<string, string>> ToFlat(this BsonArray value, string name,
            string joinSymbol, IFormatProvider format)
        {
            for (var index = 0; index < value.AsArray.RawValue.Count; index++)
            {
                var bsonValue = value.AsArray.RawValue[index];
                foreach (var items in ToFlat(bsonValue, string.Concat(name, joinSymbol, $"[{index}]"), joinSymbol, format))
                {
                    yield return items;
                }
            }
        }
    }
}
