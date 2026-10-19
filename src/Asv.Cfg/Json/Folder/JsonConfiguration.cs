using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using ZLogger;

namespace Asv.Cfg
{
    /// <summary>
    /// Stores each configuration value in a separate JSON file inside a folder.
    /// </summary>
    public class JsonConfiguration : ConfigurationBase
    {
        private readonly string _folderPath;
        private const string FixedFileExtension = ".json";
        private readonly LockByKeyExecutor<string> _lock = new(
            ConfigurationMixin.DefaultKeyComparer
        );
        private readonly ILogger _logger;
        private readonly JsonSerializer _serializer;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonConfiguration"/> class.
        /// </summary>
        /// <param name="folderPath">The folder that contains configuration files.</param>
        /// <param name="logger">The optional logger.</param>
        /// <param name="fileSystem">The optional file system abstraction.</param>
        public JsonConfiguration(
            string folderPath,
            ILogger? logger = null,
            IFileSystem? fileSystem = null
        )
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

        /// <summary>
        /// Gets the configuration working folder.
        /// </summary>
        public string WorkingFolder => _folderPath;

        private IEnumerable<string> GetConfigurationFiles() =>
            _fileSystem.Directory.EnumerateFiles(_folderPath)
                .Where(path => ConfigurationMixin.DefaultKeyComparer.Equals(
                    _fileSystem.Path.GetExtension(path), FixedFileExtension));

        private string GetFilePath(string key)
        {
            string? match = null;
            foreach (var path in GetConfigurationFiles())
            {
                if (!ConfigurationMixin.DefaultKeyComparer.Equals(
                    _fileSystem.Path.GetFileNameWithoutExtension(path), key))
                {
                    continue;
                }
                if (match is not null)
                {
                    throw new ConfigurationException($"Conflicting configuration keys: '{match}' and '{path}'.");
                }
                match = path;
            }
            return match ?? _fileSystem.Path.Combine(_folderPath, $"{key}{FixedFileExtension}");
        }

        /// <inheritdoc />
        protected override IEnumerable<string> InternalSafeGetReservedParts() => [];

        /// <inheritdoc />
        protected override IEnumerable<string> InternalSafeGetAvailableParts()
        {
            var keys = new HashSet<string>(ConfigurationMixin.DefaultKeyComparer);
            foreach (var path in GetConfigurationFiles())
            {
                var key = _fileSystem.Path.GetFileNameWithoutExtension(path) ?? string.Empty;
                if (!keys.Add(key))
                {
                    throw new ConfigurationException($"Conflicting configuration key: '{key}'.");
                }
            }
            return keys.ToArray();
        }

        /// <inheritdoc />
        protected override bool InternalSafeExist(string key)
        {
            return _lock.Execute(key, () => _fileSystem.File.Exists(GetFilePath(key)));
        }

        /// <inheritdoc />
        protected override TPocoType InternalSafeGet<TPocoType>(
            string key,
            Lazy<TPocoType> defaultValue
        )
        {
            return _lock.Execute(key, () => InternalGet(GetFilePath(key), defaultValue));
        }

        private TPocoType InternalGet<TPocoType>(string path, Lazy<TPocoType> defaultValue)
        {
            if (_fileSystem.File.Exists(path))
            {
                using var stream = _fileSystem.File.OpenRead(path);
                using var reader = new StreamReader(stream);
                using var jsonReader = new JsonTextReader(reader);
                return _serializer.Deserialize<TPocoType>(jsonReader)
                    ?? throw new InvalidOperationException();
            }
            var value = defaultValue.Value;
            InternalSet(path, value);
            return value;
        }

        /// <inheritdoc />
        protected override void InternalSafeSave<TPocoType>(string key, TPocoType value)
        {
            _lock.Execute(key, () => InternalSet(GetFilePath(key), value));
        }

        private void InternalSet<TPocoType>(string filepath, TPocoType value)
        {
            InternalRemove(filepath);

            using var file = _fileSystem.File.CreateText(filepath);
            _serializer.Serialize(file, value);
            file.Flush();
        }

        /// <inheritdoc />
        protected override void InternalSafeRemove(string key)
        {
            _lock.Execute(key, () => InternalRemove(GetFilePath(key)));
        }

        private void InternalRemove(string path)
        {
            if (!_fileSystem.File.Exists(path))
            {
                return;
            }

            _fileSystem.File.Delete(path);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"JsonConfiguration[{_folderPath}]";
        }
    }
}
