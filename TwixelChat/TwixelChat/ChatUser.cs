using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    /// <summary>
    /// Class representing the user info for a chat message
    /// </summary>
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
