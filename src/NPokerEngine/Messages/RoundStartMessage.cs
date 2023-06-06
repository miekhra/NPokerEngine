using NPokerEngine.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Messages
{
    public class RoundStartMessage : IMessage
    {
        public MessageType MessageType => MessageType.ROUND_START_MESSAGE;
        public int RoundCount { get; set; }
        public string PlayerUuid { get; set; }
        public List<Card> HoleCards { get; set; }
        public List<Player> Players { get; set; }
    }
}
