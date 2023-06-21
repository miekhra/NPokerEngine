using System;
using System.Collections.Generic;

namespace NPokerEngine.Types
{
    public class GameState : ICloneable
    {
        public int RoundCount { get; set; }
        public float SmallBlindAmount { get; set; }
        public StreetType Street { get; set; }
        public int NextPlayerIx { get; set; } = -1;
        public Table Table { get; set; }

        public object Clone()
        {
            return new GameState
            {
                RoundCount = RoundCount,
                SmallBlindAmount = SmallBlindAmount,
                Street = Street,
                NextPlayerIx = NextPlayerIx,
                Table = (Table)Table.Clone()
            };
        }

    }
}
