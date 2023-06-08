using FluentAssertions;
using FluentAssertions.Execution;

namespace NPokerEngine.Tests.Types
{
    [TestClass]
    public class DeckTests
    {
        private Deck _deck;

        [TestInitialize]
        public void Initialize()
        {
            _deck = new Deck();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _deck = null;
        }

        [TestMethod]
        public void DrawCardTest()
        {
            var card = _deck.DrawCard();

            using (new AssertionScope())
            {
                card.ToString().Should().Be("SK");
                _deck.Size.Should().Be(51);
            }
        }

        [TestMethod]
        public void DrawCardsTest()
        {
            var cards = _deck.DrawCards(3);

            using (new AssertionScope())
            {
                cards[2].ToString().Should().Be("SJ");
                _deck.Size.Should().Be(49);
            }
        }

        [TestMethod]
        public void RestoreTest()
        {
            _deck.DrawCards(5);
            _deck.Restore();
            _deck.Size.Should().Be(52);
        }

        [TestMethod]
        public void SerializationTest()
        {
            _deck.Shuffle();
            _deck.DrawCards(3);

            var serial = _deck.Serialize();
            var restoredDeck = Deck.Deserialize(serial);

            using (new AssertionScope())
            {
                _deck.IsCheat.Should().Be(restoredDeck.IsCheat);
                _deck._deck.Should().BeEquivalentTo(restoredDeck._deck);
            }
        }

        [TestMethod]
        public void CheatDrawTest()
        {
            var cardIds = new int[] { 12, 15, 17 };
            var cheatDeck = new Deck(cheat: true, cheatCardIds: cardIds);
            cheatDeck.DrawCards(3).Should().BeEquivalentTo(cardIds.Select(Card.FromId));
        }

        [TestMethod]
        public void CheatRestoreTest()
        {
            var cardIds = new int[] { 12, 15, 17 };
            var cheatDeck = new Deck(cheat: true, cheatCardIds: cardIds);
            cheatDeck.DrawCards(2);
            cheatDeck.Restore();
            cheatDeck.DrawCards(3).Should().BeEquivalentTo(cardIds.Select(Card.FromId));
        }

        [TestMethod]
        public void CheatSerializationTest()
        {
            var cardIds = new int[] { 12, 15, 17 };
            var cheatDeck = new Deck(cheat: true, cheatCardIds: cardIds);

            var serial = cheatDeck.Serialize();
            var restoredDeck = Deck.Deserialize(serial);

            using (new AssertionScope())
            {
                cheatDeck.IsCheat.Should().Be(restoredDeck.IsCheat);
                cheatDeck._deck.Should().BeEquivalentTo(restoredDeck._deck);
                cheatDeck._cheatCardIds.Should().BeEquivalentTo(restoredDeck._cheatCardIds);
            }
        }
    }
}
