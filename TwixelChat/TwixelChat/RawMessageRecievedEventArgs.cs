using System;

namespace TwixelChat
{
    public class RawMessageRecievedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
