using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TwixelChat.Tests
{
    public class RateLimiterTests
    {
        [Fact]
        public async void RateLimiterTest()
        {
            TestChatClient client = new TestChatClient(string.Empty);
            await client.Connect("twixeltest", "");
            Stopwatch timer = new Stopwatch();
            for (int i = 0; i < 10; i++)
            {
                await client.SendRawMessage("test");
            }
            Debug.WriteLine("waiting 30 seconds");
            await Task.Delay(TimeSpan.FromSeconds(30));
            Debug.WriteLine("starting again");
            timer.Start();
            // long test
            for (int i = 0; i < 100; i++)
            {
                await client.SendRawMessage("test");
                Debug.WriteLine("sent message #" + (i + 1).ToString());
            }
            timer.Stop();
            Assert.True(timer.Elapsed > TimeSpan.FromSeconds(1));
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(180));
        }
    }
}
