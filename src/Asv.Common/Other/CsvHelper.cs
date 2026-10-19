using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Asv.Common
{
    public static class CsvHelper
    {
        public static void SaveToCsv<T>(
            IEnumerable<T> items,
            string fileName,
            string separator,
            string shieldSymbol,
            params CsvColumn<T>[] columns
        )
        {
            using (var file = new StreamWriter(File.OpenWrite(fileName), Encoding.UTF8))
            {
                foreach (var csvColumn in columns)
                {
                    file.Write(csvColumn.Name);
                    file.Write(separator);
                }
                file.WriteLine();

                foreach (var item in items)
                {
                    foreach (var csvColumn in columns)
                    {
                        var value = csvColumn.Render(item) ?? string.Empty;
                        file.Write(value.Replace(separator, shieldSymbol));
                        file.Write(separator);
                    }
                    file.WriteLine();
                }
            }
        }
    }
}
