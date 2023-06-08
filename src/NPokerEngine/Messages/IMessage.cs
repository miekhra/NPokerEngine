using NPokerEngine.Types;

namespace NPokerEngine.Messages
{
    public interface IMessage
    {
        public MessageType MessageType { get; }
    }
}
