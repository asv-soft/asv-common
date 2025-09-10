using System;
using System.Threading;

namespace Asv.Common
{
    public class CorrectedTimeService : ITimeService
    {
        private long _correction;

        public void SetCorrection(long correctionIn100NanosecondsTicks)
        {
            Interlocked.Exchange(ref _correction, correctionIn100NanosecondsTicks);
        }

        public DateTime Now => DateTime.Now.AddTicks(Interlocked.Read(ref _correction));
    }
}
