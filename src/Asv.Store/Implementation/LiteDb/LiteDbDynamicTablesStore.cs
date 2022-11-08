using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace Asv.Store
{

    public class LiteDbDynamicTableDescription: IDynamicTableInfo
    {
        [BsonId]
        public int Id { get; set; }
        public Guid TableId { get; set; }
    }

    public class ColumnStatistic : IColumnStatistic
    {
        public ColumnStatistic(int count)
        {
            Count = count;
        }

        public int Count { get; }
    }

    public class DynamicTableStatistic : IDynamicTableStatistic
    {
        public DynamicTableStatistic(int rowCount, int columnCount)
        {
            RowCount = rowCount;
            ColumnCount = columnCount;
        }

        public int RowCount { get; }
        public int ColumnCount { get; }
    }

    public class LiteDbDynamicTablesStore:IDynamicTablesStore
    {
        private const string IdColumnName = "_id";
        private readonly LiteDatabase _db;
        private readonly string _subCollPrefix;
        private readonly ILiteCollection<LiteDbDynamicTableDescription> _indexColl;
        private readonly ConcurrentDictionary<Guid,ILiteCollection<BsonDocument>> _indexCache = new ConcurrentDictionary<Guid, ILiteCollection<BsonDocument>>();

        public LiteDbDynamicTablesStore(string name, LiteDatabase db, string indexCollectionName, string subCollPrefix)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(indexCollectionName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(indexCollectionName));
            if (string.IsNullOrWhiteSpace(subCollPrefix))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(subCollPrefix));
            Name = name;
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _subCollPrefix = subCollPrefix;
            _indexColl = _db.GetCollection<LiteDbDynamicTableDescription>(indexCollectionName, BsonAutoId.Int32);
            _indexColl.EnsureIndex(_ => _.TableId, true);
        }

        public string Name { get; }
        public IEnumerable<IDynamicTableInfo> Tables => _indexColl.FindAll();

        public IColumnStatistic GetColumnStatistic(Guid tableId, string columnName)
        {
            var tableColl = GetCollection(tableId);
            return new ColumnStatistic(tableColl.Count());
        }

        public bool TryReadCell(Guid tableId, string columnName, int rowIndex, out BsonValue value)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(columnName));
            var doc = TryGetBsonRaw(tableId, rowIndex);
            if (doc == null)
            {
                value = BsonValue.Null;
                return false;
            }
            value = doc[columnName];
            return !value.IsNull;
        }

        private BsonDocument TryGetBsonRaw(Guid tableId, int rowIndex)
        {
            var tableColl = GetCollection(tableId);
            return tableColl.FindById(rowIndex);
        }

        private string GetSubCollectionName(int id) => $"{_subCollPrefix}{id:000}";

        public void UpsetCell(Guid tableId, string columnName, int rowIndex, BsonValue value)
        {
            var tableColl = GetCollection(tableId);
            var doc = tableColl.FindById(rowIndex);
            if (doc == null)
            {
                doc = new BsonDocument { { IdColumnName, rowIndex },{ columnName , value } };
                
                tableColl.Insert(doc);
            }
            else
            {
                doc[columnName] = value;
                tableColl.Update(doc);
            }
        }

        public IDynamicTableStatistic GetTableStatistic(Guid tableId)
        {
            var coll = _indexColl.FindOne(_ => _.TableId == tableId);
            if (coll == null) return null;
            var subCollection = _db.GetCollection(GetSubCollectionName(coll.Id), BsonAutoId.Int32);
            var first = subCollection.FindAll().FirstOrDefault();
            var count = (first == null) ? 0 : (first.Count - 1);
            return new DynamicTableStatistic(subCollection.Count(), count);
        }

        public int ObserveAll(Guid tableId, Action<IDynamicTableRawObserver> callback)
        {
            var coll = _indexColl.FindOne(_ => _.TableId == tableId);
            if (coll == null) return 0;
            var subCollection = _db.GetCollection(GetSubCollectionName(coll.Id), BsonAutoId.Int32);
            var count = 0;
            foreach (var doc in subCollection.FindAll())
            {
                count++;
                var editor = new DynamicTableRawObserver(doc, tableId);
                callback(editor);
                if (editor.IsEdited)
                {
                    subCollection.Update(doc);
                }
            }
            return count;
        }

        public void AddRaw(Guid tableId, Action<IDynamicTableRawObserver> callback)
        {
            var tableColl = GetCollection(tableId);
            var id = tableColl.Insert(new BsonDocument());
            var doc = tableColl.FindById(id);
            var editor = new DynamicTableRawObserver(doc, tableId);
            callback(editor);
            if (editor.IsEdited)
            {
                tableColl.Update(doc);
            }
        }

        public bool RemoveRaw(Guid tableId, int rawIndex)
        {
            var tableColl = GetCollection(tableId);
            return tableColl.Delete(rawIndex);
        }

        private ILiteCollection<BsonDocument> GetCollection(Guid sessionId)
        {
            if (_indexCache.TryGetValue(sessionId, out var value))
            {
                return value;
            }

            var coll = _indexColl.FindOne(_=>_.TableId == sessionId);
            ILiteCollection<BsonDocument> subCollection;
            if (coll == null)
            {
                var id = (int)_indexColl.Insert(new LiteDbDynamicTableDescription{ TableId = sessionId});
                subCollection = _db.GetCollection(GetSubCollectionName(id), BsonAutoId.Int32);
            }
            else
            {
                subCollection = _db.GetCollection(GetSubCollectionName(coll.Id), BsonAutoId.Int32);
            }
            return _indexCache.AddOrUpdate(sessionId, _ => subCollection, (guid, collection) => subCollection);
        }

    }

    public class DynamicTableRawObserver : IDynamicTableRawObserver
    {
        private readonly BsonDocument _doc;

        public DynamicTableRawObserver(BsonDocument doc, Guid tableId)
        {
            TableId = tableId;
            _doc = doc;
        }

        public Guid TableId { get; }
        public int RawIndex => _doc["_id"];

        public IEnumerable<string> Columns => _doc.Keys;

        internal bool IsEdited { get; private set; }

        public bool TryReadCell(string columnName, out BsonValue value)
        {
            value = _doc[columnName];
            return !value.IsNull;
        }
        public void WriteCell(string columnName, BsonValue value)
        {
            _doc[columnName] = value;
            IsEdited = true;
        }
    }
}
