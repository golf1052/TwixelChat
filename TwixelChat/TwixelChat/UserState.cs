using System.Collections.Generic;
using TwixelChat.Helpers;

namespace TwixelChat
{
    /// <summary>
    /// Class representing a Twitch chat user state
    /// </summary>
    public class UserState
    {
        /// <summary>
        /// Twitch user types
        /// </summary>
        public enum UserTypes
        {
            /// <summary>
            /// Regular user
            /// </summary>
            None,
            /// <summary>
            /// Channel mod
            /// </summary>
            Mod,
            /// <summary>
            /// Global mod
            /// </summary>
            GlobalMod,
            /// <summary>
            /// Twitch admin
            /// </summary>
            Admin,
            /// <summary>
            /// Twitch staff
            /// </summary>
            Staff
        }

        /// <summary>
        /// Username color.
        /// Is null if user has not specified a color.
        /// </summary>
        public string Color { get; private set; }

        /// <summary>
        /// Chat display name.
        /// Is null if user has not specified a display name.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Emote sets available to this user
        /// </summary>
        public List<long> EmoteSets { get; private set; }

        /// <summary>
        /// Is the user a subscriber to this channel
        /// </summary>
        public bool Subscriber { get; private set; }

        /// <summary>
        /// Is the user a Turbo member
        /// </summary>
        public bool Turbo { get; private set; }

        /// <summary>
        /// User type
        /// </summary>
        public UserTypes UserType { get; private set; }

        protected Dictionary<string, string> Tags { get; private set; }

        /// <summary>
        /// Create a new UserState
        /// </summary>
        /// <param name="tagsSection">The tags section of the message</param>
        /// <param name="userState">
        /// True if this is a pure UserState.
        /// False if this is a ChatUser.
        /// </param>
        public UserState(string tagsSection, bool userState)
        {
            EmoteSets = null;

            Tags = HelperMethods.GetTags(tagsSection);

            if (!string.IsNullOrEmpty(Tags["color"]))
            {
                Color = Tags["color"];
            }
            else
            {
                Color = null;
            }

            if (!string.IsNullOrEmpty(Tags["display-name"]))
            {
                DisplayName = Tags["display-name"].Replace("\\s", " ")
                    .Replace("\\\\", "\\")
                    .Replace("\\r", '\r'.ToString())
                    .Replace("\\n", '\n'.ToString());
            }
            else
            {
                DisplayName = null;
            }

            // ChatUsers don't have emote sets
            if (userState)
            {
                if (!string.IsNullOrEmpty(Tags["emote-sets"]))
                {
                    EmoteSets = new List<long>();
                    string[] emotes = Tags["emote-sets"].Split(',');
                    foreach (string emote in emotes)
                    {
                        EmoteSets.Add(long.Parse(emote));
                    }
                }
                else
                {
                    EmoteSets = new List<long>();
                }
            }

            if (Tags.ContainsKey("subscriber"))
            {
                Subscriber = int.Parse(Tags["subscriber"]) == 1;
            }
            else
            {
                Subscriber = false;
            }

            if (Tags.ContainsKey("turbo"))
            {
                Turbo = int.Parse(Tags["turbo"]) == 1;
            }
            else
            {
                Turbo = false;
            }
            
            if (Tags.ContainsKey("badges"))
            {
                string[] badges = Tags["badges"].Split(',');
                foreach (string badge in badges)
                {
                    if (badge.StartsWith("subscriber"))
                    {
                        Subscriber = true;
                    }
                    else if (badge.StartsWith("turbo"))
                    {
                        Turbo = true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(Tags["user-type"]))
            {
                if (Tags["user-type"] == "mod")
                {
                    UserType = UserTypes.Mod;
                }
                else if (Tags["user-type"] == "global_mod")
                {
                    UserType = UserTypes.GlobalMod;
                }
                else if (Tags["user-type"] == "admin")
                {
                    UserType = UserTypes.Admin;
                }
                else if (Tags["user-type"] == "staff")
                {
                    UserType = UserTypes.Staff;
                }
            }
            else
            {
                UserType = UserTypes.None;
            }
        }
    }
}
