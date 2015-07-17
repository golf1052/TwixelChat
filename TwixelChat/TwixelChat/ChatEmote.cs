using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    /// <summary>
    /// Class representing a chat emote
    /// </summary>
    public class ChatEmote
    {
        /// <summary>
        /// The chat emote ID
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// The list of locations the emote appears.
        /// The first long is the index of the first character.
        /// The second long is the index of the last character.
        /// </summary>
        public List<Tuple<long, long>> Positions { get; private set; }

        private ChatEmote(string emoteSection)
        {
            Positions = new List<Tuple<long, long>>();
            string[] split = emoteSection.Split(':');
            Id = long.Parse(split[0]);
            string[] positionSplit = split[1].Split(',');
            foreach (string position in positionSplit)
            {
                string[] positions = position.Split('-');
                Tuple<long, long> tuple = new Tuple<long, long>(long.Parse(positions[0]),
                    long.Parse(positions[1]));
                Positions.Add(tuple);
            }
        }

        /// <summary>
        /// Parse an emotes section
        /// </summary>
        /// <param name="section">The emotes section</param>
        /// <returns>A list of emotes</returns>
        public static List<ChatEmote> ParseEmotes(string section)
        {
            List<ChatEmote> emotes = new List<ChatEmote>();
            string[] emoteSections = section.Split('/');
            foreach (string emoteSection in emoteSections)
            {
                emotes.Add(new ChatEmote(emoteSection));
            }
            return emotes;
        }
    }
}
