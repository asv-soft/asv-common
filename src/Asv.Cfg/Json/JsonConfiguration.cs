using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using ZLogger;

namespace Asv.Cfg.Json
{
    public class JsonConfiguration:IConfiguration
    {
        private readonly string _folderPath;
        private const string FixedSearchPattern = "*.json";
        private readonly LockByKeyExecutor<string> _lock = new(ConfigurationHelper.DefaultKeyComparer);
        private readonly ILogger _logger;
        private readonly JsonSerializer _serializer;

        public JsonConfiguration(string folderPath, ILogger? logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
            _folderPath = Path.GetFullPath(folderPath);
            if (!Directory.Exists(folderPath))
            {
                _logger.ZLogDebug($"Directory not exist. Create '{folderPath}' for configuration");
                Directory.CreateDirectory(folderPath);
            }

            _serializer = JsonHelper.CreateDefaultJsonSerializer();
        }

        public string WorkingFolder => _folderPath;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetFilePath(string key)
        {
            return Path.Combine(_folderPath, $"{key}.json");
        }
        
        public IEnumerable<string> AvailableParts
            =>
                Directory.EnumerateFiles(_folderPath, FixedSearchPattern)
                    .Select(Path.GetFileNameWithoutExtension)!;
                
        public bool Exist(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            return _lock.Execute(key,GetFilePath(key),InternalExist);
        }

        private bool InternalExist(string path)
        {
            return File.Exists(path);
        }

        public TPocoType Get<TPocoType>(string key, Lazy<TPocoType> defaultValue)
        {
            ConfigurationHelper.ValidateKey(key);
            return _lock.Execute(key,GetFilePath(key),defaultValue,InternalGet);
        }
        
        private TPocoType InternalGet<TPocoType>(string path,Lazy<TPocoType> defaultValue)
        {
            if (InternalExist(path))
            {
                using var stream = File.OpenRead(path);
                using var reader = new StreamReader(stream);
                using var jsonReader = new JsonTextReader(reader);
                return _serializer.Deserialize<TPocoType>(jsonReader) ?? throw new InvalidOperationException();
            }
            var value = defaultValue.Value;
            InternalSet(path, value);
            return value;
        }

        public void Set<TPocoType>(string key, TPocoType value)
        {
            ConfigurationHelper.ValidateKey(key);
            _lock.Execute(key, GetFilePath(key),value, InternalSet);
        }
        
        private void InternalSet<TPocoType>(string filepath, TPocoType value)
        {
            InternalRemove(filepath);
            _logger.ZLogTrace($"Create configuration file '{filepath}'");
            using var file = File.CreateText(filepath);
            _serializer.Serialize(file, value);
            file.Flush();
        }

        public void Remove(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            _lock.Execute(key,GetFilePath(key),InternalRemove);
        }
        private void InternalRemove(string path)
        {
            if (!File.Exists(path)) return;
            _logger.ZLogTrace($"Delete configuration file '{path}'");
            File.Delete(path);
        }

        public void Dispose()
        {
            
        }
    }
}
