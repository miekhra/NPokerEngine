using NPokerEngine.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NPokerEngine.Engine
{
    public class HandEvaluator : IHandEvaluator
    {
        private static HandEvaluator _instance;

        private HandEvaluator()
        {
        }

        public static HandEvaluator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HandEvaluator();
                }

                return _instance;
            }
        }

        internal Func<IEnumerable<Card>, IEnumerable<Card>, int> _evalFunc = null;
        public HandRankInfo GenHandRankInfo(IEnumerable<Card> hole, IEnumerable<Card> community)
        {
            var hand = _evalFunc == null ? this.EvalHand(hole, community) : _evalFunc(hole, community);
            var row_strength = this.MaskHandStrength(hand);
            var strength = (HandRankType)row_strength;
            var hand_high = this.MaskHandHighRank(hand);
            var hand_low = this.MaskHandLowRank(hand);
            var hole_high = this.MaskHoleHighRank(hand);
            var hole_low = this.MaskHoleLowRank(hand);
            return new HandRankInfo
            {
                HandStrength = strength,
                HandHigh = hand_high,
                HandLow = hand_low,
                HoleHigh = hole_high,
                HoleLow = hole_low
            };
        }

        public int EvalHand(IEnumerable<Card> hole, IEnumerable<Card> community)
        {
            var ranks = (from card in hole
                         select card.Rank).ToList().OrderBy(_p_1 => _p_1).ToList();
            var hole_flg = ranks[1] << 4 | ranks[0];
            var hand_flg = this.CalcHandInfoFlag(hole, community) << 8;
            return hand_flg | hole_flg;
        }

        // Return Format
        // [Bit flg of hand][rank1(4bit)][rank2(4bit)]
        // ex.)
        //       HighCard hole card 3,4   =>           100 0011
        //       OnePair of rank 3        =>        1 0011 0000
        //       TwoPair of rank A, 4     =>       10 1110 0100
        //       ThreeCard of rank 9      =>      100 1001 0000
        //       Straight of rank 10      =>     1000 1010 0000
        //       Flash of rank 5          =>    10000 0101 0000
        //       FullHouse of rank 3, 4   =>   100000 0011 0100
        //       FourCard of rank 2       =>  1000000 0010 0000
        //       straight flash of rank 7 => 10000000 0111 0000
        private int CalcHandInfoFlag(IEnumerable<Card> hole, IEnumerable<Card> community)
        {
            var cards = hole.Concat(community).ToList();
            if (this.TryEvalStraightFlash(cards, out var straightFlashResult))
            {
                return (int)HandRankType.STRAIGHTFLASH | straightFlashResult;
            }
            if (this.TryEvalFourCards(cards, out var fourCardsResult))
            {
                return (int)HandRankType.FOURCARD | fourCardsResult;
            }
            if (this.TryEvalFullHouse(cards, out var fullHouseResult))
            {
                return (int)HandRankType.FULLHOUSE | fullHouseResult;
            }
            if (this.TryEvalFlash(cards, out var flashResult))
            {
                return (int)HandRankType.FLASH | flashResult;
            }
            if (this.TryEvalStraight(cards, out var straightResult))
            {
                return (int)HandRankType.STRAIGHT | straightResult;
            }
            if (this.TryEvalThreeCards(cards, out var threeCardsResult))
            {
                return (int)HandRankType.THREECARD | threeCardsResult;
            }
            if (this.TryEvalTwoPairs(cards, out var twoPairsResult))
            {
                return (int)HandRankType.TWOPAIR | twoPairsResult;
            }
            if (this.TryEvalOnePair(cards, out var onePairResult))
            {
                return (int)HandRankType.ONEPAIR | onePairResult;
            }
            return EvalHoleCard(hole);
        }

        private bool TryEvalStraightFlash(IEnumerable<Card> cards, out int result)
        {
            result = SearchStraightFlash(cards);
            if (result == -1) return false;

            result = result << 4;
            return true;
        }

        private bool TryEvalFourCards(IEnumerable<Card> cards, out int result)
        {
            result = SearchFourCards(cards) << 4;
            return result != 0;
        }

        private bool TryEvalFullHouse(IEnumerable<Card> cards, out int result)
        {
            result = 0;
            var searchResult = SearchFullHouse(cards);
            if (searchResult.Item1 == 0 || searchResult.Item2 == 0) return false;

            result = searchResult.Item1 << 4 | searchResult.Item2;
            return true;
        }

        private bool TryEvalFlash(IEnumerable<Card> cards, out int result)
        {
            result = SearchFlash(cards);
            if (result == -1) return false;

            result = result << 4;
            return true;
        }

        private bool TryEvalStraight(IEnumerable<Card> cards, out int result)
        {
            result = SearchStraight(cards);
            if (result == -1) return false;

            result = result << 4;
            return true;
        }

        private bool TryEvalThreeCards(IEnumerable<Card> cards, out int result)
        {
            result = SearchThreeCards(cards);
            if (result == -1) return false;

            result = result << 4;
            return true;
        }

        private bool TryEvalTwoPairs(IEnumerable<Card> cards, out int result)
        {
            result = 0;
            var searchTwoPairs = SearchTwoPairs(cards);
            if (searchTwoPairs.Count != 2) return false;
            result = searchTwoPairs[0] << 4 | searchTwoPairs[1];
            return true;
        }

        private bool TryEvalOnePair(IEnumerable<Card> cards, out int result)
        {
            var rank = 0;
            long memo = 0;
            foreach (var card in cards)
            {
                var mask = (long)1 << card.Rank;
                if ((memo & mask) != 0)
                {
                    rank = Math.Max(rank, card.Rank);
                }
                memo |= mask;
            }
            result = rank << 4;
            return result != 0;
        }

        private int EvalHoleCard(IEnumerable<Card> hole)
        {
            var orderedRanks = hole.Select(t => t.Rank).OrderBy(t => t).ToList();
            return orderedRanks[1] << 4 | orderedRanks[0];
        }

        private int SearchStraight(IEnumerable<Card> cards)
        {
            long bitMemo = cards.Select(c => c.Rank).Aggregate((long)0, (memo, rank) => memo | (long)1 << rank);
            var rank = -1;
            foreach (var r in Enumerable.Range(2, 15 - 2))
            {
                if (Enumerable.Range(0, 5).Aggregate(1, (acc, i) => (acc & (bitMemo >> (r + i) & 1)) == 1 ? 1 : 0) >= 1)
                    rank = r;
            }
            return rank;
        }

        private int SearchFlash(IEnumerable<Card> cards)
        {
            var bestSuitRank = -1;
            Func<Card, int> fetchSuit = card => card.Suit;
            Func<Card, int> fetchRank = card => card.Rank;
            foreach (var suitGroup in cards.GroupBy(fetchSuit))
            {
                var suit = suitGroup.Key;
                var groupObj = suitGroup.ToList();
                if (groupObj.Count >= 5)
                {
                    var maxRankCard = groupObj.Max(fetchRank);
                    bestSuitRank = Math.Max(bestSuitRank, maxRankCard);
                }
            }
            return bestSuitRank;
        }

        private Tuple<int, int> SearchFullHouse(IEnumerable<Card> cards)
        {
            Func<Card, int> fetchRank = card => card.Rank;
            var three_card_ranks = new List<int>();
            var two_pair_ranks = new List<int>();
            foreach (var g in cards.OrderBy(fetchRank).GroupBy(fetchRank))
            {
                var rank = g.Key;
                var groupObj = g.ToList();
                if (groupObj.Count >= 3)
                {
                    three_card_ranks.Add(rank);
                }
                if (groupObj.Count >= 2)
                {
                    two_pair_ranks.Add(rank);
                }
            }
            two_pair_ranks = (from rank in two_pair_ranks
                              where !three_card_ranks.Contains(rank)
                              select rank).ToList();
            if (three_card_ranks.Count == 2)
            {
                two_pair_ranks.Add(three_card_ranks.Min());
            }
            Func<List<int>, int> max_ = l => l.Count == 0 ? 0 : l.Max();
            return Tuple.Create(max_(three_card_ranks), max_(two_pair_ranks));
        }

        private int SearchFourCards(IEnumerable<Card> cards)
        {
            Func<Card, int> fetchRank = card => card.Rank;
            foreach (var _tup_1 in cards.GroupBy(fetchRank))
            {
                var rank = _tup_1.Key;
                var groupObj = _tup_1.ToList();
                if (groupObj.Count >= 4)
                {
                    return rank;
                }
            }
            return 0;
        }

        private int SearchStraightFlash(IEnumerable<Card> cards)
        {
            var flashCards = new List<Card>();
            Func<Card, int> fetchSuit = card => card.Suit;
            foreach (var _tup_1 in cards.OrderBy(fetchSuit).GroupBy(fetchSuit))
            {
                var suit = _tup_1.Key;
                var groupObj = _tup_1.ToList();
                if (groupObj.Count >= 5)
                {
                    flashCards = groupObj;
                }
            }
            return this.SearchStraight(flashCards);
        }

        private int SearchThreeCards(IEnumerable<Card> cards)
        {
            var rank = -1;
            long bitMemo = 0;
            foreach (var card in cards)
            {
                bitMemo += ((long)1 << (card.Rank - 1) * 3);
            }
            //cards.Aggregate(0, (memo, card) => memo + (1 << (card.Rank - 1) * 3));
            foreach (var r in Enumerable.Range(2, 15 - 2))
            {
                bitMemo >>= 3;
                var count = bitMemo & 7;
                if (count >= 3)
                {
                    rank = r;
                }
            }
            return rank;
        }

        private List<int> SearchTwoPairs(IEnumerable<Card> cards)
        {
            var ranks = new List<int>();
            long memo = 0;
            foreach (var card in cards)
            {
                var mask = (long)1 << card.Rank;
                if ((memo & mask) != 0 && !ranks.Contains(card.Rank))
                {
                    ranks.Add(card.Rank);
                }
                memo |= mask;
            }
            ranks = ranks.OrderByDescending(_p_1 => _p_1).ToList();
            return (ranks.Count <= 2) ? ranks : ranks.Take(2).ToList();
        }

        internal int MaskHandStrength(int bit)
        {
            var mask = 511 << 16;
            return (bit & mask) >> 8;
        }

        internal int MaskHandHighRank(int bit)
        {
            var mask = 15 << 12;
            return (bit & mask) >> 12;
        }

        internal int MaskHandLowRank(int bit)
        {
            var mask = 15 << 8;
            return (bit & mask) >> 8;
        }

        internal int MaskHoleHighRank(int bit)
        {
            var mask = 15 << 4;
            return (bit & mask) >> 4;
        }

        internal int MaskHoleLowRank(int bit)
        {
            var mask = 15;
            return bit & mask;
        }
    }
}
