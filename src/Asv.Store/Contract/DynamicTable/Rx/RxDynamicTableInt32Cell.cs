using System;
using LiteDB;

namespace Asv.Store.Rx
{
    public class RxDynamicTableInt32Cell : RxDynamicTableCell<int>
    {
        public RxDynamicTableInt32Cell(IDynamicTablesStore table, Guid tableId, int rawIndex, string columnName, int defaultValue, TimeSpan? saveDelay = null)
            : base(table, tableId, rawIndex, columnName, defaultValue, saveDelay)
        {
        }

        protected override int ConvertFromBson(BsonValue bson) => bson.AsInt32;
        protected override BsonValue ConvertToBson(int value) => value;
    }
}
