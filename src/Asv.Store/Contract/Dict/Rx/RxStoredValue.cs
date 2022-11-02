using System;
using System.Reactive.Linq;
using Asv.Common;
using LiteDB;

namespace Asv.Store
{
    public abstract class RxStoredValue<T> : RxValue<T>
    {
        private readonly IKeyValueStore _store;
        private readonly string _id;
        private readonly IDisposable _subscribe;
        private bool _internalChange;

        protected RxStoredValue(IKeyValueStore store, string id, T defaultValue, TimeSpan? saveDelay = null)
        {
            _store = store;
            _id = id;

            _internalChange = true;
            _subscribe = saveDelay == null ? this.Subscribe(WriteValue) : this.Throttle(saveDelay.Value).Subscribe(WriteValue);
            OnNext(ReadValue(defaultValue));
            _internalChange = false;
        }

        private T ReadValue(T defaultValue)
        {
            var value = _store.Read(_id);
            if (value == null || value.IsNull)
            {
                WriteValue(defaultValue);
                return defaultValue;
            }
            return ConvertFromBson(value);
        }

        protected abstract T ConvertFromBson(BsonValue bson);
        protected abstract BsonValue ConvertToBson(T value);

        private void WriteValue(T value)
        {
            if (_internalChange) return;
            var bson = ConvertToBson(value);
            _store.Write(_id,bson);
        }

        protected override void InternalDisposeOnce()
        {
            _subscribe.Dispose();
            base.InternalDisposeOnce();
        }

    }
}
