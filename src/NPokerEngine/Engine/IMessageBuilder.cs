//using System;
//using System.Collections.Generic;
//using System.Text;
//using NPokerEngine.Messages;
//using NPokerEngine.Types;

//namespace NPokerEngine.Engine
//{
//    public interface IMessageBuilder
//    {
//        RoundStartMessage BuildRoundStartMessage(int roundCount, int playerPos, Seats seats);
//        StreetStartMessage BuildStreetStartMessage(GameState state);
//        AskMessage BuildAskMessage(int playerPos, GameState state);
//        GameUpdateMessage BuildGameUpdateMessage(int playerPos, ActionType action, float amount, GameState state);
//        RoundResultMessage BuildRoundResultMessage(int round_count, IEnumerable<Player> winners, object hand_info, GameState state);
//    }
//}
