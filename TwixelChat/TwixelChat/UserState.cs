using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    public class UserState
    {
        public enum UserTypes
        {
            None,
            Mod,
            GlobalMod,
            Admin,
            Staff
        }

        public string Color { get; private set; }
        public string DisplayName { get; private set; }
        public List<long> EmoteSets { get; private set; }
        public bool Subscriber { get; private set; }
        public bool Turbo { get; private set; }
        public UserTypes UserType { get; private set; }

        protected Dictionary<string, string> Tags { get; private set; }

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

            Subscriber = int.Parse(Tags["subscriber"]) == 1;

            Turbo = int.Parse(Tags["turbo"]) == 1;

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
