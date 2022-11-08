using System;
using System.Linq;
using LiteDB;

namespace Asv.Store.Rx
{
    public class RxDynamicTableEnumCell<TValue> : RxDynamicTableCell<TValue>
    {
        public RxDynamicTableEnumCell(IDynamicTablesStore table, Guid tableId, int rawIndex, string columnName, TValue defaultValue, TimeSpan? saveDelay = null) : base(table, tableId, rawIndex, columnName, defaultValue, saveDelay)
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
