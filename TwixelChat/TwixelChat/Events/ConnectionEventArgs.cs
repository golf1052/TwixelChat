using System;

namespace TwixelChat.Events
{
    public class ConnectionEventArgs : EventArgs
    {
        public ChatClientBase.ConnectionStates State { get; set; }
    }
}
