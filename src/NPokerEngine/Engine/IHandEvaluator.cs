using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Engine
{
    public interface IHandEvaluator
    {
        int EvalHand(IEnumerable<Card> hole, IEnumerable<Card> community);
        Dictionary<string, object> GenHandRankInfo(IEnumerable<Card> hole, IEnumerable<Card> community);
    }
}
