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
    public class Client : ClientBase
    {
        TcpClient client;
        Task readTask;
        CancellationTokenSource readTaskCancellationSource;
        CancellationToken readTaskCancellationToken;

        public Client() : base()
        {
        }

        public override async Task Connect(string name, string accessToken, long timeOutTime = 0)
        {
            client = new TcpClient(TwitchChatConstants.TwitchServer, TwitchChatConstants.TwitchPort);
            ConnectionState = ConnectionStates.Connected;
            NetworkStream stream = client.GetStream();
            Reader = new StreamReader(stream, Encoding.UTF8);
            Writer = new StreamWriter(stream, Encoding.ASCII);
            Writer.AutoFlush = true;

            readTaskCancellationSource = new CancellationTokenSource();
            readTaskCancellationToken = readTaskCancellationSource.Token;
            readTask = Task.Factory.StartNew(() => ReadFromStream(readTaskCancellationToken), readTaskCancellationToken);
            await Login(name, accessToken, timeOutTime);
        }

        public override void Disconnect()
        {
            readTaskCancellationSource.Cancel();
            Task.WaitAny(new Task[]{readTask}, TimeSpan.FromMilliseconds(500));
            client.Close();
            LoggedInState = LoggedInStates.LoggedOut;
            ConnectionState = ConnectionStates.Disconnected;
        }
    }
}
