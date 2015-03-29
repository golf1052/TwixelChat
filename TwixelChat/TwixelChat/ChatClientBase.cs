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

        public enum ChannelStates
        {
            InChannel,
            NotInChannel
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

        public ChannelStates ChannelState { get; protected internal set; }
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
        public string Channel { get; protected internal set; }

        private TimeSpan timer;
        private long defaultTimeOutTime = 5000;
        public long TimeOutTime { get; set; }
        private int delayTime = 100;

        private bool loginFailed;

        public ChatClientBase()
        {
            this.ConnectionState = ConnectionStates.Disconnected;
            this.loggedInState = LoggedInStates.LoggedOut;
            this.ChannelState = ChannelStates.NotInChannel;
            timer = TimeSpan.Zero;
            TimeOutTime = defaultTimeOutTime;

            loginFailed = false;
        }

        public abstract Task Connect(string name, string accessToken, long timeOutTime = 0);
        public abstract void Disconnect();

        protected async Task Login(string name, string accessToken, long timeOutTime = 0)
        {
            if (timeOutTime <= 0)
            {
                timeOutTime = TimeOutTime;
            }
            ResetTimer();
            await SendRawMessage("PASS oauth:" + accessToken);
            await SendRawMessage("NICK " + name);
            while (LoggedInState != LoggedInStates.LoggedIn)
            {
                await Task.Delay(delayTime);
                timer += TimeSpan.FromMilliseconds(delayTime);
                if (timer >= TimeSpan.FromMilliseconds(TimeOutTime))
                {
                    throw new TimeoutException("Did not recieve login confirmation. Not logged in.");
                }
                if (loginFailed)
                {
                    loginFailed = false;
                    throw new TwixelChatException(TwitchChatConstants.LoginUnsuccessful);
                }
            }
        }

        public async Task JoinChannel(string channel, long timeOutTime = 0)
        {
            if (timeOutTime <= 0)
            {
                timeOutTime = TimeOutTime;
            }
            ResetTimer();
            Channel = channel;
            await SendRawMessage("JOIN #" + channel);
            while (ChannelState != ChannelStates.InChannel)
            {
                await Task.Delay(delayTime);
                timer += TimeSpan.FromMilliseconds(delayTime);
                if (timer >= TimeSpan.FromMilliseconds(TimeOutTime))
                {
                    throw new TimeoutException("Did not recieve channel connection confirmation. Not in channel.");
                }
            }
        }

        public async Task LeaveChannel(long timeOutTime = 0)
        {
            if (timeOutTime <= 0)
            {
                timeOutTime = TimeOutTime;
            }
            ResetTimer();
            await SendRawMessage("PART #" + Channel);
            while (ChannelState != ChannelStates.NotInChannel)
            {
                await Task.Delay(delayTime);
                timer += TimeSpan.FromMilliseconds(delayTime);
                if (timer >= TimeSpan.FromMilliseconds(TimeOutTime))
                {
                    throw new TimeoutException("Did not recieve channel connection confirmation. Not in channel.");
                }
            }
        }

        void ResetTimer()
        {
            timer = TimeSpan.Zero;
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
                Debug.WriteLine(result);
                await HandleResponse(result);
            }
        }

        async Task HandleResponse(string message)
        {
            RawMessageEvent(message, RawServerMessageRecieved);
            bool hasParts = false;
            int secondColon = message.IndexOf(':', 1);
            string firstPart = null;
            string secondPart = null;
            if (secondColon != -1)
            {
                hasParts = true;
                firstPart = message.Substring(0, secondColon + 1);
                secondPart = message.Substring(secondColon + 1);
                RawMessageEvent(secondPart, RawMessageRecieved);
            }
            if (message.StartsWith("PING"))
            {
                await SendRawMessage("PONG " + TwitchChatConstants.TwitchHost);
                Debug.WriteLine("Sent pong");
            }
            if (hasParts)
            {
                RawMessageEvent(secondPart, RawMessageRecieved);
                string[] firstSplit = firstPart.Split(' ');
                if (firstPart.StartsWith(":"))
                {
                    MessageType messageType = HandleReplyNumber(firstSplit[1], secondPart);
                    if (messageType == MessageType.PrivMsg)
                    {
                        string[] splitName = firstSplit[0].Split('!');
                        MessageEvent(splitName[0].Substring(1), secondPart, MessageRecieved);
                    }
                }
            }
            else
            {
                if (message.Contains("PART"))
                {
                    ChannelState = ChannelStates.NotInChannel;
                }
            }
        }

        void MessageEvent(string username, string message, EventHandler<MessageRecievedEventArgs> handler)
        {
            MessageRecievedEventArgs messageEvent = new MessageRecievedEventArgs();
            messageEvent.Username = username;
            messageEvent.Message = message;
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
                ChannelState = ChannelStates.InChannel;
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
    }
}
