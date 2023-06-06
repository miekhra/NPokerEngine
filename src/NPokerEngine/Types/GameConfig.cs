using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Types
{
    public class GameConfig
    {
        public float InitialStack { get; set; }
        public int MaxRound { get; set; }
        public float SmallBlindAmount { get; set; }
        public float Ante { get; set; }
        public Dictionary<int, float> BlindStructure { get; set; }
    }
}
