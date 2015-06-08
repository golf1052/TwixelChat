using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TwixelChat.Tests
{
    public class ChatClientTests
    {
        TestChatClient chatClient;

        private string NewLine
        {
            get
            {
                return '\r'.ToString() + '\n'.ToString();
            }
        }

        [Fact]
        public void InitialServerMessagesTest()
        {
            string message = ":tmi.twitch.tv 001 golf1052 :Welcome, GLHF!" + NewLine +
                ":tmi.twitch.tv 002 golf1052 :Your host is tmi.twitch.tv" + NewLine +
                ":tmi.twitch.tv 003 golf1052 :This server is rather new" + NewLine +
                ":tmi.twitch.tv 004 golf1052 :-" + NewLine +
                ":tmi.twitch.tv 375 golf1052 :-" + NewLine +
                ":tmi.twitch.tv 372 golf1052 :You are in a maze of twisty passages, all alike." + NewLine +
                ":tmi.twitch.tv 376 golf1052 :>" + NewLine;
            chatClient = new TestChatClient(message);
            chatClient.Connect("twixeltest", "");
            Assert.True(true);
        }

        [Fact]
        public async void MessageTagsTest()
        {
            string message = "@color=#1E0ACC;display-name=3ventic;emotes=25:0-4,12-16/1902:6-10;subscriber=0;turbo=1;user-type=mod;user_type=mod :3ventic!3ventic@3ventic.tmi.twitch.tv PRIVMSG #gophergaming :Kappa Keepo Kappa";
            chatClient = new TestChatClient(message);
            ChatMessage chatMessage = null;
            chatClient.MessageRecieved += (o, e) => chatMessage = e.ChatMessage;
            await chatClient.Connect("twixeltest", "");
            await Task.Delay(100);
            Assert.NotNull(chatMessage);
            Assert.NotNull(chatMessage.User);
            Assert.Equal("#1E0ACC", chatMessage.User.Color);
            Assert.Equal("3ventic", chatMessage.User.DisplayName);
            Assert.Equal("3ventic", chatMessage.Username);
            Assert.False(chatMessage.User.Subscriber);
            Assert.True(chatMessage.User.Turbo);
            Assert.Equal<UserState.UserTypes>(UserState.UserTypes.Mod, chatMessage.User.UserType);
            Assert.Equal("Kappa Keepo Kappa", chatMessage.Message);

            List<ChatEmote> emotes = chatMessage.Emotes;
            Assert.NotNull(emotes);
            Assert.True(emotes.Count > 0);
            Assert.True(emotes.Count == 2);
            for (int i = 0; i < emotes.Count; i++)
            {
                ChatEmote emote = emotes[i];
                if (i == 0)
                {
                    Assert.True(25 == emote.Id);
                    Assert.True(emote.Positions.Count == 2);
                    Assert.True(emote.Positions[0].Item1 == 0);
                    Assert.True(emote.Positions[0].Item2 == 4);
                    Assert.True(emote.Positions[1].Item1 == 12);
                    Assert.True(emote.Positions[1].Item2 == 16);
                }
                else if (i == 1)
                {
                    Assert.True(1902 == emote.Id);
                    Assert.True(emote.Positions.Count == 1);
                    Assert.True(emote.Positions[0].Item1 == 6);
                    Assert.True(emote.Positions[0].Item2 == 10);
                }
            }
        }
    }
}
