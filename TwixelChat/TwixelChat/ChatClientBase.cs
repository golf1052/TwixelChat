using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwixelChat.Constants;

namespace TwixelChat
{
    public abstract class ChatClientBase
    {
        public enum ConnectionStates
        {
            Disconnected,
            Connecting,
            Connected
        }

        public enum LoggedInStates
        {
            LoggedIn,
            LoggedOut
        }

        public enum MessageType
        {
            Other,
            Number,
            Notice,
            PrivMsg
        }

        public event EventHandler<RawMessageRecievedEventArgs> RawServerMessageRecieved;
        public event EventHandler<RawMessageRecievedEventArgs> RawMessageRecieved;
        public event EventHandler<MessageRecievedEventArgs> MessageRecieved;
        public event EventHandler<LoggedInEventArgs> LoggedInStateChanged;
        public event EventHandler LogInFailed;

        public ConnectionStates ConnectionState { get; protected internal set; }
        private LoggedInStates loggedInState;
        public LoggedInStates LoggedInState
        {
            get
            {
                return loggedInState;
            }
            protected internal set
            {
                if (loggedInState != value)
                {
                    loggedInState = value;
                    LoggedInStateChangedEvent(LoggedInState, LoggedInStateChanged);
                }
                loggedInState = value;
            }
        }
        public StreamReader Reader { get; protected internal set; }
        public StreamWriter Writer { get; protected internal set; }
        public Channel Channel { get; protected internal set; }

        public bool MembershipCapabilityEnabled { get; protected internal set; }
        public bool CommandsCapabilityEnabled { get; protected internal set; }
        public bool TagsCapabilityEnabled { get; protected internal set; }

        private const long DefaultTimeOutTime = 5000;
        public TimeSpan TimeOutTime { get; set; }
        private int delayTime = 100;

        private bool loginFailed;

        public ChatClientBase()
        {
            this.ConnectionState = ConnectionStates.Disconnected;
            this.loggedInState = LoggedInStates.LoggedOut;
            Channel = new TwixelChat.Channel();
            TimeOutTime = TimeSpan.FromMilliseconds(DefaultTimeOutTime);

            loginFailed = false;
        }

        public abstract Task Connect(string name, string accessToken);
        public abstract void Disconnect();

        protected async Task Login(string name, string accessToken,
            bool membership = true, bool commands = true, bool tags = true)
        {
            await SendRawMessage("PASS oauth:" + accessToken);
            await SendRawMessage("NICK " + name);
            if (membership)
            {
                await EnableMembershipCapability();
            }
            
            if (commands)
            {
                await EnableCommandsCapability();
            }
            
            if (tags)
            {
                await EnableTagsCapability();
            }
            
            TimeoutTimer timer = CreateDefaultTimer("Did not receive login confirmation. Not logged in.");
            //while (LoggedInState != LoggedInStates.LoggedIn)
            //{
            //    await Task.Delay(delayTime);
            //    timer.UpdateTimer();
            //    if (loginFailed)
            //    {
            //        loginFailed = false;
            //        throw new TwixelChatException(TwitchChatConstants.LoginUnsuccessful);
            //    }
            //}
        }

        public async Task EnableMembershipCapability()
        {
            await SendCapability(TwitchChatConstants.MembershipCapability);
            TimeoutTimer timer = CreateDefaultTimer("Did not receive membership capability ACK");
            //while (MembershipCapabilityEnabled != true)
            //{
            //    await Task.Delay(delayTime);
            //    timer.UpdateTimer();
            //}
        }

        public async Task EnableCommandsCapability()
        {
            await SendCapability(TwitchChatConstants.CommandsCapability);
            TimeoutTimer timer = CreateDefaultTimer("Did not receive commands capability ACK");
            //while (CommandsCapabilityEnabled != true)
            //{
            //    await Task.Delay(delayTime);
            //    timer.UpdateTimer();
            //}
        }

        public async Task EnableTagsCapability()
        {
            await SendCapability(TwitchChatConstants.TagsCapability);
            TimeoutTimer timer = CreateDefaultTimer("Did not receive tags capability ACK");
            //while (TagsCapabilityEnabled !=  true)
            //{
            //    await Task.Delay(delayTime);
            //    timer.UpdateTimer();
            //}
        }

        protected async Task SendCapability(string capability)
        {
            if (Channel.ChannelState == TwixelChat.Channel.ChannelStates.NotInChannel)
            {
                // You can't remove a capability once you send it
                await SendRawMessage("CAP REQ :" + capability);
            }
            else
            {
                throw new TwixelChatException("Can only send capability while not in channel.");
            }
        }

        public async Task JoinChannel(string channel)
        {
            Channel.ChannelName = channel;
            await SendRawMessage("JOIN #" + channel);
            TimeoutTimer timer = CreateDefaultTimer("Did not receive channel connection confirmation. Not in channel.");
            //while (ChannelState != ChannelStates.InChannel)
            //{
            //    await Task.Delay(delayTime);
            //    timer.UpdateTimer();
            //}
        }

        public async Task LeaveChannel()
        {
            await SendRawMessage("PART #" + Channel);
            TimeoutTimer timer = CreateDefaultTimer("Did not receive channel connection confirmation. Not in channel.");
            //while (ChannelState != ChannelStates.NotInChannel)
            //{
            //    await Task.Delay(delayTime);
            //    timer.UpdateTimer();
            //}
        }

        public async Task SendRawMessage(string message)
        {
            await Writer.WriteLineAsync(message);
        }

        public async Task SendMessage(string message)
        {
            await Writer.WriteLineAsync("PRIVMSG #" + Channel + " :" + message);
        }

