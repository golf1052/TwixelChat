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
    public abstract class ClientBase : IDisposable
    {
        private bool disposed = false;

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

        public event EventHandler<MessageRecievedEventArgs> RawServerMessageRecieved;
        public event EventHandler<MessageRecievedEventArgs> RawMessageRecieved;
        public event EventHandler<MessageRecievedEventArgs> MessageRecieved;
        public event EventHandler<LoggedInEventArgs> LoggedInStateChanged;

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
        public string Channel { get; private set; }

        public ClientBase(string channel)
        {
            this.Channel = channel;
            this.ConnectionState = ConnectionStates.Disconnected;
        }

        public abstract Task Connect(string name, string accessToken);
        public abstract void Disconnect();

        protected async Task Login(string name, string accessToken)
        {
            await SendMessage("PASS oauth:" + accessToken);
            await SendMessage("NICK " + name);
        }

        public async Task SendMessage(string message)
        {
            await Writer.WriteLineAsync (message);
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
            MessageEvent(message, RawServerMessageRecieved);
            bool hasParts = false;
            int secondColon = message.IndexOf(':', 1);
            string firstPart = null;
            string secondPart = null;
            if (secondColon != -1)
            {
                hasParts = true;
                firstPart = message.Substring(0, secondColon + 1);
                secondPart = message.Substring(secondColon + 1);
                MessageEvent(secondPart, RawMessageRecieved);
            }
            if (hasParts)
            {
                string[] firstSplit = firstPart.Split(' ');
                if (firstPart.StartsWith(":"))
                {
                    if (firstSplit[0].Substring(1) == TwitchChatConstants.TwitchHost)
                    {
                        HandleReplyNumber(firstSplit[1]);
                    }
                }
            }
            if (message.StartsWith("PING"))
            {
                await SendMessage("PONG " + TwitchChatConstants.TwitchHost);
            }
        }

        void MessageEvent(string message, EventHandler<MessageRecievedEventArgs> handler)
        {
            MessageRecievedEventArgs messageEvent = new MessageRecievedEventArgs();
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

        void HandleReplyNumber(string number)
        {
            if (number == "001")
            {
                // Welcome
            }
            else if (number == "002")
            {
                // Host
            }
            else if (number == "003")
            {
                // Server is new
            }
            else if (number == "004")
            {
                // Mystery dash
            }
            else if (number == "353")
            {
                // RPL_NAMREPLY
                // Reply: Name reply
            }
            else if (number == "366")
            {
                // RPL_ENDOFNAMES
                // Reply: End of /NAMES list
            }
            else if (number == "372")
            {
                // RPL_MOTD
                // Reply: Message of the day message
            }
            else if (number == "375")
            {
                // RPL_MOTDSTART
                // Reply: Message of the day start
            }
            else if (number == "376")
            {
                // RPL_ENDOFMOTD
                // Reply: End of message of the day
                LoggedInState = LoggedInStates.LoggedIn;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Reader.Dispose();
                    Writer.Dispose();
                }
                disposed = true;
            }
        }

        ~ClientBase()
        {
            Dispose(false);
        }
    }
}
