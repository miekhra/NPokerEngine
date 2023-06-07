using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Messages
{
    public interface IPlayerMessage : IMessage
    {
        public string PlayerUuid { get; set; }
    }
}
