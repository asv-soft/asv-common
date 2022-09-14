using System;

namespace Asv.Common
{
    public interface ITimeService
    {
        void SetCorrection(long correctionIn100NanosecondsTicks);
        DateTime Now { get; }
    }
}
