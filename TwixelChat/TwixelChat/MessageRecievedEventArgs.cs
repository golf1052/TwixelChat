using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    public delegate void MessageHandler(object source, MessageRecievedEventArgs e);

    public class MessageRecievedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
