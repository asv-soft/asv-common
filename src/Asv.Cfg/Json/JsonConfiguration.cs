using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
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
        private readonly IFileSystem _fileSystem;

        public JsonConfiguration(string folderPath, ILogger? logger = null, IFileSystem? fileSystem = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _fileSystem = fileSystem ?? new FileSystem();
            ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
            _folderPath = _fileSystem.Path.GetFullPath(folderPath);
            if (!_fileSystem.Directory.Exists(folderPath))
            {
                _logger.ZLogDebug($"Directory not exist. Create '{folderPath}' for configuration");
                _fileSystem.Directory.CreateDirectory(folderPath);
            }

            _serializer = JsonHelper.CreateDefaultJsonSerializer();
        }

        public string WorkingFolder => _folderPath;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetFilePath(string key)
        {
            return _fileSystem.Path.Combine(_folderPath, $"{key}.json");
        }
        
        public IEnumerable<string> AvailableParts
            =>
                _fileSystem.Directory.EnumerateFiles(_folderPath, FixedSearchPattern)
                    .Select(_fileSystem.Path.GetFileNameWithoutExtension)!;
                
        public bool Exist(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            return _lock.Execute(key,GetFilePath(key),InternalExist);
        }

        private bool InternalExist(string path)
        {
            return _fileSystem.File.Exists(path);
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
                using var stream = _fileSystem.File.OpenRead(path);
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
            using var file = _fileSystem.File.CreateText(filepath);
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
            if (!_fileSystem.File.Exists(path)) return;
            _logger.ZLogTrace($"Delete configuration file '{path}'");
            _fileSystem.File.Delete(path);
        }

        public void Dispose()
        {
            
        }
    }
}
