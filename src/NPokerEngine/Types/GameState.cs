using System;
using System.Collections.Generic;
using System.Text;

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

        public Dictionary<string, object> ToDictionary()
            => new Dictionary<string, object>
            {
                {
                    "round_count",
                    this.RoundCount
                },
                {
                    "small_blind_amount",
                    this.SmallBlindAmount
                },
                {
                    "street",
                    this.Street},
                {
                    "next_player",
                    this.NextPlayerIx},
                {
                    "table",
                    this.Table
                }
            };

    }
}
