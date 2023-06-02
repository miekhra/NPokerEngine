using NPokerEngine.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Messages
{
    public class AskMessage : IMessage
    {
        public MessageType MessageType => MessageType.ASK_MESSAGE;
        public string PlayerUuid { get; set; }
        public GameState State { get; set; }
        public Dictionary<ActionType, Tuple<int, int?>> ValidActions { get; set; }
    }
}
