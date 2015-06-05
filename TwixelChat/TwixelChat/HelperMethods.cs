using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    public class HelperMethods
    {
        public static Dictionary<string, string> GetTags(string tagsSection)
        {
            if (tagsSection.StartsWith("@"))
            {
                tagsSection = tagsSection.Substring(1);
            }
            string[] tags = tagsSection.Split(';');
            Dictionary<string, string> Tags = new Dictionary<string, string>();
            foreach (string tag in tags)
            {
                string[] sections = tag.Split('=');
                Tags.Add(sections[0], sections[1]);
            }
            return Tags;
        }
    }
}
