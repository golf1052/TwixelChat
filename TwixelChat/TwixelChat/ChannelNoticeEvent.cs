using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    public class ChannelNoticeEvent : EventArgs
    {
        public string Message { get; set; }
        public ChannelNotice.MessageIds MessageId { get; set; }
        public long? SlowDuration { get; set; }
        public string HostChannel { get; set; }
    }
}
