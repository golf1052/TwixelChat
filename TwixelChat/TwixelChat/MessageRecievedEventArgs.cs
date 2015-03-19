using System;

namespace TwixelChat
{
    public class MessageRecievedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
