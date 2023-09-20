using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Asv.Common;
using Newtonsoft.Json;

namespace Asv.Cfg.Json
{
    public class ZipJsonConfiguration:DisposableOnce, IConfiguration
    {
        private readonly ZipArchive _archive;
        private readonly object _sync = new();
        private const string FileExt = ".json";

        public ZipJsonConfiguration(Stream zipStream,bool leaveOpen = false)
        {
            if (zipStream == null) throw new ArgumentNullException(nameof(zipStream));
            _archive = new ZipArchive(zipStream, ZipArchiveMode.Update, leaveOpen);
        }
        
        protected override void InternalDisposeOnce()
        {
            lock (_sync)
            {
                _archive.Dispose();
            }
        }

        public IEnumerable<string> AvailableParts
        {
            get
            {
                lock (_sync)
                {
                    return _archive.Entries.Where(_ => Path.GetExtension(_.Name) == FileExt)
                        .Select(x => x.Name).ToArray();
                }
            }
        }
        public bool Exist<TPocoType>(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            var fileName = key+FileExt;
            lock (_sync)
            {
                return _archive.Entries.Any(x => ConfigurationHelper.DefaultKeyComparer.Equals(x.Name, fileName));
            }
        }

        public bool Exist(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            var fileName = key+FileExt;
            lock (_sync)
            {
                return _archive.Entries.Any(x => ConfigurationHelper.DefaultKeyComparer.Equals(x.Name, fileName));
            }
        }

        public TPocoType Get<TPocoType>(string key, TPocoType defaultValue)
        {
            ConfigurationHelper.ValidateKey(key);
            var fileName = key+FileExt;
            lock (_sync)
            {
                var entry = _archive.Entries.FirstOrDefault(x => ConfigurationHelper.DefaultKeyComparer.Equals(x.Name, fileName));
                if (entry == default)
                {
                    return defaultValue;
                }
                var serializer = new JsonSerializer();
                using var rdr = new JsonTextReader(new StreamReader(entry.Open()));
                return serializer.Deserialize<TPocoType>(rdr);
            }
        }

        public void Set<TPocoType>(string key, TPocoType value)
        {
            ConfigurationHelper.ValidateKey(key);
            var fileName = key+FileExt;
            lock (_sync)
            {
                _archive.GetEntry(fileName)?.Delete();
                var entry = _archive.CreateEntry(fileName);
                using var wrt = new JsonTextWriter(new StreamWriter(entry.Open()));
                var serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented
                };
                serializer.Serialize(wrt, value);
            }
        }

        public void Remove(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            var fileName = key+FileExt;
            lock (_sync)
            {
                var entry = _archive.GetEntry(fileName);
                entry?.Delete();
            }
        }
    }
}