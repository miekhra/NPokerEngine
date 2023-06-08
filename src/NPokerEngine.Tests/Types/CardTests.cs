using FluentAssertions;
using FluentAssertions.Execution;

namespace NPokerEngine.Tests.Types
{
    [TestClass]
    public class CardTests
    {
        [TestMethod]
        public void ToStringTest()
        {
            using (new AssertionScope())
            {
                new Card(Card.CLUB, 1).ToString().Should().Be("CA");
                new Card(Card.CLUB, 14).ToString().Should().Be("CA");
                new Card(Card.CLUB, 2).ToString().Should().Be("C2");
                new Card(Card.HEART, 10).ToString().Should().Be("HT");
                new Card(Card.SPADE, 11).ToString().Should().Be("SJ");
                new Card(Card.DIAMOND, 12).ToString().Should().Be("DQ");
                new Card(Card.DIAMOND, 13).ToString().Should().Be("DK");
            }
        }

        [TestMethod]
        public void ToIdTest()
        {
            using (new AssertionScope())
            {
                new Card(Card.HEART, 3).ToId().Should().Be(29);
                new Card(Card.SPADE, 1).ToId().Should().Be(40);
            }
        }

        [TestMethod]
        public void FromIdTest()
        {
            using (new AssertionScope())
            {
                Card.FromId(1).Should().BeEquivalentTo(new Card(Card.CLUB, 1));
                Card.FromId(29).Should().BeEquivalentTo(new Card(Card.HEART, 3));
                Card.FromId(40).Should().BeEquivalentTo(new Card(Card.SPADE, 1));
            }
        }

        [TestMethod]
        public void FromStringTest()
        {
            using (new AssertionScope())
            {
                Card.FromString("CA").Should().BeEquivalentTo(new Card(Card.CLUB, 14));
                Card.FromString("HT").Should().BeEquivalentTo(new Card(Card.HEART, 10));
                Card.FromString("S9").Should().BeEquivalentTo(new Card(Card.SPADE, 9));
                Card.FromString("DQ").Should().BeEquivalentTo(new Card(Card.DIAMOND, 12));
            }
        }
    }
}
