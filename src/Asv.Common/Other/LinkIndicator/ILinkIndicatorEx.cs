using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Asv.Common
{
    public interface ILinkIndicatorEx : ILinkIndicator
    {
        /// <summary>
        /// Represents an event that is triggered when the link is connected.
        /// This event happens when the last state was Disconnected and the new state is Connected (downgrade is not considered).
        /// If current state is Connected event will be risen immediately after subscription.
        /// </summary>
        IObservable<Unit> OnFound { get; }

        /// <summary>
        /// Represents an event that is triggered when the link is lost.
        /// This event happens when the last state was Connected or Downgrade, and the new state is Disconnected.
        /// If current state is Disconnected event will be risen immediately after subscription.
        /// </summary>
        /// <remarks>
        /// This event is triggered when the number of connection errors exceeds the specified downgrade errors.
        /// </remarks>
        IObservable<Unit> OnLost { get; }
    }

    public class LinkIndicatorBase:RxValue<LinkState>, ILinkIndicatorEx
    {
        private readonly int _downgradeErrors;
        private int _connErrors;
        private readonly object _sync = new();
        private LinkState _lastState = LinkState.Disconnected;
        private readonly Subject<Unit> _onLost;
        private readonly Subject<Unit> _onFound;

        public LinkIndicatorBase(int downgradeErrors = 3)
            :base(LinkState.Disconnected)
        {
            _downgradeErrors = downgradeErrors;
            _onLost = new Subject<Unit>();
            _onFound = new Subject<Unit>();
            
            OnLost = new AnonymousObservable<Unit>(x =>
            {
                var result = _onLost.Subscribe(x);
                if (_lastState == LinkState.Disconnected) 
                    x.OnNext(Unit.Default);
                return result;
            });
            OnFound = new AnonymousObservable<Unit>(x =>
            {
                var result = _onFound.Subscribe(x);
                if (_lastState == LinkState.Connected) 
                    x.OnNext(Unit.Default);
                return result;
            });
        }
        protected void InternalUpgrade()
        {
            lock (_sync)
            {
                _connErrors = 0;
                PushNewValue(LinkState.Connected);
            }
        }
        
        protected void InternalDowngrade()
        {
            lock (_sync)
            {
                _connErrors++;
                if (_connErrors >= 1 && _connErrors <= _downgradeErrors) PushNewValue(LinkState.Downgrade);
                if (_connErrors >= _downgradeErrors) PushNewValue(LinkState.Disconnected);
            }
        }

        private void PushNewValue(LinkState state)
        {
            // distinct until changed
            if (state == _lastState) { return; }
            // publish new value
            OnNext(state);
            if (_lastState == LinkState.Disconnected && state == LinkState.Connected)
            {
                _onFound.OnNext(Unit.Default);
            }
            else if (state == LinkState.Disconnected)
            {
                _onLost.OnNext(Unit.Default);
            }
            _lastState = state;            
        }

        public void ForceDisconnected()
        {
            PushNewValue(LinkState.Disconnected);
        }

        protected override void InternalDisposeOnce()
        {
            base.InternalDisposeOnce();
            _onLost.Dispose();
            _onFound.Dispose();
        }

        public IObservable<Unit> OnFound { get; }
        public IObservable<Unit> OnLost { get; }
    }

    
    public class ManualLinkIndicator(int downgradeErrors = 3) : LinkIndicatorBase(downgradeErrors)
    {
        public void Upgrade()
        {
            InternalUpgrade();
        }

        public void Downgrade()
        {
            InternalDowngrade();
        }
    }
    
    public class TimeBasedLinkIndicator : LinkIndicatorBase
    {
        private readonly TimeSpan _timeout;
        private readonly IDisposable _timer;
        private DateTime _lastTime;

        public TimeBasedLinkIndicator(TimeSpan timeout,int downgradeErrors = 3) : base(downgradeErrors)
        {
            _timeout = timeout;
            _timer = Observable.Timer(_timeout, _timeout)
                .Where(_ => DateTime.Now - _lastTime > _timeout)
                .Subscribe(x => InternalDowngrade());
        }

        public void Upgrade()
        {
            InternalUpgrade();
            _lastTime = DateTime.Now;
        }

        protected override void InternalDisposeOnce()
        {
            base.InternalDisposeOnce();
            _timer.Dispose();
        }
    }
    
    public class TimeBasedObservableLinkIndicator<T> : LinkIndicatorBase,IObserver<T>
    {
        private readonly IDisposable _timer;
        private DateTime _lastTime;

        public TimeBasedObservableLinkIndicator(TimeSpan timeout,int downgradeErrors = 3) : base(downgradeErrors)
        {
            _timer = Observable.Timer(timeout, timeout)
                .Where(_ => DateTime.Now - _lastTime > timeout)
                .Subscribe(x => InternalDowngrade());
        }

        protected override void InternalDisposeOnce()
        {
            base.InternalDisposeOnce();
            _timer.Dispose();
        }

        public void OnNext(T value)
        {
            InternalUpgrade();
            _lastTime = DateTime.Now;
        }
    }
    
   
}
