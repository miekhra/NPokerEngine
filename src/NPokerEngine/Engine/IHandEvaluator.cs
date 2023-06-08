using NPokerEngine.Types;
using System.Collections.Generic;

namespace NPokerEngine.Engine
{
    public interface IHandEvaluator
    {
        int EvalHand(IEnumerable<Card> hole, IEnumerable<Card> community);
        HandRankInfo GenHandRankInfo(IEnumerable<Card> hole, IEnumerable<Card> community);
    }
}
