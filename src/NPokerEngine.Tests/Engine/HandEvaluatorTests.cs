using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NPokerEngine.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPokerEngine.Tests.Engine
{
    [TestClass]
    public class HandEvaluatorTests
    {
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

            using (new AssertionScope())
            {
                rankInfo.HandStrength.Should().Be("HIGHCARD");
                //((IDictionary)rankInfo["hand"])["strength"].Should().Be("HIGHCARD");
                rankInfo.HandHigh.Should().Be(9);
                //((IDictionary)rankInfo["hand"])["high"].Should().Be(9);
                rankInfo.HandLow.Should().Be(2);
                //((IDictionary)rankInfo["hand"])["low"].Should().Be(2);
                rankInfo.HoleHigh.Should().Be(9);
                //((IDictionary)rankInfo["hole"])["high"].Should().Be(9);
                rankInfo.HoleLow.Should().Be(2);
                //((IDictionary)rankInfo["hole"])["low"].Should().Be(2);
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

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be(HandEvaluator.HIGHCARD);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(9);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(2);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(9);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(2);
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

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be(HandEvaluator.ONEPAIR);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(3);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(0);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(9);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(3);
            }
        }

        [TestMethod]
        public void TwoPairTest()
        {
            var community = new List<Card>()
            {
                new Card(Card.CLUB, 7),
                new Card(Card.CLUB, 9),
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

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be(HandEvaluator.TWOPAIR);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(9);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(3);
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

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be(HandEvaluator.TWOPAIR);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(8);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(7);
            }
        }

        [TestMethod]
        public void ThreeCardTest()
        {
            var community = new List<Card>()
            {
                new Card(Card.CLUB, 3),
                new Card(Card.CLUB, 7),
                new Card(Card.DIAMOND, 3),
                new Card(Card.DIAMOND, 5),
                new Card(Card.DIAMOND, 6)
            };

            var hole = new List<Card>()
            {
                new Card(Card.CLUB, 9),
                new Card(Card.DIAMOND, 3)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be(HandEvaluator.THREECARD);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(3);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(0);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(9);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(3);
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
                new Card(Card.DIAMOND, 5)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be(HandEvaluator.STRAIGHT);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(3);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(0);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(5);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(4);
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
                new Card(Card.DIAMOND, 5)
            };

            var bit = HandEvaluator.Instance.EvalHand(hole, community);

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be(HandEvaluator.FLASH);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(6);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(0);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(5);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(4);
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

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be(HandEvaluator.FULLHOUSE);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(4);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(5);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(5);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(4);
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

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be(HandEvaluator.FULLHOUSE);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(7);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(3);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(8);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(7);
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

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be(HandEvaluator.FOURCARD);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(3);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(0);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(8);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(3);
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

            using (new AssertionScope())
            {
                HandEvaluator.Instance.MaskHandStrength(bit).Should().Be(HandEvaluator.STRAIGHTFLASH);
                HandEvaluator.Instance.MaskHandHighRank(bit).Should().Be(10);
                HandEvaluator.Instance.MaskHandLowRank(bit).Should().Be(0);
                HandEvaluator.Instance.MaskHoleHighRank(bit).Should().Be(14);
                HandEvaluator.Instance.MaskHoleLowRank(bit).Should().Be(10);
            }
        }
    }
}
