using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwixelChat.Constants;

namespace TwixelChat.Full
{
    public class ChatClient : ChatClientBase
    {
        TcpClient client;
        Task readTask;
        CancellationTokenSource readTaskCancellationSource;
        CancellationToken readTaskCancellationToken;

        public ChatClient() : base()
        {
        }

        public override async Task Connect(string name, string accessToken)
        {
            client = new TcpClient(TwitchChatConstants.TwitchServer, TwitchChatConstants.TwitchPort);
            ConnectionState = ConnectionStates.Connected;
            NetworkStream stream = client.GetStream();
            UTF8Encoding utf8WithoutBOM = new UTF8Encoding(false);
            Reader = new StreamReader(stream, Encoding.UTF8);
            Writer = new StreamWriter(stream, utf8WithoutBOM);
            Writer.AutoFlush = true;

            readTaskCancellationSource = new CancellationTokenSource();
            readTaskCancellationToken = readTaskCancellationSource.Token;
            readTask = Task.Factory.StartNew(() => ReadFromStream(readTaskCancellationToken), readTaskCancellationToken);
            await Login(name, accessToken);
        }

        public override void Disconnect()
        {
            readTaskCancellationSource.Cancel();
            Task.WaitAny(new Task[]{readTask}, TimeSpan.FromMilliseconds(500));
            client.Close();
            LoggedInState = LoggedInStates.LoggedOut;
            ConnectionState = ConnectionStates.Disconnected;
            client = null;
        }
    }
}
