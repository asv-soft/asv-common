using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LiteDB;

namespace Asv.Store
{
    public class LiteDbSimpleSeries<TRecord, TKey>:ISimpleSeries<TRecord>
    {
        private readonly ILiteCollection<TRecord> _collection;

        public LiteDbSimpleSeries(string name, ILiteCollection<TRecord> collection, Expression<Func<TRecord, TKey>> property)
        {
            _collection = collection;
            BsonMapper.Global.Entity<TRecord>().Id(property);
        }

        public int GetRecordsCount()
        {
            return _collection.Count();
        }

        public IEnumerable<TRecord> ReadAll()
        {
            return _collection.FindAll();
        }

        public void Push(TRecord record)
        {
            _collection.Upsert(record);
        }

        public void ClearAll()
        {
            _collection.DeleteAll();
        }

        public void Dispose()
        {

        }
    }
}
