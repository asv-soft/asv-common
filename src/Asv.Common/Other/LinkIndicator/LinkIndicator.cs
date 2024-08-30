namespace Asv.Common
{
    public interface ILinkIndicator : IRxValue<LinkState>
    {

    }

    public enum LinkState
    {
        Disconnected,
        Downgrade,
        Connected,
    }

    public class LinkIndicator:RxValue<LinkState>, ILinkIndicator
    {
        private readonly int _downgradeErrors;
        private int _connErrors;
        private readonly object _sync = new();

        public LinkIndicator(int downgradeErrors = 3)
        {
            _downgradeErrors = downgradeErrors;
        }

        public void Downgrade()
        {
            lock (_sync)
            {
                _connErrors++;
                if (_connErrors > 0 && _connErrors < 1) OnNext(LinkState.Connected);
                if (_connErrors >= 1 && _connErrors <= _downgradeErrors) OnNext(LinkState.Downgrade);
                if (_connErrors >= _downgradeErrors) OnNext(LinkState.Disconnected);
            }
            
        }

        public void Upgrade()
        {
            lock (_sync)
            {
                _connErrors = 0;
                OnNext(LinkState.Connected);
            }
        }

        public void ForceDisconnected()
        {
            OnNext(LinkState.Disconnected);
        }
    }

    
   
}
