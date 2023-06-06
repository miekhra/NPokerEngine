using NPokerEngine.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Messages
{
    public class RoundResultMessage : IMessage
    {
        public int RoundCount { get; set; }
        public GameState State { get; set; }
        public List<Player> Winners { get; set; }

        public MessageType MessageType => MessageType.ROUND_RESULT_MESSAGE;
    }
}
