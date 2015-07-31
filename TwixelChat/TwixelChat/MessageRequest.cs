using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TwixelChat
{
    internal class MessageRequest
    {
        internal int availableMessages;
        internal DateTime? lastRequest;
        internal SemaphoreSlim semaphore;

        internal MessageRequest(int messagesPer30Seconds)
        {
            availableMessages = messagesPer30Seconds;
            semaphore = new SemaphoreSlim(1);
        }
    }
}
