using System;

namespace TwixelChat.Events
{
    public class LoggedInEventArgs : EventArgs
    {
        public ChatClientBase.LoggedInStates State { get; set; }
    }
}
