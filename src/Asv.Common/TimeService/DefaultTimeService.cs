using System;

namespace Asv.Common
{
    public class DefaultTimeService : ITimeService
    {
        private static readonly object _sync = new();
        private static DefaultTimeService _default;

        public static DefaultTimeService Default
        {
            get
            {
                if (_default != null) return _default;
                lock (_sync)
                {
                    _default ??= new DefaultTimeService();
                }
                return _default;
            }
        }
        
        public void SetCorrection(long correctionIn100NanosecondsTicks)
        {
            throw new NotImplementedException();
        }

        public DateTime Now => DateTime.Now;
    }
}