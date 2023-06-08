using NPokerEngine.Types;

namespace NPokerEngine.Messages
{
    public class StreetStartMessage : IMessage
    {
        public MessageType MessageType => MessageType.STREET_START_MESSAGE;
        public GameState GameState { get; set; }
        public StreetType Street { get; set; }
    }
}
