using System;

namespace Asv.Common
{
    public interface IRxValue<out TValue> : IObservable<TValue>
    {
        TValue Value { get; }
    }

    public interface IRxEditableValue<TValue> : IRxValue<TValue>,IObserver<TValue>
    {
        
    }

}
