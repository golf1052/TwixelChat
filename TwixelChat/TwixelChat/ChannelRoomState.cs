using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwixelChat.Helpers;

namespace TwixelChat
{
    public class ChannelRoomState
    {
        /// <summary>
        /// The broadcasters language. Currently isn't used so this will always be null.
        /// </summary>
        public string BroadcasterLang { get; private set; }

        /// <summary>
        /// Is the channel in R9K mode.
        /// Messages with more than 9 characters must be unique.
        /// </summary>
        public bool? R9K { get; private set; }

        /// <summary>
        /// Is the channel in sub only mode.
        /// Only subs and mods can send messages.
        /// </summary>
        public bool? SubsOnly { get; private set; }

        /// <summary>
        /// The amount of seconds a user must wait before sending a message if the room is in slow mode.
        /// If the room is not in slow mode this is set to 0.
        /// </summary>
        public int? Slow { get; private set; }

        public ChannelRoomState(string rawServerMessage)
        {
            if (rawServerMessage.StartsWith("@"))
            {
                int splitIndex = rawServerMessage.IndexOf(' ');
                string tagsSection = rawServerMessage.Substring(0, splitIndex);
                Dictionary<string, string> tags = HelperMethods.GetTags(tagsSection);
                if (tags.ContainsKey("broadcaster-lang"))
                {
                    if (!string.IsNullOrEmpty(tags["broadcaster-lang"]))
                    {
                        BroadcasterLang = tags["broadcaster-lang"];
                    }
                }
                if (tags.ContainsKey("r9k"))
                {
                    if (!string.IsNullOrEmpty(tags["r9k"]))
                    {
                        R9K = NumberToBoolean(int.Parse(tags["r9k"]));
                    }
                }
                if (tags.ContainsKey("subs-only"))
                {
                    if (!string.IsNullOrEmpty(tags["subs-only"]))
                    {
                        SubsOnly = NumberToBoolean(int.Parse(tags["subs-only"]));
                    }
                }
                if (tags.ContainsKey("slow"))
                {
                    if (!string.IsNullOrEmpty(tags["slow"]))
                    {
                        Slow = int.Parse(tags["slow"]);
                    }
                }
            }
        }

        private bool NumberToBoolean(int num)
        {
            if (num == 0)
            {
                return false;
            }
            else if (num == 1)
            {
                return true;
            }
            else
            {
                throw new Exception("The number wasn't 0 or 1");
            }
        }
    }
}
