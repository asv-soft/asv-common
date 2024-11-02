using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace Asv.Common
{
    public class RxValueBehaviour<T>(T initValue) : DisposableOnce, IRxEditableValue<T>, IDisposable
    {
        private readonly BehaviorSubject<T> _subject = new(initValue);

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _subject.Subscribe(observer);
        }

        public T Value => _subject.Value;

        public void OnCompleted()
        {
            _subject.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _subject.OnError(error);
        }

        public void OnNext(T value)
        {
            _subject.OnNext(value);
        }

        protected override void InternalDisposeOnce()
        {
            _subject.OnCompleted();
            _subject.Dispose();
        }
    }

    public class RxValue<TValue> : DisposableOnce, IRxEditableValue<TValue>
    {
        public RxValue() { }

        public RxValue(TValue initValue)
            : this()
        {
            _value = initValue;
        }

        private readonly Subject<TValue> _subject = new();
        private TValue _value = default!;

        public TValue Value
        {
            get => _value;
            set
            {
                _value = value;
                OnNext(value);
            }
        }

        protected override void InternalDisposeOnce()
        {
            _subject.OnCompleted();
            _subject.Dispose();
        }

        public void OnNext(TValue value)
        {
            _value = value;
            if (_subject is { HasObservers: true, IsDisposed: false })
            {
                _subject.OnNext(value);
            }
        }

        public void OnError(Exception error)
        {
            if (_subject is { HasObservers: true, IsDisposed: false })
            {
                _subject.OnError(error);
            }
        }

        public void OnCompleted()
        {
            if (_subject is { HasObservers: true, IsDisposed: false })
            {
                _subject.OnCompleted();
            }
        }

        public IDisposable Subscribe(IObserver<TValue> observer)
        {
            if (_subject.IsDisposed)
            {
                return Disposable.Empty;
            }

            var result = _subject.Subscribe(observer);
            if (_value != null)
            {
                observer.OnNext(_value);
            }

            return result;
        }

        public override string ToString()
        {
            return Value?.ToString() ?? string.Empty;
        }
    }
}
