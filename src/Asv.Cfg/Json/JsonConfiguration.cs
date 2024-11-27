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
using R3;
using ZLogger;

namespace Asv.Cfg
{
    public class JsonConfiguration:IConfiguration
    {
        private readonly string _folderPath;
        private const string FixedSearchPattern = "*.json";
        private readonly LockByKeyExecutor<string> _lock = new(ConfigurationHelper.DefaultKeyComparer);
        private readonly ILogger _logger;
        private readonly JsonSerializer _serializer;
        private readonly IFileSystem _fileSystem;
        private readonly Subject<ConfigurationException> _onError;

        public JsonConfiguration(string folderPath, ILogger? logger = null, IFileSystem? fileSystem = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _onError = new Subject<ConfigurationException>();
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

        public IEnumerable<string> ReservedParts => Array.Empty<string>();

        public IEnumerable<string> AvailableParts
        {
            get
            {
                try
                {

                    return _fileSystem.Directory.EnumerateFiles(_folderPath, FixedSearchPattern)
                        .Select(_fileSystem.Path.GetFileNameWithoutExtension)
                        .Select(x => x ?? string.Empty);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        public bool Exist(string key)
        {
            try
            {
                ConfigurationHelper.ValidateKey(key);
                return _lock.Execute(key,GetFilePath(key),InternalExist);
            }
            catch (Exception e)
            {
                _logger.ZLogError(e,$"Error to check exist '{key}' part:{e.Message}");
                var ex = new ConfigurationException($"Error to check exist '{key}' part",e);
                _onError.OnNext(ex);
                throw ex;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool InternalExist(string path) => _fileSystem.File.Exists(path);

        public TPocoType Get<TPocoType>(string key, Lazy<TPocoType> defaultValue)
        {
            try
            {
                ConfigurationHelper.ValidateKey(key);
                return _lock.Execute(key,GetFilePath(key),defaultValue,InternalGet);
            }
            catch (Exception e)
            {
                _logger.ZLogError(e,$"Error to get '{key}' part:{e.Message}");
                var ex = new ConfigurationException($"Error to get exist '{key}' part",e);
                _onError.OnNext(ex);
                throw ex;
            }
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
            try
            {
                ConfigurationHelper.ValidateKey(key);
                _lock.Execute(key, GetFilePath(key),value, InternalSet);
            }
            catch (Exception e)
            {
                _logger.ZLogError(e,$"Error to set '{key}' part:{e.Message}");
                var ex = new ConfigurationException($"Error to set exist '{key}' part",e);
                _onError.OnNext(ex);
                throw ex;
            }
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
            try
            {
                ConfigurationHelper.ValidateKey(key);
                _lock.Execute(key,GetFilePath(key),InternalRemove);
            }
            catch (Exception e)
            {
                _logger.ZLogError(e,$"Error to remove '{key}' part:{e.Message}");
                var ex = new ConfigurationException($"Error to remove '{key}' part",e);
                _onError.OnNext(ex);
                throw ex;
            }
        }

        public Observable<ConfigurationException> OnError => _onError;

        private void InternalRemove(string path)
        {
            if (!_fileSystem.File.Exists(path)) return;
            _logger.ZLogTrace($"Delete configuration file '{path}'");
            _fileSystem.File.Delete(path);
        }

        public void Dispose()
        {
            _logger.ZLogTrace($"Dispose {this}");
            _onError.Dispose();
        }

        public override string ToString()
        {
            return $"JsonConfiguration: {_folderPath}";
        }
    }
}
