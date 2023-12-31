﻿using NPokerEngine.Types;
using System.Collections.Generic;

namespace NPokerEngine.Messages
{
    public class RoundStartMessage : IPlayerMessage
    {
        public MessageType MessageType => MessageType.ROUND_START_MESSAGE;
        public int RoundCount { get; set; }
        public string PlayerUuid { get; set; }
        public List<Card> HoleCards { get; set; }
        public List<Player> Players { get; set; }
    }
}
