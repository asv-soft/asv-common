using System;
using System.Linq;
using System.Threading;

namespace Asv.Common
{
    public class IncrementalRateCounter
    {
        private readonly CircularBuffer2<double> _valueBuffer;
        private long _lastValue = 0;
        private readonly TimeProvider _timeProvider;
        private long _lastUpdated;
        private readonly double[] _buffer;

        public IncrementalRateCounter(int movingAverageSize = 5, TimeProvider? timeProvider = null)
        {
            _timeProvider = timeProvider ?? TimeProvider.System;
            _buffer = new double[movingAverageSize];
            _valueBuffer = new CircularBuffer2<double>(_buffer, _buffer.Length);
            _lastUpdated = _timeProvider.GetTimestamp();
            for (var i = 0; i < _buffer.Length; i++)
            {
                _valueBuffer.PushBack(0);
            }
        }

        public double Calculate(long sum)
        {
            var lastTime = Interlocked.Exchange(ref _lastUpdated, _timeProvider.GetTimestamp());
            var elapsedTime = _timeProvider.GetElapsedTime(lastTime);
            var deltaSeconds = elapsedTime.TotalSeconds;
            if (deltaSeconds <= 0)
            {
                deltaSeconds = 1;
            }

            var rateHz = (sum - _lastValue) / deltaSeconds;
            if (rateHz >= 0)
            {
                _valueBuffer.PushBack(rateHz);
            }
            _lastValue = sum;
            return _buffer.Average();
        }
    }
}
