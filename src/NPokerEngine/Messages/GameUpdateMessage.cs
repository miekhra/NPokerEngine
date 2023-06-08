using NPokerEngine.Types;

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
