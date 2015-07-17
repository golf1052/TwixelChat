using System.Collections.Generic;
using TwixelChat.Helpers;

namespace TwixelChat
{
    /// <summary>
    /// Class representing a channel notice.
    /// </summary>
    public class ChannelNotice
    {
        /// <summary>
        /// Message IDs
        /// </summary>
        public enum MessageIds
        {
            None,
            SubsOn,
            SubsOff,
            SlowOn,
            SlowOff,
            R9KOn,
            R9KOff,
            HostOn,
            HostOff
        }

        /// <summary>
        /// Channel notice message ID
        /// </summary>
        public MessageIds MessageId { get; private set; }

        /// <summary>
        /// Channel notice message
        /// </summary>
        public string Message { get; private set; }

        public ChannelNotice(string rawServerMessage)
        {
            MessageId = MessageIds.None;
            string rest = rawServerMessage;

            if (rawServerMessage.StartsWith("@"))
            {
                int splitIndex = rawServerMessage.IndexOf(' ');

                string tagsSection = rawServerMessage.Substring(0, splitIndex);

                Dictionary<string, string> tags = HelperMethods.GetTags(tagsSection);

                if (!string.IsNullOrEmpty(tags["msg-id"]))
                {
                    if (tags["msg-id"] == "subs_on")
                    {
                        MessageId = MessageIds.SubsOn;
                    }
                    else if (tags["msg-id"] == "subs_off")
                    {
                        MessageId = MessageIds.SubsOff;
                    }
                    else if (tags["msg-id"] == "slow_on")
                    {
                        MessageId = MessageIds.SlowOn;
                    }
                    else if (tags["msg-id"] == "slow_off")
                    {
                        MessageId = MessageIds.SlowOff;
                    }
                    else if (tags["msg-id"] == "r9k_on")
                    {
                        MessageId = MessageIds.R9KOn;
                    }
                    else if (tags["msg-id"] == "r9k_off")
                    {
                        MessageId = MessageIds.R9KOff;
                    }
                    else if (tags["msg-id"] == "host_on")
                    {
                        MessageId = MessageIds.HostOn;
                    }
                    else if (tags["msg-id"] == "host_off")
                    {
                        MessageId = MessageIds.HostOff;
                    }
                }

                rest = rawServerMessage.Substring(splitIndex + 1);
            }

            Message = rest.Substring(1).Substring(rest.IndexOf(':') + 1);
        }
    }
}
