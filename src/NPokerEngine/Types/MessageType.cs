using NPokerEngine.Engine;
using System.ComponentModel;

namespace NPokerEngine.Types
{
    public enum MessageType
    {
        [Category(MessageBuilder.NOTIFICATION)] GAME_START_MESSAGE,
        [Category(MessageBuilder.NOTIFICATION)] ROUND_START_MESSAGE,
        [Category(MessageBuilder.NOTIFICATION)] STREET_START_MESSAGE,
        [Category(MessageBuilder.ASK)] ASK_MESSAGE,
        [Category(MessageBuilder.NOTIFICATION)] GAME_UPDATE_MESSAGE,
        [Category(MessageBuilder.NOTIFICATION)] ROUND_RESULT_MESSAGE,
        [Category(MessageBuilder.NOTIFICATION)] GAME_RESULT_MESSAGE
    }
}
