using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    public class Channel
    {
        public enum ChannelStates
        {
            InChannel,
            NotInChannel
        }

        public ChannelStates ChannelState { get; internal set; }
        public string ChannelName { get; internal set; }
        public UserState ChannelUserState { get; internal set; }

        public List<string> Mods { get; internal set; }

        public bool SubMode { get; private set; }
        public bool SlowMode { get; private set; }
        public long SlowDuration { get; private set; }
        public bool R9KMode { get; private set; }
        public bool HostMode { get; private set; }
        public string HostChannel { get; private set; }

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
