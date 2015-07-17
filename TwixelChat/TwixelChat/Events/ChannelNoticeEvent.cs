using System;

namespace TwixelChat.Events
{
    public class ChannelNoticeEvent : EventArgs
    {
        /// <summary>
        /// The channel notice message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The channel notice message ID
        /// </summary>
        public ChannelNotice.MessageIds MessageId { get; set; }

        /// <summary>
        /// The new slow duration
        /// </summary>
        public long? SlowDuration { get; set; }

        /// <summary>
        /// The new host channel
        /// </summary>
        public string HostChannel { get; set; }
    }
}
