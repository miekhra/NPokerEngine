using System;
using System.Collections.Generic;
using System.Text;
using NPokerEngine.Types;

namespace NPokerEngine.Engine
{
    public interface IMessageBuilder
    {
        Dictionary<string, object> BuildRoundStartMessage(int roundCount, int playerPos, Seats seats);
        Dictionary<string, object> BuildStreetStartMessage(Dictionary<string, object> state);
        Dictionary<string, object> BuildAskMessage(int playerPos, Dictionary<string, object> state);
        Dictionary<string, object> BuildGameUpdateMessage(int playerPos, object action, object amount, Dictionary<string, object> state);
        Dictionary<string, object> BuildRoundResultMessage(object round_count, IEnumerable<Player> winners, object hand_info, Dictionary<string, object> state);
    }
}
