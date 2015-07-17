﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    /// <summary>
    /// Class representing a Twitch chat channel chat message.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Chat user, contains extra info about a username.
        /// This can be null.
        /// </summary>
        public ChatUser User { get; private set; }

        /// <summary>
        /// Username of the user who sent the message.
        /// Regular username. If User isn't null use DisplayName instead.
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// The list of emotes in the message
        /// </summary>
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
