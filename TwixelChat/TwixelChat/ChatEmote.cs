using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    public class ChatEmote
    {
        public long Id { get; private set; }
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
