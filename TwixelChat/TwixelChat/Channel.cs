using System;
using System.Collections.Generic;
using TwixelChat.Events;

namespace TwixelChat
{
    /// <summary>
    /// Class for handling channel stuffs.
    /// </summary>
    public class Channel
    {
        /// <summary>
        /// Channel connection states.
        /// </summary>
        public enum ChannelStates
        {
            /// <summary>
            /// Currently in a channel.
            /// </summary>
            InChannel,
            /// <summary>
            /// Currently not in a channel.
            /// </summary>
            NotInChannel
        }

        private ChannelStates channelState;
        /// <summary>
        /// Channel connection state.
        /// </summary>
        public ChannelStates ChannelState
        {
            get
            {
                return channelState;
            }
            internal set
            {
                if (channelState != value)
                {
                    channelState = value;
                    ChannelConnectionEventArgs connectionEvent = new ChannelConnectionEventArgs();
                    connectionEvent.State = value;
                    Event(connectionEvent, ChannelConnectionStateChanged);
                }
            }
        }

        /// <summary>
        /// Channel name.
        /// </summary>
        public string ChannelName { get; internal set; }

        /// <summary>
        /// Channel user state.
        /// </summary>
        public UserState ChannelUserState { get; internal set; }

        /// <summary>
        /// List of mods in the channel.
        /// May not be correct due to heavy Twitch chat load.
        /// </summary>
        public List<string> Mods { get; internal set; }

        /// <summary>
        /// Is the channel in sub only mode.
        /// Only subs and mods can send messages.
        /// </summary>
        public bool SubMode { get; private set; }

        /// <summary>
        /// Is the channel in slow mode.
        /// Messages can be sent to the server every X seconds.
        /// X is SlowDuration.
        /// </summary>
        public bool SlowMode { get; private set; }

        /// <summary>
        /// The amount of seconds a user must wait before sending a message if the room is in slow mode.
        /// If the room is not in slow mode this is set to 0.
        /// </summary>
        public long SlowDuration { get; private set; }

        /// <summary>
        /// Is the channel in R9K mode.
        /// Messages with more than 9 characters must be unique.
        /// </summary>
        public bool R9KMode { get; private set; }

        /// <summary>
        /// Is the channel hosting another channel.
        /// </summary>
        public bool HostMode { get; private set; }
        
        /// <summary>
        /// The channel name that this channel is hosting.
        /// If the channel is not hosting a channel this will be null.
        /// </summary>
        public string HostChannel { get; private set; }

        /// <summary>
        /// Channel connection state change.
        /// </summary>
        public event EventHandler<ChannelConnectionEventArgs> ChannelConnectionStateChanged;

        /// <summary>
        /// Channel notice was received.
        /// </summary>
        public event EventHandler<ChannelNoticeEvent> ChannelEventRecieved;

        public Channel()
        {
            ChannelState = ChannelStates.NotInChannel;
            ChannelName = null;
            ChannelUserState = null;
            Mods = new List<string>();
            SubMode = false;
            SlowMode = false;
            SlowDuration = 0;
            R9KMode = false;
            HostMode = false;
            HostChannel = null;
        }

        /// <summary>
        /// Handles a channel notice message.
        /// </summary>
        /// <param name="notice">The channel notice</param>
        public void HandleNotice(ChannelNotice notice)
        {
            if (notice.MessageId == ChannelNotice.MessageIds.None)
            {
                // custom parsing waaat
                // might not exist actually
            }
            else if (notice.MessageId == ChannelNotice.MessageIds.SubsOn)
            {
                SubMode = true;
            }
            else if (notice.MessageId == ChannelNotice.MessageIds.SubsOff)
            {
                SubMode = false;
            }
            else if (notice.MessageId == ChannelNotice.MessageIds.SlowOn)
            {
                SlowMode = true;
                SlowDuration = long.Parse(notice.Message.Split(' ')[12]);
            }
            else if (notice.MessageId == ChannelNotice.MessageIds.SlowOff)
            {
                SlowMode = false;
                SlowDuration = 0;
            }
            else if (notice.MessageId == ChannelNotice.MessageIds.R9KOn)
            {
                R9KMode = true;
            }
            else if (notice.MessageId == ChannelNotice.MessageIds.R9KOff)
            {
                R9KMode = false;
            }
            else if (notice.MessageId == ChannelNotice.MessageIds.HostOn)
            {
                HostMode = true;
                HostChannel = notice.Message.Split(' ')[2];
            }
            else if (notice.MessageId == ChannelNotice.MessageIds.HostOff)
            {
                HostMode = false;
                HostChannel = null;
            }

            ChannelNoticeEvent channelEvent = new ChannelNoticeEvent();
            channelEvent.Message = notice.Message;
            channelEvent.MessageId = notice.MessageId;
            channelEvent.SlowDuration = SlowDuration;
            channelEvent.HostChannel = HostChannel;
            Event(channelEvent, ChannelEventRecieved);
        }

        private void Event<T>(T e, EventHandler<T> h)
        {
            EventHandler<T> handler = h;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
