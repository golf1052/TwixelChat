using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace TwixelChat.Tests
{
    public class TestChatClient : ChatClientBase
    {
        Task readTask;
        CancellationTokenSource readTaskCancellationSource;
        CancellationToken readTaskCancellationToken;

        public string InputString { get; set; }
        public string OutputString { get; set; }

        public TestChatClient(string input) : base()
        {
            InputString = input;
        }

        public override async Task Connect(string name, string accessToken)
        {
            ConnectionState = ConnectionStates.Connected;
            Reader = new StreamReader(CreateStream(InputString, Encoding.UTF8), Encoding.UTF8);
            Writer = new StreamWriter(CreateStream(OutputString, Encoding.UTF8), Encoding.UTF8);
            InternalConnect();

            readTaskCancellationSource = new CancellationTokenSource();
            readTaskCancellationToken = readTaskCancellationSource.Token;
            readTask = Task.Run(() => ReadFromStream(readTaskCancellationToken), readTaskCancellationToken);
        }

        public override void Disconnect()
        {
            readTaskCancellationSource.Cancel();
            Task.WaitAny(new Task[] { readTask }, TimeSpan.FromMilliseconds(500));
            LoggedInState = LoggedInStates.LoggedOut;
            ConnectionState = ConnectionStates.Disconnected;
            Reader.Dispose();
            Writer.Dispose();
            InternalDisconnect();
            Name = null;
        }

        private MemoryStream CreateStream(string s, Encoding encoding)
        {
            return new MemoryStream(encoding.GetBytes(s ?? string.Empty));
        }
    }
}
