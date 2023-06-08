using NPokerEngine.Engine;
using NPokerEngine.Messages;
using NPokerEngine.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Players
{
    public class FoldPlayer : PokerPlayer
    {
        public FoldPlayer(string name, Guid uuid = default) : base(name, uuid)
        {
        }

        public override Tuple<ActionType, int> DeclareAction(IEnumerable validActions, HoleCards holeCards, object roundState)
        {
            return new Tuple<ActionType, int>(ActionType.FOLD, 0);
        }

        public override void ReceiveGameStartMessage(GameStartMessage gameStartMessage)
        {

        }

        public override void ReceiveGameUpdateMessage(GameUpdateMessage gameUpdateMessage)
        {

        }

        public override void ReceiveRoundResultMessage(RoundResultMessage roundResultMessage)
        {

        }

        public override void ReceiveRoundStartMessage(RoundStartMessage roundStartMessage)
        {

        }

        public override void ReceiveStreetStartMessage(StreetStartMessage streetStartMessage)
        {

        }
    }
}
