using System;
using LiteDB;

namespace Asv.Store
{
    public class RxStoredBool : RxStoredValue<bool>
    {
        public RxStoredBool(IKeyValueStore store, string id, bool defaultValue, TimeSpan? saveDelay = null) : base(store, id, defaultValue, saveDelay)
        {
        }

        protected override bool ConvertFromBson(BsonValue bson)
        {
            return bson;
        }

        protected override BsonValue ConvertToBson(bool value)
        {
            return value;
        }
    }
}
