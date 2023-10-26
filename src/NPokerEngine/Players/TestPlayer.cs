using NPokerEngine.Messages;
using NPokerEngine.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NPokerEngine.Players
{
    public class TestPlayer : PokerPlayer
    {
        private readonly List<Tuple<ActionType, int>> _testActions;

        public TestPlayer(string name, List<Tuple<ActionType, int>> testActions, Guid uuid = default) : base(name, uuid)
        {
            _testActions = testActions;
        }

        public override Tuple<ActionType, int> DeclareAction(IEnumerable validActions, HoleCards holeCards, object roundState)
        {
            var first = _testActions.First();
            Debug.WriteLine($"{Name}->{first.Item1}->{_testActions.Count}->{((GameState)roundState).RoundCount}");
            _testActions.RemoveAt(0);
            return first;
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
