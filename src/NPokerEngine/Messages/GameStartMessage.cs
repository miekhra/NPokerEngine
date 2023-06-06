using NPokerEngine.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Messages
{
    public class GameStartMessage : IMessage
    {
        public MessageType MessageType => MessageType.GAME_START_MESSAGE;
        public GameConfig Config { get; set; }
        public Seats Seats { get; set; }
    }
}
