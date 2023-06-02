using System;
using System.Collections.Generic;
using System.Text;
using NPokerEngine.Types;

namespace NPokerEngine.Messages
{
    public interface IMessage
    {
        public MessageType MessageType { get; }
    }
}
