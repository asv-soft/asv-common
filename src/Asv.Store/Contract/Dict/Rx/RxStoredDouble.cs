using System;
using LiteDB;

namespace Asv.Store
{
    public class RxStoredDouble : RxStoredValue<double>
    {
        public RxStoredDouble(IKeyValueStore store, string id, double defaultValue, TimeSpan? saveDelay = null) : base(store, id, defaultValue, saveDelay)
        {
        }

        protected override double ConvertFromBson(BsonValue bson)
        {
            if (bson.IsNull) return double.NaN;
            return bson;
        }

        protected override BsonValue ConvertToBson(double value)
        {
            return value;
        }
    }
}
