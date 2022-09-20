using System;
using System.IO.Ports;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO
{
    public class CustomSerialPort : PortBase
    {
        private readonly SerialPortConfig _config;
        private SerialPort _serial;
        private readonly AsyncLock _sync = new();
        private int _isReading;
        private IDisposable _readingTimer;

        public CustomSerialPort(SerialPortConfig config)
        {
            _config = config;
        }

        public override PortType PortType => PortType.Serial;


        public override string PortLogName => _config.ToString();

        protected override async Task InternalSend(byte[] data, int count, CancellationToken cancel)
        {
            if (_serial == null) return;
            using (await _sync.LockAsync(cancel).ConfigureAwait(false))
            {
                if (_serial is not { IsOpen: true }) return;
                await _serial.BaseStream.WriteAsync(data, 0, count, cancel).ConfigureAwait(false);
            }
        }

        protected override void InternalStop()
        {
            if (_serial == null) return;
            using (_sync.Lock())
            {
                if (_serial == null) return;
                try
                {
                    _readingTimer?.Dispose();
                    if (_serial.IsOpen == true)
                        _serial.Close();

                }
                catch (Exception e)
                {
                    // ignore close errors
                }
                _serial.Dispose();
                _serial = null;
            }
        }

        protected override void InternalStart()
        {
            using (_sync.Lock())
            {
                _serial = new SerialPort(_config.PortName, _config.BoundRate, _config.Parity, _config.DataBits, _config.StopBits)
                {
                    WriteBufferSize = _config.WriteBufferSize,
                    WriteTimeout = _config.WriteTimeout,
                };
                _serial.Open();
                _readingTimer = Observable.Timer(TimeSpan.FromMilliseconds(30),TimeSpan.FromMilliseconds(30)).Subscribe(TryReadData);
            }
        }

        private void TryReadData(long l)
        {
            if (Interlocked.CompareExchange(ref _isReading,1,0) != 0) return;
            try
            {
                byte[] data;
                using (_sync.Lock())
                {
                    if (_serial == null || _serial.BytesToRead == 0 || _serial.IsOpen == false) goto exit;
                    data = new byte[_serial.BytesToRead];
                    _serial.Read(data, 0, data.Length);
                }
                InternalOnData(data);
            }
            catch (Exception e)
            {
                InternalOnError(e);
            }
            
            exit:
            Interlocked.Exchange(ref _isReading, 0);
        }


        public override string ToString()
        {
            return $"Serial '{_config.PortName}'\n" +
                   $"Options: {_config.BoundRate} baud {_config.DataBits}-{_config.Parity:G}-{_config.StopBits:G}";
        }
    }
}