        protected async Task ReadFromStream(CancellationToken cancellationToken)
        {
            string result = null;
            while ((result = await Reader.ReadLineAsync()) != null)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                await HandleResponse(result);
            }
        }

        async Task HandleResponse(string rawServerMessage)
        {
            Debug.WriteLine(rawServerMessage);
            // First send off raw server message event.
            RawMessageEvent(rawServerMessage, RawServerMessageRecieved);

            // Now check for PINGs because those require no processing
            if (rawServerMessage.StartsWith("PING"))
            {
                await SendRawMessage("PONG " + TwitchChatConstants.TwitchHost);
                Debug.WriteLine("Sent pong");
                return;
            }

            if (rawServerMessage.StartsWith("@"))
            {
                int splitIndex = rawServerMessage.IndexOf(' ');
                string rest = rawServerMessage.Substring(splitIndex + 1);
                string[] splitSpaces = rest.Split(' ');

                if (splitSpaces[1] == "PRIVMSG")
                {
                    ChatMessage message = new ChatMessage(rawServerMessage);
                    MessageEvent(message, MessageRecieved);
                }
                else if (splitSpaces[1] == "USERSTATE")
                {
                    string tagsSection = rawServerMessage.Substring(0, splitIndex);
                    Channel.ChannelUserState = new UserState(tagsSection, true);
                }
                else if (splitSpaces[1] == "NOTICE")
                {
                    // do notice stuff
                    Channel.HandleNotice(new ChannelNotice(rawServerMessage));
                }
                else
                {
                    // some kind of error?
                    Debug.WriteLine("¯\\_(ツ)_/¯");
                }
            }
            else if (rawServerMessage.StartsWith(":"))
            {

            }
            else
            {

            }

            bool hasParts = false;
            int secondColon = rawServerMessage.IndexOf(':', 1);
            string firstPart = null;
            string secondPart = null;
            //if (secondColon != -1)
            //{
            //    hasParts = true;
            //    firstPart = message.Substring(0, secondColon + 1);
            //    secondPart = message.Substring(secondColon + 1);
            //    RawMessageEvent(secondPart, RawMessageRecieved);
            //}
            
            //if (hasParts)
            //{
            //    RawMessageEvent(secondPart, RawMessageRecieved);
            //    string[] firstSplit = firstPart.Split(' ');
            //    if (firstPart.StartsWith(":"))
            //    {
            //        MessageType messageType = HandleReplyNumber(firstSplit[1], secondPart);
            //        if (messageType == MessageType.PrivMsg)
            //        {
            //            string[] splitName = firstSplit[0].Split('!');
            //            MessageEvent(splitName[0].Substring(1), secondPart, MessageRecieved);
            //        }
            //    }
            //}
            //else
            //{
            //    if (message.Contains("PART"))
            //    {
            //        ChannelState = ChannelStates.NotInChannel;
            //    }
            //}
        }

        void MessageEvent(ChatMessage message, EventHandler<MessageRecievedEventArgs> handler)
        {
            MessageRecievedEventArgs messageEvent = new MessageRecievedEventArgs();
            messageEvent.ChatMessage = message;
            messageEvent.Message = message.Message;
            Event(messageEvent, handler);
        }

        void RawMessageEvent(string message, EventHandler<RawMessageRecievedEventArgs> handler)
        {
            RawMessageRecievedEventArgs messageEvent = new RawMessageRecievedEventArgs();
            messageEvent.Message = message;
            Event(messageEvent, handler);
        }

        void LoggedInStateChangedEvent(LoggedInStates state, EventHandler<LoggedInEventArgs> handler)
        {
            LoggedInEventArgs loggedInEvent = new LoggedInEventArgs();
            loggedInEvent.State = state;
            Event(loggedInEvent, handler);
        }

        protected virtual void Event<T>(T e, EventHandler<T> h)
        {
            EventHandler<T> handler = h;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void EmptyEvent(EventArgs e, EventHandler h)
        {
            EventHandler handler = h;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        MessageType HandleReplyNumber(string number, string message)
        {
            if (number == "001")
            {
                // Welcome
                return MessageType.Number;
            }
            else if (number == "002")
            {
                // Host
                return MessageType.Number;
            }
            else if (number == "003")
            {
                // Server is new
                return MessageType.Number;
            }
            else if (number == "004")
            {
                // Mystery dash
                return MessageType.Number;
            }
            else if (number == "353")
            {
                // RPL_NAMREPLY
                // Reply: Name reply
                return MessageType.Number;
            }
            else if (number == "366")
            {
                // RPL_ENDOFNAMES
                // Reply: End of /NAMES list
                Channel.ChannelState = Channel.ChannelStates.InChannel;
                return MessageType.Number;
            }
            else if (number == "372")
            {
                // RPL_MOTD
                // Reply: Message of the day message
                return MessageType.Number;
            }
            else if (number == "375")
            {
                // RPL_MOTDSTART
                // Reply: Message of the day start
                return MessageType.Number;
            }
            else if (number == "376")
            {
                // RPL_ENDOFMOTD
                // Reply: End of message of the day
                LoggedInState = LoggedInStates.LoggedIn;
                return MessageType.Number;
            }
            else if (number == "NOTICE")
            {
                if (message == TwitchChatConstants.LoginUnsuccessful)
                {
                    loginFailed = true;
                }
                return MessageType.Notice;
            }
            else if (number == "PRIVMSG")
            {
                return MessageType.PrivMsg;
            }
            else
            {
                return MessageType.Other;
            }
        }

        private TimeoutTimer CreateDefaultTimer(string errorString)
        {
            return new TimeoutTimer(delayTime, TimeOutTime, errorString);
        }
    }
}
