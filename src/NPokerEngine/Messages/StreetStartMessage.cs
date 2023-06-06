using NPokerEngine.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Messages
{
    public class StreetStartMessage : IMessage
    {
        public MessageType MessageType => MessageType.STREET_START_MESSAGE;
        public GameState GameState { get; set; }
        public StreetType Street { get; set; }
    }
}
