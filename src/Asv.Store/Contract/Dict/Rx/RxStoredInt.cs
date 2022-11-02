using System;
using LiteDB;

namespace Asv.Store
{
    public class RxStoredInt : RxStoredValue<int>
    {
        public RxStoredInt(IKeyValueStore store, string id, int defaultValue, TimeSpan? saveDelay = null) : base(store, id, defaultValue, saveDelay)
        {
        }

        protected override int ConvertFromBson(BsonValue bson)
        {
            if (bson.IsNull) return Int32.MaxValue;
            return bson;
        }

        protected override BsonValue ConvertToBson(int value)
        {
            return value;
        }
    }
}
