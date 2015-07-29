using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwixelChat.Constants;
using TwixelChat.Events;

namespace TwixelChat
{
    /// <summary>
    /// Chat client base class.
    /// Does everything except connect and disconnect from a server.
    /// </summary>
    public abstract class ChatClientBase
    {
        /// <summary>
        /// Server connection states.
        /// </summary>
        public enum ConnectionStates
        {
            /// <summary>
            /// Disconnected from server.
            /// </summary>
            Disconnected,
            /// <summary>
            /// Connecting to server.
            /// </summary>
            Connecting,
            /// <summary>
            /// Connected to server.
            /// </summary>
            Connected
        }

        /// <summary>
        /// Twitch chat logged in states.
        /// </summary>
        public enum LoggedInStates
        {
            /// <summary>
            /// Logged in to Twitch chat. Means a client can join a channel.
            /// </summary>
            LoggedIn,
            /// <summary>
            /// Not logged in to Twitch chat.
            /// Means a client must login first before they can join a channel.
            /// </summary>
            LoggedOut
        }

        // Probably going to remove this IDK
        public enum MessageType
        {
            Other,
            Number,
            Notice,
            PrivMsg
        }

        /// <summary>
        /// Raw unprocessed server message, used for all messages.
        /// </summary>
        public event EventHandler<RawMessageRecievedEventArgs> RawServerMessageRecieved;

        /// <summary>
        /// Raw message, does not include server messages.
        /// </summary>
        public event EventHandler<RawMessageRecievedEventArgs> RawMessageRecieved;

        /// <summary>
        /// Chat message.
        /// </summary>
        public event EventHandler<MessageRecievedEventArgs> MessageRecieved;

        /// <summary>
        /// Connection state change.
        /// </summary>
        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;

        /// <summary>
        /// Logged in state change.
        /// </summary>
        public event EventHandler<LoggedInEventArgs> LoggedInStateChanged;

        private ConnectionStates connectionState;
        /// <summary>
        /// Server connection state.
        /// </summary>
        public ConnectionStates ConnectionState
        {
            get
            {
                return connectionState;
            }
            protected internal set
            {
                if (connectionState != value)
                {
                    connectionState = value;
                    ConnectionStateChangedEvent(ConnectionState, ConnectionStateChanged);
                }
                connectionState = value;
            }
        }
        private LoggedInStates loggedInState;

        /// <summary>
        /// Twitch chat logged in state.
        /// </summary>
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

        /// <summary>
        /// Server output.
        /// </summary>
        public StreamReader Reader { get; protected internal set; }

        /// <summary>
        /// Server input.
        /// </summary>
        public StreamWriter Writer { get; protected internal set; }

        /// <summary>
        /// Channel info.
        /// </summary>
        public Channel Channel { get; protected internal set; }

        /// <summary>
        /// The Twitch username of the connected user
        /// </summary>
        public string Name { get; protected internal set; }

        /// <summary>
        /// Is the membership state capability enabled
        /// </summary>
        public bool MembershipCapabilityEnabled { get; protected internal set; }

        /// <summary>
        /// Is the commands capability enabled
        /// </summary>
        public bool CommandsCapabilityEnabled { get; protected internal set; }

        /// <summary>
        /// Are message tags enabled
        /// </summary>
        public bool TagsCapabilityEnabled { get; protected internal set; }

        private const long DefaultTimeOutTime = 5000;

        /// <summary>
        /// Time out time for waiting for server responses
        /// </summary>
        public TimeSpan TimeOutTime { get; set; }
        private int delayTime = 100;

        private bool loginFailed;
        private bool initatedPart;

        public ChatClientBase()
        {
            this.ConnectionState = ConnectionStates.Disconnected;
            this.loggedInState = LoggedInStates.LoggedOut;
            Channel = new TwixelChat.Channel();
            TimeOutTime = TimeSpan.FromMilliseconds(DefaultTimeOutTime);

            loginFailed = false;
            initatedPart = false;
        }

        /// <summary>
        /// Connect to the Twitch chat server
        /// </summary>
        /// <param name="name">Twitch username</param>
        /// <param name="accessToken">Twitch access token</param>
        /// <returns></returns>
        public abstract Task Connect(string name, string accessToken);

