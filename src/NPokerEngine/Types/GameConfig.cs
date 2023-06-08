using System.Collections.Generic;

namespace NPokerEngine.Types
{
    public class GameConfig
    {
        public float InitialStack { get; set; }
        public int MaxRound { get; set; }
        public float SmallBlindAmount { get; set; }
        public float Ante { get; set; }
        public Dictionary<object, object> BlindStructure { get; set; }
    }
}
