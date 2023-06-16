using FluentAssertions;
using FluentAssertions.Execution;

namespace NPokerEngine.Tests.Engine
{
    [TestClass]
    public class HandEvaluatorTests
    {
        [TestInitialize]
        public void Initialize()
        {
            HandEvaluatorResolver.Register(new CustomHandEvaluator());
        }

        [TestCleanup]
        public void Cleanup()
        {
            HandEvaluatorResolver.ResoreDefault();
        }

        [TestMethod]
        public void GenHandInfoTest()
        {
            var community = new List<Card>()
            {
                new Card(Card.CLUB, 3),
                new Card(Card.CLUB, 7),
                new Card(Card.CLUB, 10),
                new Card(Card.DIAMOND, 5),
                new Card(Card.DIAMOND, 6)
            };

            var hole = new List<Card>()
            {
                new Card(Card.CLUB, 9),
                new Card(Card.DIAMOND, 2)
            };

            var rankInfo = HandEvaluator.Instance.GenHandRankInfo(hole, community);
            var rankInfoCustom = HandEvaluatorResolver.Get().GenHandRankInfo(hole, community);

            using (new AssertionScope())
            {
                rankInfo.HandStrength.Should().Be(HandRankType.HIGHCARD);
                rankInfo.HandHigh.Should().Be(9);
                rankInfo.HandLow.Should().Be(2);
                rankInfo.HoleHigh.Should().Be(9);
                rankInfo.HoleLow.Should().Be(2);
                HandEvaluatorResolver.Get().Should().BeOfType<CustomHandEvaluator>();
                rankInfoCustom.HandStrength.Should().Be(HandRankType.HIGHCARD);
            }
        }

        [TestMethod]
        public void EvalHighCardTest()
        {
            var community = new List<Card>()
            {
                new Card(Card.CLUB, 3),
                new Card(Card.CLUB, 7),
                new Card(Card.CLUB, 10),
                new Card(Card.DIAMOND, 5),
                new Card(Card.DIAMOND, 6)
            };

            var hole = new List<Card>()
            {
                new Card(Card.CLUB, 9),
                new Card(Card.DIAMOND, 2)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);
            var rankInfoCustom = HandEvaluatorResolver.Get().GenHandRankInfo(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be((int)HandRankType.HIGHCARD);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(9);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(2);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(9);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(2);
                HandEvaluatorResolver.Get().Should().BeOfType<CustomHandEvaluator>();
                rankInfoCustom.HandStrength.Should().Be(HandRankType.HIGHCARD);
            }
        }

        [TestMethod]
        public void OnePairTest()
        {
            var community = new List<Card>()
            {
                new Card(Card.CLUB, 3),
                new Card(Card.CLUB, 7),
                new Card(Card.CLUB, 10),
                new Card(Card.DIAMOND, 5),
                new Card(Card.DIAMOND, 6)
            };

            var hole = new List<Card>()
            {
                new Card(Card.CLUB, 9),
                new Card(Card.DIAMOND, 3)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);
            var rankInfoCustom = HandEvaluatorResolver.Get().GenHandRankInfo(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be((int)HandRankType.ONEPAIR);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(3);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(0);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(9);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(3);
                HandEvaluatorResolver.Get().Should().BeOfType<CustomHandEvaluator>();
                rankInfoCustom.HandStrength.Should().Be(HandRankType.ONEPAIR);
            }
        }

        [TestMethod]
        public void TwoPairTest()
        {
            var community = new List<Card>()
            {
                new Card(Card.CLUB, 7),
                new Card(Card.SPADE, 9),
                new Card(Card.DIAMOND, 2),
                new Card(Card.DIAMOND, 3),
                new Card(Card.DIAMOND, 5)
            };

            var hole = new List<Card>()
            {
                new Card(Card.CLUB, 9),
                new Card(Card.DIAMOND, 3)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);
            var rankInfoCustom = HandEvaluatorResolver.Get().GenHandRankInfo(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be((int)HandRankType.TWOPAIR);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(9);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(3);
                HandEvaluatorResolver.Get().Should().BeOfType<CustomHandEvaluator>();
                rankInfoCustom.HandStrength.Should().Be(HandRankType.TWOPAIR);
            }
        }

