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
            while (LoggedInState != LoggedInStates.LoggedIn)
            {
                await Task.Delay(delayTime);
                timer.UpdateTimer();
                if (loginFailed)
                {
                    loginFailed = false;
                    throw new TwixelChatException(TwitchChatConstants.LoginUnsuccessful);
                }
            }
        }

        public async Task EnableMembershipCapability()
        {
            await SendCapability(TwitchChatConstants.MembershipCapability);
            TimeoutTimer timer = CreateDefaultTimer("Did not receive membership capability ACK");
            while (MembershipCapabilityEnabled != true)
            {
                await Task.Delay(delayTime);
                timer.UpdateTimer();
            }
        }

        public async Task EnableCommandsCapability()
        {
            await SendCapability(TwitchChatConstants.CommandsCapability);
            TimeoutTimer timer = CreateDefaultTimer("Did not receive commands capability ACK");
            while (CommandsCapabilityEnabled != true)
            {
                await Task.Delay(delayTime);
                timer.UpdateTimer();
            }
        }

        public async Task EnableTagsCapability()
        {
            await SendCapability(TwitchChatConstants.TagsCapability);
            TimeoutTimer timer = CreateDefaultTimer("Did not receive tags capability ACK");
            while (TagsCapabilityEnabled != true)
            {
                await Task.Delay(delayTime);
                timer.UpdateTimer();
            }
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
            while (Channel.ChannelState != Channel.ChannelStates.InChannel)
            {
                await Task.Delay(delayTime);
                timer.UpdateTimer();
            }
        }

        public async Task LeaveChannel()
        {
            await SendRawMessage("PART #" + Channel);
            TimeoutTimer timer = CreateDefaultTimer("Did not receive channel connection confirmation. Might still be in channel.");
            while (Channel.ChannelState != Channel.ChannelStates.NotInChannel)
            {
                await Task.Delay(delayTime);
                timer.UpdateTimer();
            }
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

            int firstSpace = rawServerMessage.IndexOf(' ');
            string rest = rawServerMessage.Substring(firstSpace + 1);

            if (rawServerMessage.StartsWith("@"))
            {
                string[] splitSpaces = rest.Split(' ');

                if (splitSpaces[1] == "PRIVMSG")
                {
                    ChatMessage message = new ChatMessage(rawServerMessage);
                    RawMessageEvent(message.Message, RawMessageRecieved);
                    MessageEvent(message, MessageRecieved);
                }
                else if (splitSpaces[1] == "USERSTATE")
                {
                    string tagsSection = rawServerMessage.Substring(0, firstSpace);
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
                int secondSpace = rest.IndexOf(' ');
                string host = rawServerMessage.Substring(1, firstSpace - 1);
                string replyNumber = rest.Substring(0, secondSpace);
                int secondColon = rawServerMessage.IndexOf(':', 1);
                string rawMessage = null;
                if (secondColon != -1)
                {
                    rawMessage = rawServerMessage.Substring(secondColon + 1);
                    RawMessageEvent(rawMessage, RawMessageRecieved);
                }

                HandleReplyNumber(rawServerMessage, host, replyNumber, rawMessage);
            }
            else
            {
                // ping pong part
                if (rawServerMessage.StartsWith("PART"))
                {
                    Channel.ChannelState = Channel.ChannelStates.NotInChannel;
                }
            }
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

        MessageType HandleReplyNumber(string raw, string host,
            string replyNumber, string message)
        {
            if (replyNumber == "001")
            {
                // Welcome
                return MessageType.Number;
            }
            else if (replyNumber == "002")
            {
                // Host
                return MessageType.Number;
            }
            else if (replyNumber == "003")
            {
                // Server is new
                return MessageType.Number;
            }
            else if (replyNumber == "004")
            {
                // Mystery dash
                return MessageType.Number;
            }
            else if (replyNumber == "353")
            {
                // RPL_NAMREPLY
                // Reply: Name reply
                return MessageType.Number;
            }
            else if (replyNumber == "366")
            {
                // RPL_ENDOFNAMES
                // Reply: End of /NAMES list
                Channel.ChannelState = Channel.ChannelStates.InChannel;
                return MessageType.Number;
            }
            else if (replyNumber == "372")
            {
                // RPL_MOTD
                // Reply: Message of the day message
                return MessageType.Number;
            }
            else if (replyNumber == "375")
            {
                // RPL_MOTDSTART
                // Reply: Message of the day start
                return MessageType.Number;
            }
            else if (replyNumber == "376")
            {
                // RPL_ENDOFMOTD
                // Reply: End of message of the day
                LoggedInState = LoggedInStates.LoggedIn;
                return MessageType.Number;
            }
            else if (replyNumber == "CAP")
            {
                string[] split = raw.Split(' ');
                if (split.Length >= 5)
                {
                    if (split[2] == "*" && split[3] == "ACK")
                    {
                        string enabledTag = split[4].Substring(1);
                        if (enabledTag == TwitchChatConstants.MembershipCapability)
                        {
                            MembershipCapabilityEnabled = true;
                        }
                        else if (enabledTag == TwitchChatConstants.CommandsCapability)
                        {
                            CommandsCapabilityEnabled = true;
                        }
                        else if (enabledTag == TwitchChatConstants.TagsCapability)
                        {
                            TagsCapabilityEnabled = true;
                        }
                    }
                }
                else
                {
                    // some kind of error?
                    Debug.WriteLine("Something is wrong with CAPs");
                }
                return MessageType.Other;
            }
            else if (replyNumber == "JOIN")
            {
                // we handle OUR channel join with 366
                // can handle other people joins here
                return MessageType.Other;
            }
            else if (replyNumber == "PART")
            {
                // can handle parts here
                return MessageType.Other;
            }
            else if (replyNumber == "MODE")
            {
                if (host == "jtv")
                {
                    string[] split = raw.Split(' ');
                    if (split[split.Length - 2] == "+o")
                    {
                        Channel.Mods.Add(split[split.Length - 1]);
                    }
                    else if (split[split.Length - 2] == "-o")
                    {
                        Channel.Mods.Remove(split[split.Length - 1]);
                    }
                }
                return MessageType.Other;
            }
            else if (replyNumber == "NOTICE")
            {
                if (!string.IsNullOrEmpty(message) && message == TwitchChatConstants.LoginUnsuccessful)
                {
                    loginFailed = true;
                }
                return MessageType.Notice;
            }
            else if (replyNumber == "CLEARCHAT")
            {
                // user was muted/banned
                return MessageType.Other;
            }
            else if (replyNumber == "PRIVMSG")
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