        /// <summary>
        /// Disconnect from the Twitch chat server
        /// </summary>
        public abstract void Disconnect();

        /// <summary>
        /// Authenticate to the Twitch chat server, should run immediately
        /// after connecting to the Twitch chat server
        /// </summary>
        /// <exception cref="TwixelChat.TwixelChatException">
        /// Throws an exception if login fails
        /// </exception>
        /// <exception cref="System.TimeoutException">
        /// Throws an exception if the server does not respond within the TimeOutTime limit
        /// </exception>
        /// <param name="name">Twitch username</param>
        /// <param name="accessToken">Twitch access token</param>
        /// <param name="membership">Enable membership capability</param>
        /// <param name="commands">Enable commands capability</param>
        /// <param name="tags">Enable message tags</param>
        /// <returns></returns>
        protected async Task Login(string name, string accessToken,
            bool membership = true, bool commands = true, bool tags = true)
        {
            await SendRawMessage("PASS oauth:" + accessToken);
            await SendRawMessage("NICK " + name);
            Name = name;
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

        /// <summary>
        /// Enables the membership capability
        /// </summary>
        /// <exception cref="System.TimeoutException">
        /// Throws an exception if the server does not respond within the TimeOutTime limit
        /// </exception>
        /// <returns></returns>
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

        /// <summary>
        /// Enables the commands capability
        /// </summary>
        /// <exception cref="System.TimeoutException">
        /// Throws an exception if the server does not respond within the TimeOutTime limit
        /// </exception>
        /// <returns></returns>
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

        /// <summary>
        /// Enables the tags capability
        /// </summary>
        /// <exception cref="System.TimeoutException">
        /// Throws an exception if the server does not respond within the TimeOutTime limit
        /// </exception>
        /// <returns></returns>
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
                // I'm pretty sure, probably need to check again
                await SendRawMessage("CAP REQ :" + capability);
            }
            else
            {
                throw new TwixelChatException("Can only send capability while not in channel.");
            }
        }

        /// <summary>
        /// Join a Twitch channel
        /// </summary>
        /// <exception cref="System.TimeoutException">
        /// Throws an exception if the server does not respond within the TimeOutTime limit
        /// </exception>
        /// <param name="channel">The channel name</param>
        /// <returns></returns>
        public async Task JoinChannel(string channel)
        {
            initatedPart = false;
            Channel.ChannelName = channel;
            await SendRawMessage("JOIN #" + channel);
            TimeoutTimer timer = CreateDefaultTimer("Did not receive channel connection confirmation. Not in channel.");
            while (Channel.ChannelState != Channel.ChannelStates.InChannel)
            {
                await Task.Delay(delayTime);
                timer.UpdateTimer();
            }
        }

        /// <summary>
        /// Leave a Twitch channel
        /// </summary>
        /// <exception cref="System.TimeoutException">
        /// Throws an exception if the server does not respond within the TimeOutTime limit
        /// </exception>
        /// <returns></returns>
        public async Task LeaveChannel()
        {
            initatedPart = true;
            await SendRawMessage("PART #" + Channel.ChannelName);
            TimeoutTimer timer = CreateDefaultTimer("Did not receive channel connection confirmation. Might still be in channel.");
            while (Channel.ChannelState != Channel.ChannelStates.NotInChannel)
            {
                await Task.Delay(delayTime);
                timer.UpdateTimer();
            }
        }

        /// <summary>
        /// Send the Twitch server a message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns></returns>
        public async Task SendRawMessage(string message)
        {
            await Writer.WriteLineAsync(message);
        }

        /// <summary>
        /// Send a Twitch channel a message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns></returns>
        public async Task SendMessage(string message)
        {
            await Writer.WriteLineAsync("PRIVMSG #" + Channel.ChannelName + " :" + message);
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
                return;
            }

            int firstSpace = rawServerMessage.IndexOf(' ');
            string tagsSection = null;
            string rawServerMessage_noTags = rawServerMessage;

            if (rawServerMessage.StartsWith("@"))
            {
                tagsSection = rawServerMessage.Substring(0, firstSpace);
                rawServerMessage_noTags = rawServerMessage.Substring(firstSpace + 1);
            }

