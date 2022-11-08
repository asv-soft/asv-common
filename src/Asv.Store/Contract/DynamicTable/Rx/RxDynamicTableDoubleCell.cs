using System;
using LiteDB;

namespace Asv.Store.Rx
{
    public class RxDynamicTableDoubleCell : RxDynamicTableCell<double>
    {
        public RxDynamicTableDoubleCell(IDynamicTablesStore table, Guid tableId, int rawIndex, string columnName, double defaultValue, TimeSpan? saveDelay = null)
            : base(table, tableId, rawIndex, columnName, defaultValue, saveDelay)
        {
        }

        protected override double ConvertFromBson(BsonValue bson) => bson.AsDouble;
        

        protected override BsonValue ConvertToBson(double value) => value;
    }
}
