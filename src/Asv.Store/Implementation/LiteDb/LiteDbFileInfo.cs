using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LiteDB;

namespace Asv.Store
{
    public class LiteDbFileInfo:IFileInfo
    {
        private readonly LiteFileInfo<string> _info;
        private readonly string _gridName;
        private const char IdSeparator = '/';

        public LiteDbFileInfo(LiteFileInfo<string> info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            _info = info;
            string fileName;
            SplitId(_info.Id,out _gridName,out fileName);

        }

        public long Length => _info.Length;
        public DateTime UploadDate => _info.UploadDate;
        public string Id => _info.Id;
        public string Filename => _info.Filename;
        public string MimeType => _info.MimeType;
        public string this[string key] => _info.Metadata[key]?.AsString;
        public string GridName => _gridName;

        public static string JoinId(string gridName, string fileName)
        {
            return string.Concat(gridName, IdSeparator, fileName);
        }

        public static void SplitId(string src, out string grid, out string id)
        {
            var split = src.Split(IdSeparator);
            grid = split[0].ToLower();
            id = split[1].ToLower();
        }
    }

    public class LiteDbFileGrid:IFileGrid
    {
        private static readonly Regex FileNameRegex = new Regex(@"^[\w,\s-]+\.[A-Za-z]{3}$", RegexOptions.Compiled);
        private readonly ILiteStorage<string> _fileGrid;

        public LiteDbFileGrid(string name, ILiteStorage<string> fileGrid)
        {
            if (fileGrid == null) throw new ArgumentNullException(nameof(fileGrid));
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
            _fileGrid = fileGrid;
            GridName = name.ToLower();
        }

        public IEnumerable<IFileInfo> Files => _fileGrid.FindAll().Select(_=>new LiteDbFileInfo(_)).Where(_=>_.GridName == GridName);

        public bool Exist(string fileName)
        {
            return _fileGrid.Exists(LiteDbFileInfo.JoinId(GridName, fileName));
        }

        public IFileInfo Upload(string fileName, Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            fileName = CheckAndNormalizeFileName(fileName);
            var info = _fileGrid.Upload(LiteDbFileInfo.JoinId(GridName, fileName), fileName, stream);
            return new LiteDbFileInfo(info);
        }

        public IFileInfo Download(string fileName, Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            fileName = CheckAndNormalizeFileName(fileName);
            var info = _fileGrid.Download(LiteDbFileInfo.JoinId(GridName, fileName), stream);
            return new LiteDbFileInfo(info);
        }

        public IFileInfo GetInfo(string fileName)
        {
            fileName = CheckAndNormalizeFileName(fileName);
            var info = _fileGrid.FindById(LiteDbFileInfo.JoinId(GridName, fileName));
            return info == null ? null : new LiteDbFileInfo(info);
        }

        public void UpdateMetadata(string fileName, IReadOnlyDictionary<string, string> metadata)
        {
            fileName = CheckAndNormalizeFileName(fileName);
            var doc = new BsonDocument();
            foreach (var pair in metadata)
            {
                doc[pair.Key] = pair.Value;
            }
            _fileGrid.SetMetadata(LiteDbFileInfo.JoinId(GridName, fileName), doc);
        }

        public bool Delete(string fileName)
        {
            fileName = CheckAndNormalizeFileName(fileName);
            return _fileGrid.Delete(LiteDbFileInfo.JoinId(GridName, fileName));
        }

        private string CheckAndNormalizeFileName(string fileName)
        {
            if (FileNameRegex.IsMatch(fileName) == false)
                throw new Exception($"The file name '{fileName}' is in the wrong format");
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
            return fileName.ToLower();
        }

        public string GridName { get; }



    }
}
