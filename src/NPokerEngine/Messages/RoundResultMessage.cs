using NPokerEngine.Types;
using System.Collections.Generic;

namespace NPokerEngine.Messages
{
    public class RoundResultMessage : IMessage
    {
        public int RoundCount { get; set; }
        public GameState State { get; set; }
        public List<Player> Winners { get; set; }
        public Dictionary<int, float> PrizeMap { get; set; }

        public MessageType MessageType => MessageType.ROUND_RESULT_MESSAGE;
    }
}
