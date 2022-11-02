using System.Collections.Generic;
using LiteDB;

namespace Asv.Store
{


    public class SeriesPoint<TXValue, TYValue>
    {
        [BsonId]
        public TXValue X { get; set; }
        public TYValue Y { get; set; }
    }

    public class SeriesQuery<TXValue>
        where TXValue : struct 
    {
        public TXValue? From { get; set; }
        public TXValue? To { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; } = 1000;
    }

    public interface ISeriesValueStore<TXValue, TYValue> where TXValue : struct
    {
        void Push(SeriesPoint<TXValue, TYValue> point);
        IEnumerable<SeriesPoint<TXValue, TYValue>> Read(SeriesQuery<TXValue> query);
        TXValue GetXMinValue();
        TXValue GetXMaxValue();
        void ClearAll();
        string Name { get; }
    }

    public static class SeriesValueStoreHelper
    {
        public static void Push<TXValue, TYValue>(this ISeriesValueStore<TXValue, TYValue> src, TXValue x, TYValue y) where TXValue : struct
        {
            src.Push(new SeriesPoint<TXValue, TYValue> {X = x, Y = y});
        }
    }



}
