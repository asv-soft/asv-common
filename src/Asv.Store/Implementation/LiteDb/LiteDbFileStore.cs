using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Asv.Common;
using LiteDB;

namespace Asv.Store
{
    public class LiteDbFileStore : IStore
    {
        public const string DictionaryPrefix = "d_";
        public const string TextPrefix = "t_";
        public const string DoublePrefix = "r_";
        public const string IntPrefix = "n_";
        public const string SimpleSeriesPrefix = "s_";
        public const string DynamicTablePrefix = "t_";
        public const string DynamicTableSubPrefix = "tt_";

        private readonly LiteDatabase _db;
        private readonly string _sourceName;

        public LiteDbFileStore(LiteDatabase db,string sourceName)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _sourceName = sourceName;
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public string SourceName => _sourceName;
        public IEnumerable<string> Dicts => _db.GetCollectionNames().Select(_ => ConvertBackCollectionName(_,DictionaryPrefix)).IgnoreNulls();

        public IKeyValueStore GetDict(string name)
        {
            return new LiteDbKeyValueStore(name,_db.GetCollection<BsonDocument>(ConvertCollectionName(name,DictionaryPrefix)));
        }

        public IEnumerable<string> Texts => _db.GetCollectionNames().Select(_ => ConvertBackCollectionName(_, TextPrefix)).IgnoreNulls();

        private string ConvertBackCollectionName(string name, string prefix)
        {
            return name.StartsWith(prefix) ? name.Remove(0, prefix.Length) : null;
        }

        private string ConvertCollectionName(string name, string prefix)
        {
            return string.Concat(prefix, name);
        }

        public ITextStore GetText(string name)
        {
            return new LiteDbTextStore(_db.GetCollection<TextMessage>(ConvertCollectionName(name, TextPrefix)));
        }

        public IEnumerable<string> DoubleSeries => _db.GetCollectionNames().Select(_ => ConvertBackCollectionName(_, DoublePrefix)).IgnoreNulls();

        public ISeriesValueStore<double, TYValue> GetDoubleSeries<TYValue>(string name)
        {
            return new LiteDbDoubleSeriesStore<TYValue>(name, _db.GetCollection<SeriesPoint<double, TYValue>>(ConvertCollectionName(name, DoublePrefix)));
        }

        public IEnumerable<string> IntSeries => _db.GetCollectionNames().Select(_ => ConvertBackCollectionName(_, IntPrefix)).IgnoreNulls();
        
        public ISeriesValueStore<int, TYValue> GetIntSeries<TYValue>(string name)
        {
            return new LiteDbIntSeriesStore<TYValue>(name, _db.GetCollection<SeriesPoint<int, TYValue>>(ConvertCollectionName(name, IntPrefix)));
        }

        public IEnumerable<string> FileGrids => _db.FileStorage.FindAll().Select(_=>new LiteDbFileInfo(_)).Select(_=>_.GridName).Distinct();

        public IFileGrid GetFileGrid(string name)
        {
            return new LiteDbFileGrid(name, _db.FileStorage);
        }

        public IEnumerable<string> RecordSeries => _db.GetCollectionNames().Select(_ => ConvertBackCollectionName(_, SimpleSeriesPrefix)).IgnoreNulls();

        public ISimpleSeries<TRecord> GetRecordSeries<TRecord, TKey>(string name, Expression<Func<TRecord, TKey>> keyMapper)
        {
            return new LiteDbSimpleSeries<TRecord,TKey>(name, _db.GetCollection<TRecord>(ConvertCollectionName(name, SimpleSeriesPrefix)),keyMapper);
        }

        public IEnumerable<string> DynamicTables => _db.GetCollectionNames().Select(_ => ConvertBackCollectionName(_, DynamicTablePrefix)).IgnoreNulls();

        public IDynamicTablesStore GetDynamicTables(string name)
        {
            return new LiteDbDynamicTablesStore(name, _db, ConvertCollectionName(name, SimpleSeriesPrefix), ConvertCollectionName(name, DynamicTableSubPrefix));
        }
    }

    
}
