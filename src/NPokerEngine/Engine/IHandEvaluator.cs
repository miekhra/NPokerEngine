using System;
using System.Collections.Generic;
using System.Text;
using NPokerEngine.Types;

namespace NPokerEngine.Engine
{
    public interface IHandEvaluator
    {
        int EvalHand(IEnumerable<Card> hole, IEnumerable<Card> community);
        HandRankInfo GenHandRankInfo(IEnumerable<Card> hole, IEnumerable<Card> community);
    }
}
