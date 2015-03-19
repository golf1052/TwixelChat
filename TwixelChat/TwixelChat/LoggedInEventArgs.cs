using System;

namespace TwixelChat
{
    public class LoggedInEventArgs : EventArgs
    {
        public ClientBase.LoggedInStates State { get; set; }
    }
}
