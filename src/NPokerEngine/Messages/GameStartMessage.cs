using NPokerEngine.Types;

namespace NPokerEngine.Messages
{
    public class GameStartMessage : IMessage
    {
        public MessageType MessageType => MessageType.GAME_START_MESSAGE;
        public GameConfig Config { get; set; }
        public Seats Seats { get; set; }
    }
}
