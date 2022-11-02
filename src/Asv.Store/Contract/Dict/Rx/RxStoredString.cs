using System;
using LiteDB;

namespace Asv.Store
{
    public class RxStoredString : RxStoredValue<string>
    {
        public RxStoredString(IKeyValueStore store, string id, string defaultValue, TimeSpan? saveDelay = null) : base(store, id, defaultValue, saveDelay)
        {
        }

        protected override string ConvertFromBson(BsonValue bson)
        {
            return bson;
        }

        protected override BsonValue ConvertToBson(string value)
        {
            return value;
        }
    }
    
}
