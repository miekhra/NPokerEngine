using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Types
{
    public class HoleCards
    {
        public Card FirstCard { get; set; }
        public Card SecondCard { get; set; }
        public bool Empty => FirstCard == null || SecondCard == null;
    }
}
