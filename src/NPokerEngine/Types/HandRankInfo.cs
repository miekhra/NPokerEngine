using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Types
{
    public class HandRankInfo
    {
        public string HandStrength { get; set; }
        public int HandHigh { get; set; }
        public int HandLow { get; set; }
        public int HoleHigh { get; set; }
        public int HoleLow { get; set; }
    }
}