        [TestMethod]
        public void TwoPair2Test()
        {
            var community = new List<Card>()
            {
                new Card(Card.DIAMOND, 4),
                new Card(Card.SPADE, 8),
                new Card(Card.HEART, 4),
                new Card(Card.DIAMOND, 7),
                new Card(Card.CLUB, 8)
            };

            var hole = new List<Card>()
            {
                new Card(Card.CLUB, 7),
                new Card(Card.SPADE, 5)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);
            var rankInfoCustom = HandEvaluatorResolver.Get().GenHandRankInfo(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be((int)HandRankType.TWOPAIR);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(8);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(7);
                HandEvaluatorResolver.Get().Should().BeOfType<CustomHandEvaluator>();
                rankInfoCustom.HandStrength.Should().Be(HandRankType.TWOPAIR);
            }
        }

        [TestMethod]
        public void ThreeCardTest()
        {
            var community = new List<Card>()
            {
                new Card(Card.CLUB, 3),
                new Card(Card.CLUB, 7),
                new Card(Card.SPADE, 3),
                new Card(Card.DIAMOND, 5),
                new Card(Card.DIAMOND, 6)
            };

            var hole = new List<Card>()
            {
                new Card(Card.CLUB, 9),
                new Card(Card.DIAMOND, 3)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);
            var rankInfoCustom = HandEvaluatorResolver.Get().GenHandRankInfo(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be((int)HandRankType.THREECARD);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(3);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(0);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(9);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(3);
                HandEvaluatorResolver.Get().Should().BeOfType<CustomHandEvaluator>();
                rankInfoCustom.HandStrength.Should().Be(HandRankType.THREECARD);
            }
        }

        [TestMethod]
        public void StraightTest()
        {
            var community = new List<Card>()
            {
                new Card(Card.CLUB, 3),
                new Card(Card.CLUB, 7),
                new Card(Card.DIAMOND, 2),
                new Card(Card.DIAMOND, 5),
                new Card(Card.DIAMOND, 6)
            };

            var hole = new List<Card>()
            {
                new Card(Card.CLUB, 4),
                new Card(Card.SPADE, 5)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);
            var rankInfoCustom = HandEvaluatorResolver.Get().GenHandRankInfo(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be((int)HandRankType.STRAIGHT);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(3);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(0);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(5);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(4);
                HandEvaluatorResolver.Get().Should().BeOfType<CustomHandEvaluator>();
                rankInfoCustom.HandStrength.Should().Be(HandRankType.STRAIGHT);
            }
        }

        [TestMethod]
        public void FlashTest()
        {
            var community = new List<Card>()
            {
                new Card(Card.CLUB, 7),
                new Card(Card.DIAMOND, 2),
                new Card(Card.DIAMOND, 3),
                new Card(Card.DIAMOND, 5),
                new Card(Card.DIAMOND, 6)
            };

            var hole = new List<Card>()
            {
                new Card(Card.CLUB, 4),
                new Card(Card.DIAMOND, 8)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);
            var rankInfoCustom = HandEvaluatorResolver.Get().GenHandRankInfo(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be((int)HandRankType.FLASH);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(8);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(0);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(8);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(4);
                HandEvaluatorResolver.Get().Should().BeOfType<CustomHandEvaluator>();
                rankInfoCustom.HandStrength.Should().Be(HandRankType.FLASH);
            }
        }

        [TestMethod]
        public void FullHouseTest()
        {
            var community = new List<Card>()
            {
                new Card(Card.CLUB, 4),
                new Card(Card.DIAMOND, 2),
                new Card(Card.DIAMOND, 4),
                new Card(Card.DIAMOND, 5),
                new Card(Card.DIAMOND, 6)
            };

            var hole = new List<Card>()
            {
                new Card(Card.CLUB, 4),
                new Card(Card.DIAMOND, 5)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);
            var rankInfoCustom = HandEvaluatorResolver.Get().GenHandRankInfo(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be((int)HandRankType.FULLHOUSE);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(4);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(5);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(5);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(4);
                HandEvaluatorResolver.Get().Should().BeOfType<CustomHandEvaluator>();
                rankInfoCustom.HandStrength.Should().Be(HandRankType.FULLHOUSE);
            }
        }

