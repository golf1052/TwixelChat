namespace TwixelChat.Events
{
    public class MessageRecievedEventArgs : RawMessageRecievedEventArgs
    {
        public ChatMessage ChatMessage { get; set; }
    }
}