            int spacePastHost = rawServerMessage_noTags.IndexOf(' ');
            string textPastHost = rawServerMessage_noTags.Substring(spacePastHost + 1);
            int secondSpace = textPastHost.IndexOf(' ');
            string host = rawServerMessage_noTags.Substring(1, spacePastHost - 1);
            string replyNumber = textPastHost.Substring(0, secondSpace);
            int secondColon = rawServerMessage_noTags.IndexOf(':', 1);
            string rawMessage = null;

            if (secondColon != -1)
            {
                rawMessage = rawServerMessage.Substring(secondColon + 1);
                RawMessageEvent(rawMessage, RawMessageRecieved);
            }

            // Handle Reply Number
            if (replyNumber == "001")
            {
                // Welcome
            }
            else if (replyNumber == "002")
            {
                // Host
            }
            else if (replyNumber == "003")
            {
                // Server is new
            }
            else if (replyNumber == "004")
            {
                // Mystery dash
            }
            else if (replyNumber == "353")
            {
                // RPL_NAMREPLY
                // Reply: Name reply
            }
            else if (replyNumber == "366")
            {
                // RPL_ENDOFNAMES
                // Reply: End of /NAMES list
                Channel.ChannelState = Channel.ChannelStates.InChannel;
            }
            else if (replyNumber == "372")
            {
                // RPL_MOTD
                // Reply: Message of the day message
            }
            else if (replyNumber == "375")
            {
                // RPL_MOTDSTART
                // Reply: Message of the day start
            }
            else if (replyNumber == "376")
            {
                // RPL_ENDOFMOTD
                // Reply: End of message of the day
                LoggedInState = LoggedInStates.LoggedIn;
            }
            else if (replyNumber == "CAP")
            {
                string[] split = rawServerMessage_noTags.Split(' ');
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
            }
            else if (replyNumber == "JOIN")
            {
                // we handle OUR channel join with 366
                // can handle other people joins here
            }
            else if (replyNumber == "PART")
            {
                // can handle parts here
                // pulling out the username here,
                // there's a ton of duplicate code in this library
                string username = host.Split('!')[0];
                if (initatedPart && username == Name)
                {
                    initatedPart = false;
                    Channel.ChannelState = TwixelChat.Channel.ChannelStates.NotInChannel;
                }
                else
                {
                    // some other user left the channel
                }
            }
            else if (replyNumber == "MODE")
            {
                if (host == "jtv")
                {
                    string[] split = rawServerMessage_noTags.Split(' ');
                    if (split[split.Length - 2] == "+o")
                    {
                        Channel.ElevatedUsers.Add(split[split.Length - 1]);
                    }
                    else if (split[split.Length - 2] == "-o")
                    {
                        Channel.ElevatedUsers.Remove(split[split.Length - 1]);
                    }
                }
            }
            else if (replyNumber == "CLEARCHAT")
            {
                // user was muted/banned
            }
            else if (replyNumber == "PRIVMSG")
            {
                ChatMessage message = new ChatMessage(rawServerMessage_noTags, tagsSection);
                MessageEvent(message, MessageRecieved);
            }
            else if (replyNumber == "USERSTATE")
            {
                if (!string.IsNullOrEmpty(tagsSection))
                {
                    Channel.ChannelUserState = new UserState(tagsSection, true);
                }
            }
            else if (replyNumber == "NOTICE")
            {
                if (!string.IsNullOrEmpty(rawMessage) && rawMessage == TwitchChatConstants.LoginUnsuccessful)
                {
                    loginFailed = true;
                }
                else
                {
                    Channel.HandleNotice(new ChannelNotice(rawServerMessage_noTags, tagsSection));
                }
            }
            else if (replyNumber == "ROOMSTATE")
            {
                Channel.HandleRoomState(rawServerMessage_noTags, tagsSection);
            }
            else
            {
                // welp, no idea what this replyNumber is
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

        void ConnectionStateChangedEvent(ConnectionStates state, EventHandler<ConnectionEventArgs> handler)
        {
            ConnectionEventArgs connectionEvent = new ConnectionEventArgs();
            connectionEvent.State = state;
            Event(connectionEvent, handler);
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

        private TimeoutTimer CreateDefaultTimer(string errorString)
        {
            return new TimeoutTimer(delayTime, TimeOutTime, errorString);
        }
    }
}
