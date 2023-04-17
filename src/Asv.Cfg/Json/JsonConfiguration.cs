using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Asv.Cfg.Json
{
    public class JsonConfiguration:IConfiguration
    {
        private readonly string _folderPath;
        private const string SearchPattern = "*.json";

        public JsonConfiguration(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(folderPath));
            _folderPath = folderPath;
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        }

        public IEnumerable<string> AvailableParts
            =>
                Directory.EnumerateFiles(_folderPath, SearchPattern)
                    .Select(Path.GetFileNameWithoutExtension);
                

        public bool Exist<TPocoType>(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            return File.Exists(Path.Combine(_folderPath, key + ".json"));
        }

        public TPocoType Get<TPocoType>(string key, TPocoType defaultValue)
        {
            ConfigurationHelper.ValidateKey(key);
            return Exist<TPocoType>(key) ? JsonConvert.DeserializeObject<TPocoType>(File.ReadAllText(Path.Combine(_folderPath, key + ".json"))) : defaultValue;
        }

        public void Set<TPocoType>(string key, TPocoType value)
        {
            ConfigurationHelper.ValidateKey(key);
            File.WriteAllText(Path.Combine(_folderPath, key + ".json"),JsonConvert.SerializeObject(value, Formatting.Indented));
        }

        public void Remove(string key)
        {
            ConfigurationHelper.ValidateKey(key);
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
