using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    internal class TimeoutTimer
    {
        TimeSpan timer;
        long delayTime;
        TimeSpan timeoutTime;
        string errorString;

        internal TimeoutTimer(long delayTime, TimeSpan timeoutTime, string errorString)
        {
            timer = TimeSpan.Zero;
            this.delayTime = delayTime;
            this.timeoutTime = timeoutTime;
            this.errorString = errorString;
        }

        internal void UpdateTimer()
        {
            timer += TimeSpan.FromMilliseconds(delayTime);
            if (timer >= timeoutTime)
            {
                throw new TimeoutException(errorString);
            }
        }
    }
}
