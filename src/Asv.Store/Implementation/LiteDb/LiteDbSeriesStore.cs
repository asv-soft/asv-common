using System.Collections.Generic;
using LiteDB;

namespace Asv.Store
{
    public abstract class LiteDbSeriesStore<TXValue, TYValue> : ISeriesValueStore<TXValue, TYValue> where TXValue : struct
    {
        protected const string XName = "_id";

        private readonly ILiteCollection<SeriesPoint<TXValue, TYValue>> _coll;

        public LiteDbSeriesStore(string name, ILiteCollection<SeriesPoint<TXValue, TYValue>> collection)
        {
            Name = name;
            _coll = collection;
            _coll.EnsureIndex(_=>_.X, true);
        }

        public void Push(SeriesPoint<TXValue, TYValue> point)
        {
            if (!_coll.Update(point))
            {
                _coll.Insert(point);
            }
            
        }

        public IEnumerable<SeriesPoint<TXValue, TYValue>> Read(SeriesQuery<TXValue> query)
        {
            var q = Query.All(XName, Query.Ascending);
            if (query.From.HasValue)
            {
                q.Where.Add(Query.GTE(XName, new BsonValue(query.From.Value)));
            }
            if (query.To.HasValue)
            {
                q.Where.Add(Query.LTE(XName, new BsonValue(query.To.Value)));
            }

            return _coll.Find(q, query.Skip, query.Take);
        }

        public abstract TXValue GetXMinValue();

        public abstract TXValue GetXMaxValue();
        

        public void ClearAll()
        {
            _coll.DeleteAll();
        }

        public string Name { get; }
    }

    public class LiteDbIntSeriesStore<TYValue> : LiteDbSeriesStore<int, TYValue>
    {
        private readonly ILiteCollection<SeriesPoint<int, TYValue>> _collection;

        public LiteDbIntSeriesStore(string name, ILiteCollection<SeriesPoint<int, TYValue>> collection) : base(name, collection)
        {
            _collection = collection;
        }

        public override int GetXMinValue()
        {
            return _collection.Min(XName);
        }

        public override int GetXMaxValue()
        {
            return _collection.Max(XName);
        }
    }

    public class LiteDbDoubleSeriesStore<TYValue> : LiteDbSeriesStore<double, TYValue>
    {
        private readonly ILiteCollection<SeriesPoint<double, TYValue>> _collection;

        public LiteDbDoubleSeriesStore(string name, ILiteCollection<SeriesPoint<double, TYValue>> collection) : base(name,collection)
        {
            _collection = collection;
        }

        public override double GetXMinValue()
        {
            
            return _collection.Min(XName);
        }

        public override double GetXMaxValue()
        {
            return _collection.Max(XName);
        }
    }
}
