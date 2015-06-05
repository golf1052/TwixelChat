using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    public class ChatUser
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
        public bool Subscriber { get; private set; }
        public bool Turbo { get; private set; }
        public UserTypes UserType { get; private set; }
        internal List<ChatEmote> Emotes { get; private set; }

        public ChatUser(string tagsSection)
        {
            Emotes = new List<ChatEmote>();
            string rest = tagsSection.Substring(1);
            string[] tags = rest.Split(';');
            foreach (string tag in tags)
            {
                string[] sections = tag.Split('=');
                if (sections[0] == "color")
                {
                    if (!string.IsNullOrEmpty(sections[1]))
                    {
                        Color = sections[1];
                    }
                    else
                    {
                        Color = null;
                    }
                }
                else if (sections[0] == "display-name")
                {
                    if (!string.IsNullOrEmpty(sections[1]))
                    {
                        DisplayName = sections[1].Replace("\\s", " ")
                            .Replace("\\\\", "\\")
                            .Replace("\\r", '\r'.ToString())
                            .Replace("\\n", '\n'.ToString());
                    }
                    else
                    {
                        DisplayName = null;
                    }
                }
                else if (sections[0] == "emotes")
                {
                    if (!string.IsNullOrEmpty(sections[1]))
                    {
                        Emotes = ChatEmote.ParseEmotes(sections[1]);
                    }
                }
                else if (sections[0] == "subscriber")
                {
                    int value = int.Parse(sections[1]);
                    Subscriber = value == 1;
                }
                else if (sections[0] == "turbo")
                {
                    int value = int.Parse(sections[1]);
                    Turbo = value == 1;
                }
                else if (sections[0] == "user-type")
                {
                    if (!string.IsNullOrEmpty(sections[1]))
                    {
                        if (sections[1] == "mod")
                        {
                            UserType = UserTypes.Mod;
                        }
                        else if (sections[1] == "global_mod")
                        {
                            UserType = UserTypes.GlobalMod;
                        }
                        else if (sections[1] == "admin")
                        {
                            UserType = UserTypes.Admin;
                        }
                        else if (sections[1] == "staff")
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
    }
}
