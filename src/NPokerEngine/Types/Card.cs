using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NPokerEngine.Types
{
    public class Card : ICloneable, IEquatable<Card>
    {
        public const byte CLUB = 2;
        public const byte DIAMOND = 4;
        public const byte HEART = 8;
        public const byte SPADE = 16;

        public static Dictionary<byte, string> SUIT_MAP = new Dictionary<byte, string> 
        {
            { CLUB, "C"},
            { DIAMOND, "D"},
            { HEART, "H"},
            { SPADE, "S"}
        };

        public static Dictionary<byte, string> RANK_MAP = new Dictionary<byte, string> 
        {
            { 2, "2"},
            { 3, "3"},
            { 4, "4"},
            { 5, "5"},
            { 6, "6"},
            { 7, "7"},
            { 8, "8"},
            { 9, "9"},
            { 10, "T"},
            { 11, "J"},
            { 12, "Q"},
            { 13, "K"},
            { 14, "A"}
        };

        private byte _suit, _rank;
        public byte Suit => _suit;
        public byte Rank => _rank;

        public Card(byte suit, byte rank)
        {
            _suit = suit;
            _rank = rank == (byte)1 ? (byte)14 : rank;
        }

        public int ToId()
        {
            var rank = this._rank == 14 ? 1 : this._rank;
            var num = 0;
            var tmp = this._suit >> 1;
            while ((tmp & 1) != 1)
            {
                num += 1;
                tmp >>= 1;
            }
            return rank + 13 * num;
        }

        public static Card FromId(int card_id)
        {
            var suit = 2;
            var rank = card_id;
            while (rank > 13)
            {
                suit <<= 1;
                rank -= 13;
            }
            return new Card((byte)suit, (byte)rank);
        }

        public static Card FromString(string str_card)
        {
            Debug.Assert(str_card.Length == 2);
            return new Card(
                suit: SUIT_MAP.First(t => string.Equals(t.Value, str_card[0].ToString(), StringComparison.OrdinalIgnoreCase)).Key,
                rank: RANK_MAP.First(t => string.Equals(t.Value, str_card[1].ToString(), StringComparison.OrdinalIgnoreCase)).Key
                );
        }

        public bool Equals(Card other)
        {
            return this._suit == other._suit && this._rank == other._rank;
        }

        public override string ToString()
        {
            return $"{SUIT_MAP[this._suit]}{RANK_MAP[this._rank]}";
        }

        public object Clone()
        {
            return Card.FromId(this.ToId());
        }
    }
}
