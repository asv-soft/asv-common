using System;
using LiteDB;

namespace Asv.Store
{
    public class RxStoredDecimal : RxStoredValue<decimal>
    {
        public RxStoredDecimal(IKeyValueStore store, string id, decimal defaultValue, TimeSpan? saveDelay = null) : base(store, id, defaultValue, saveDelay)
        {
        }

        protected override decimal ConvertFromBson(BsonValue bson)
        {
            return bson;
        }

        protected override BsonValue ConvertToBson(decimal value)
        {
            return value;
        }
    }
}
