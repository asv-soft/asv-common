using System;
using System.Reactive.Linq;
using Asv.Common;
using LiteDB;

namespace Asv.Store.Rx
{
    public abstract class RxDynamicTableCell<T> : RxValue<T>
    {
        private readonly IDynamicTablesStore _table;
        private readonly Guid _tableId;
        private readonly int _rawIndex;
        private readonly string _columnName;
        private readonly bool _internalChange;
        private readonly IDisposable _subscribe;

        protected RxDynamicTableCell(IDynamicTablesStore table, Guid tableId, int rawIndex, string columnName, T defaultValue, TimeSpan? saveDelay = null)
        {
            _table = table;
            _tableId = tableId;
            _rawIndex = rawIndex;
            _columnName = columnName;
            _internalChange = true;
            _subscribe = saveDelay == null ? this.Subscribe(WriteValue) : this.Throttle(saveDelay.Value).Subscribe(WriteValue);
            _internalChange = false;
            OnNext(ReadValue(defaultValue));
        }

        protected abstract T ConvertFromBson(BsonValue bson);
        protected abstract BsonValue ConvertToBson(T value);

        private T ReadValue(T defaultValue)
        {
            if (_table.TryReadCell(_tableId, _columnName, _rawIndex, out var bsonValue))
            {
                return ConvertFromBson(bsonValue);
            }
            WriteValue(defaultValue);
            return defaultValue;
        }

        private void WriteValue(T value)
        {
            if (_internalChange) return;
            var bson = ConvertToBson(value);
            _table.UpsetCell(_tableId, _columnName, _rawIndex, ConvertToBson(value));
        }

        protected override void InternalDisposeOnce()
        {
            _subscribe.Dispose();
            base.InternalDisposeOnce();
        }
    }
}
