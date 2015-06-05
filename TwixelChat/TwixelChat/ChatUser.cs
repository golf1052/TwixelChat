using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    public class ChatUser : UserState
    {
        internal List<ChatEmote> Emotes { get; private set; }

        public ChatUser(string tagsSection) : base(tagsSection, false)
        {
            Emotes = new List<ChatEmote>();

            if (!string.IsNullOrEmpty(Tags["emotes"]))
            {
                Emotes = ChatEmote.ParseEmotes(Tags["emotes"]);
            }
        }
    }
}
