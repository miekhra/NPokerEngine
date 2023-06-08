using NPokerEngine.Types;

namespace NPokerEngine.Messages
{
    public class GameResultMessage : IMessage
    {
        public MessageType MessageType => MessageType.GAME_RESULT_MESSAGE;
        public GameConfig Config { get; set; }
        public Seats Seats { get; set; }
    }
}
