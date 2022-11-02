using System;
using LiteDB;

namespace Asv.Store
{
    public class RxStoredDateTime : RxStoredValue<DateTime>
    {
        public RxStoredDateTime(IKeyValueStore store, string id, DateTime defaultValue, TimeSpan? saveDelay = null) : base(store, id, defaultValue, saveDelay)
        {
        }

        protected override DateTime ConvertFromBson(BsonValue bson)
        {
            return bson;
        }

        protected override BsonValue ConvertToBson(DateTime value)
        {
            return value;
        }
    }
}
