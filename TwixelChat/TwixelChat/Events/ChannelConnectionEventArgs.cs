using System;

namespace TwixelChat.Events
{
    public class ChannelConnectionEventArgs : EventArgs
    {
        public Channel.ChannelStates State { get; set; }
    }
}
