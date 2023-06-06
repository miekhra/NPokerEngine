using NPokerEngine.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Messages
{
    public class GameResultMessage : IMessage
    {
        public MessageType MessageType => MessageType.GAME_RESULT_MESSAGE;
        public GameConfig Config { get; set; }
        public Seats Seats { get; set; }
    }
}
