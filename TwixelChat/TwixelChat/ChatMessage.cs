using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    public class ChatMessage
    {
        public ChatUser User { get; private set; }
        public string Username { get; private set; }
        public string Message { get; private set; }
        public List<ChatEmote> Emotes { get; private set; }

        public ChatMessage(string rawServerMessage)
        {
            Emotes = null;
            if (rawServerMessage.StartsWith("@"))
            {
                // contains tags
                Emotes = new List<ChatEmote>();
                int splitIndex = rawServerMessage.IndexOf(' ');
                
                string tagsSection = rawServerMessage.Substring(0, splitIndex);
                User = new ChatUser(tagsSection);
                Emotes = User.Emotes;
                string rest = rawServerMessage.Substring(splitIndex + 1);
                ParseMessage(rest);
            }
            else
            {
                // doesn't contain tags
                User = null;
                ParseMessage(rawServerMessage);
            }
        }

        private void ParseMessage(string message)
        {
            string rest = message.Substring(1);
            Username = rest.Split('!')[0];
            Message = rest.Substring(rest.IndexOf(':') + 1);
        }
    }
}
