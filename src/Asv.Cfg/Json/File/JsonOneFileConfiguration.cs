using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZLogger;

namespace Asv.Cfg
{
    
    public class JsonOneFileConfiguration : JsonConfigurationBase
    {
        private readonly string _fileName;
        private readonly IFileSystem _fileSystem;
        private readonly string _backupFileName;
        
        public JsonOneFileConfiguration(
            string fileName, 
            bool createIfNotExist, 
            TimeSpan? flushToFileDelayMs, 
            bool sortKeysInFile = false, 
            ILogger? logger = null,
            IFileSystem? fileSystem = null,
            TimeProvider? timeProvider = null
        ) : base(() => LoadConfiguration(fileName, createIfNotExist, fileSystem, logger), flushToFileDelayMs, sortKeysInFile, timeProvider)
        {
            var fs = fileSystem ?? new FileSystem();
            var file = fs.Path.GetFullPath(fileName);
            _backupFileName = file + ".backup";
            
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            _fileName = fileName;
            _fileSystem = fileSystem ?? new FileSystem();
        }
        
        private static Stream LoadConfiguration(string fileName, bool createIfNotExist, IFileSystem? fileSystem,
            ILogger? logger)
        {
            var fs = fileSystem ?? new FileSystem();
            var file = fs.Path.GetFullPath(fileName);
            var backup = file + ".backup";
            logger ??= NullLogger.Instance;

            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            var dir = fs.Path.GetDirectoryName(fs.Path.GetFullPath(fileName));
            ArgumentException.ThrowIfNullOrWhiteSpace(dir);
            
            if (fs.Directory.Exists(dir) == false)
            {
                if (!createIfNotExist)
                    throw new DirectoryNotFoundException($"Directory with config file not exist: {dir}");
                
                logger.ZLogWarning($"Directory with config file not exist. Try to create it: {dir}");
                fs.Directory.CreateDirectory(dir);
            }
            if (fs.File.Exists(fileName) == false && fs.File.Exists(backup))
            {
                logger.ZLogWarning($"Configuration file doesn't exist. Try to load from backup file: {backup} => {file}");
                fs.File.Replace(backup, file,null,true);
            }
            
            if (fs.File.Exists(fileName) == false)
            {
                if (createIfNotExist)
                {
                    logger.ZLogWarning($"Config file not exist. Try to create {fileName}");
                    fs.File.WriteAllText(fileName,"{}");
                }
                else
                {
                    throw new ConfigurationException($"Configuration file not exist {fileName}");    
                }
            }

            return fs.File.OpenRead(file);
        }
        
        public string FileName => _fileName;
        
        protected override void EndSaveChanges()
        {
            _fileSystem.File.Move(_backupFileName,_fileName,true);
            Logger.ZLogTrace($"Flush configuration to file '{_fileName}' [{Count} items] ");
        }

        protected override Stream BeginSaveChanges()
        {
            if (_fileSystem.File.Exists(_backupFileName))
            {
                _fileSystem.File.Delete(_backupFileName);
            }

            return _fileSystem.File.Create(_backupFileName);
        }

        public override string ToString()
        {
            return $"{nameof(JsonOneFileConfiguration)}[{_fileName}]";
        }

        protected override IEnumerable<string> InternalSafeGetReservedParts() => [];
    }
}
