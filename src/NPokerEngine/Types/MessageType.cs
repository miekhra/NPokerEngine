using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Linq;

namespace NPokerEngine.Types
{
    public enum MessageType
    {
        GAME_START_MESSAGE,
        ROUND_START_MESSAGE,
        STREET_START_MESSAGE,
        ASK_MESSAGE,
        GAME_UPDATE_MESSAGE,
        ROUND_RESULT_MESSAGE,
        GAME_RESULT_MESSAGE
    }
}
