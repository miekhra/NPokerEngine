using System.Collections.Generic;
using System.Linq;

namespace NPokerEngine.Types
{
    public class HoleCards
    {
        public static HoleCards FromSequence(IEnumerable<Card> cards)
        {
            return new HoleCards
            {
                FirstCard = cards.ElementAt(0),
                SecondCard = cards.ElementAt(1)
            };
        }
        public Card FirstCard { get; set; }
        public Card SecondCard { get; set; }
        public bool Empty => FirstCard == null || SecondCard == null;
    }
}
