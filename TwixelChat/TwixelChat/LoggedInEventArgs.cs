using System;

namespace TwixelChat
{
    public class LoggedInEventArgs : EventArgs
    {
        public ChatClientBase.LoggedInStates State { get; set; }
    }
}
