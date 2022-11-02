using System;
using System.Linq;
using LiteDB;

namespace Asv.Store
{
    public class RxStoredEnum<TValue> : RxStoredValue<TValue>
    {
        public RxStoredEnum(IKeyValueStore store, string id, TValue defaultValue, TimeSpan? saveDelay = null) : base(store, id, defaultValue, saveDelay)
        {
        }

        protected override TValue ConvertFromBson(BsonValue bson)
        {
            try
            {
                return (TValue)Enum.Parse(typeof(TValue), bson, true);
            }
            catch
            {
                return default(TValue);
            }
        }

        protected override BsonValue ConvertToBson(TValue value)
        {
            if (!typeof(TValue).IsEnum) return default(BsonValue);
            try
            {
                return Enum.GetName(typeof(TValue), value);
            }
            catch
            {
                return Enum.GetNames(typeof(TValue)).FirstOrDefault();
            }
        }
    }
}