        [TestMethod]
        public void FullHouse2Test()
        {
            var community = new List<Card>()
            {
                new Card(Card.CLUB, 3),
                new Card(Card.DIAMOND, 7),
                new Card(Card.DIAMOND, 3),
                new Card(Card.HEART, 3),
                new Card(Card.HEART, 7)
            };

            var hole = new List<Card>()
            {
                new Card(Card.SPADE, 8),
                new Card(Card.SPADE, 7)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);
            var rankInfoCustom = HandEvaluatorResolver.Get().GenHandRankInfo(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be((int)HandRankType.FULLHOUSE);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(7);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(3);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(8);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(7);
                HandEvaluatorResolver.Get().Should().BeOfType<CustomHandEvaluator>();
                rankInfoCustom.HandStrength.Should().Be(HandRankType.FULLHOUSE);
            }
        }

        [TestMethod]
        public void FourCardTest()
        {
            var community = new List<Card>()
            {
                new Card(Card.CLUB, 3),
                new Card(Card.DIAMOND, 7),
                new Card(Card.DIAMOND, 3),
                new Card(Card.HEART, 3),
                new Card(Card.HEART, 7)
            };

            var hole = new List<Card>()
            {
                new Card(Card.SPADE, 3),
                new Card(Card.SPADE, 8)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);
            var rankInfoCustom = HandEvaluatorResolver.Get().GenHandRankInfo(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be((int)HandRankType.FOURCARD);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(3);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(0);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(8);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(3);
                HandEvaluatorResolver.Get().Should().BeOfType<CustomHandEvaluator>();
                rankInfoCustom.HandStrength.Should().Be(HandRankType.FOURCARD);
            }
        }

        [TestMethod]
        public void StraightFlashTest()
        {
            var community = new List<Card>()
            {
                new Card(Card.DIAMOND, 4),
                new Card(Card.DIAMOND, 5),
                new Card(Card.HEART, 11),
                new Card(Card.HEART, 12),
                new Card(Card.HEART, 13)
            };

            var hole = new List<Card>()
            {
                new Card(Card.HEART, 10),
                new Card(Card.HEART, 1)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);
            var rankInfoCustom = HandEvaluatorResolver.Get().GenHandRankInfo(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be((int)HandRankType.STRAIGHTFLASH);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(10);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(0);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(14);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(10);
                HandEvaluatorResolver.Get().Should().BeOfType<CustomHandEvaluator>();
                rankInfoCustom.HandStrength.Should().Be(HandRankType.STRAIGHTFLASH);
            }
        }

        public class CustomHandEvaluator : IHandEvaluator
        {

            private static Dictionary<HoldemHand.Hand.HandTypes, HandRankType> _handTypeMapping = 
                new Dictionary<HoldemHand.Hand.HandTypes, HandRankType>
                {
                    { HoldemHand.Hand.HandTypes.Flush, HandRankType.FLASH },
                    { HoldemHand.Hand.HandTypes.FullHouse, HandRankType.FULLHOUSE },
                    { HoldemHand.Hand.HandTypes.Pair, HandRankType.ONEPAIR },
                    { HoldemHand.Hand.HandTypes.Straight, HandRankType.STRAIGHT },
                    { HoldemHand.Hand.HandTypes.StraightFlush, HandRankType.STRAIGHTFLASH },
                    { HoldemHand.Hand.HandTypes.FourOfAKind, HandRankType.FOURCARD },
                    { HoldemHand.Hand.HandTypes.TwoPair, HandRankType.TWOPAIR },
                    { HoldemHand.Hand.HandTypes.HighCard, HandRankType.HIGHCARD },
                    { HoldemHand.Hand.HandTypes.Trips, HandRankType.THREECARD },
                };

            public int EvalHand(IEnumerable<Card> hole, IEnumerable<Card> community)
            {
                return (int)GetPockerHand(hole, community).HandValue;
            }

            public HandRankInfo GenHandRankInfo(IEnumerable<Card> hole, IEnumerable<Card> community)
            {
                var hand = GetPockerHand(hole, community);
                return new HandRankInfo
                {
                    HandHigh = (int)hand.BoardMask,
                    HandLow = 0,
                    HoleHigh = (int)hand.PocketMask,
                    HoleLow = 0,
                    HandStrength = _handTypeMapping[hand.HandTypeValue],
                };
            }

            private static HoldemHand.Hand GetPockerHand(IEnumerable<Card> hole, IEnumerable<Card> community)
            {
                return new HoldemHand.Hand(pocket: ConvertCards(hole), board: ConvertCards(community));
            }

            private static string ConvertCards(IEnumerable<Card> cards)
            {
                return string.Join(" ", cards.Select(card => card.ToString()).Select(card => $"{card[1].ToString().ToUpper()}{card[0].ToString().ToLower()}"));
            }
        }
    }
}
