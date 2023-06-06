using NPokerEngine.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Messages
{
    public class GameUpdateMessage : IMessage
    {
        public MessageType MessageType => MessageType.GAME_UPDATE_MESSAGE;
        public string PlayerUuid { get; set; }
        public ActionType Action { get; set; }
        public float Amount { get; set; }
        public Seats Seats { get; set; }
    }
}
