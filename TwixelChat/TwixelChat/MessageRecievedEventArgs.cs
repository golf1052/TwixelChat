using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelChat
{
    public class MessageRecievedEventArgs : RawMessageRecievedEventArgs
    {
        public string Username { get; set; }
    }
}
