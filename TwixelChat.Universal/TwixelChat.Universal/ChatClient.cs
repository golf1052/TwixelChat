using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using System.IO;
using TwixelChat.Constants;
using System.Threading;

namespace TwixelChat.Universal
{
    public class ChatClient : ChatClientBase
    {
        private StreamSocket client;
        Task readTask;
        CancellationTokenSource readTaskCancellationSource;
        CancellationToken readTaskCancellationToken;
        
        public ChatClient() : base()
        {
        }

        public async override Task Connect(string name, string accessToken)
        {
            ConnectionState = ConnectionStates.Connecting;
            client = new StreamSocket();
            try
            {
                await client.ConnectAsync(new HostName(TwitchChatConstants.TwitchServer), TwitchChatConstants.TwitchPort.ToString());
            }
            catch (Exception ex)
            {
                throw;
            }
            ConnectionState = ConnectionStates.Connected;
            Reader = new StreamReader(client.InputStream.AsStreamForRead(), Encoding.UTF8);
            Writer = new StreamWriter(client.OutputStream.AsStreamForWrite());
            Writer.AutoFlush = true;

            readTaskCancellationSource = new CancellationTokenSource();
            readTaskCancellationToken = readTaskCancellationSource.Token;
            readTask = Task.Factory.StartNew(() => ReadFromStream(readTaskCancellationToken));
            await Login(name, accessToken);
        }

        public override void Disconnect()
        {
            readTaskCancellationSource.Cancel();
            Task.WaitAll(new Task[] { readTask }, TimeSpan.FromMilliseconds(500));
            client.Dispose();
            LoggedInState = LoggedInStates.LoggedOut;
            ConnectionState = ConnectionStates.Disconnected;
        }
    }
}
