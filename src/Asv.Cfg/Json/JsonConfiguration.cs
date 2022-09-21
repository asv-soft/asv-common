using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Asv.Cfg.Json
{
    public class JsonConfiguration:IConfiguration
    {
        private readonly string _folderPath;
        private readonly string _searchPattern;
        private string searchPattern = "*.json";

        public JsonConfiguration(string folderPath)
        {
            _folderPath = folderPath;
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            _searchPattern = searchPattern;
        }

        public IEnumerable<string> AvalableParts
            =>
                Directory.EnumerateFiles(_folderPath, _searchPattern)
                    .Select(Path.GetFileNameWithoutExtension);
                

        public bool Exist<TPocoType>(string key)
        {
            return File.Exists(Path.Combine(_folderPath, key + ".json"));
        }

        public TPocoType Get<TPocoType>(string key, TPocoType defaultValue)
        {
            if (Exist<TPocoType>(key))
            {
                return
                    JsonConvert.DeserializeObject<TPocoType>(File.ReadAllText(Path.Combine(_folderPath, key + ".json")));
            }
            else
            {
                return defaultValue;
            }
        }

        public void Set<TPocoType>(string key, TPocoType value)
        {
            File.WriteAllText(Path.Combine(_folderPath, key + ".json"),JsonConvert.SerializeObject(value, Formatting.Indented));
        }

        public void Remove(string key)
        {
            var path = Path.Combine(_folderPath, key + ".json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void Dispose()
        {
        }
    }
}
