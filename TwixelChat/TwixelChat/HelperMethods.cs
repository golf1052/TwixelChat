using System.Collections.Generic;

namespace TwixelChat.Helpers
{
    /// <summary>
    /// Contains a collection of helpful methods.
    /// </summary>
    public class HelperMethods
    {
        /// <summary>
        /// Gets a dictionary of tags from a tags section
        /// </summary>
        /// <param name="tagsSection">The tags section</param>
        /// <returns>A dictionary of tags</returns>
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
