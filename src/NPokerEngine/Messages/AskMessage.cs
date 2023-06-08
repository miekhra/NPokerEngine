using NPokerEngine.Types;
using System;
using System.Collections.Generic;

namespace NPokerEngine.Messages
{
    public class AskMessage : IPlayerMessage
    {
        public MessageType MessageType => MessageType.ASK_MESSAGE;
        public string PlayerUuid { get; set; }
        public Dictionary<ActionType, Tuple<float, float?>> ValidActions { get; set; }
        public GameState State { get; set; }
    }
}
