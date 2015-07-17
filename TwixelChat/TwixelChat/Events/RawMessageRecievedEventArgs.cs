using System;

namespace TwixelChat.Events
{
    public class RawMessageRecievedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
