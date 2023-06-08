using FluentAssertions;
using FluentAssertions.Execution;

namespace NPokerEngine.Tests.Types
{
    [TestClass]
    public class TableTests
    {
        private Table _table;
        private Player _player;

        [TestInitialize]
        public void Initialize()
        {
            SetupTable();
            SetupPlayer();
        }

        [TestMethod]
        public void SetBlindTest()
        {
            using (new AssertionScope())
            {
                _table.SmallBlindPosition.Should().BeNull();
                _table.BigBlindPosition.Should().BeNull();
                _table.SetBlindPositions(1, 2);
                _table.SmallBlindPosition.Should().Be(1);
                _table.BigBlindPosition.Should().Be(2);
            }
        }

        [TestMethod]
        public void ResetDeckTest()
        {
            _table.Reset();
            _table.Deck.Size.Should().Be(52);
        }

        [TestMethod]
        public void ResetCommunityCardTest()
        {

            var action = () =>
            {
                _table.Reset();
                foreach (var card in _table.Deck.DrawCards(5))
                {
                    _table.AddCommunityCard(card);
                }
            };

            action.Should().NotThrow();
        }

        [TestMethod]
        public void ResetplayerStatusTest()
        {
            _table.Reset();
            using (new AssertionScope())
            {
                _player.HoleCards.Should().BeEmpty();
                _player.ActionHistories.Should().BeEmpty();
                _player.PayInfo.Status.Should().Be(PayInfoStatus.PAY_TILL_END);
            }
        }

        [TestMethod]
        public void CommunityCardExceedTest()
        {

            Action action = () =>
            {
                _table.AddCommunityCard(Card.FromId(1));
            };

            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void ShiftDealerButtonSkipTest()
        {
            _table = SetupPlayersWithTable();
            _table.ShiftDealerButton();

            using (new AssertionScope())
            {
                _table.DealerButton.Should().Be(2);
                _table.ShiftDealerButton();
                _table.DealerButton.Should().Be(0);
            }
        }

        [TestMethod]
        public void NextAskWaitingPlayerPosTest()
        {
            _table = SetupPlayersWithTable();
            using (new AssertionScope())
            {
                _table.NextAskWaitingPlayerPosition(0).Should().Be(0);
                _table.NextAskWaitingPlayerPosition(1).Should().Be(0);
                _table.NextAskWaitingPlayerPosition(2).Should().Be(0);
            }
        }

        [TestMethod]
        public void NextAskWaitintPlayerPosWhenNoOneWaitingTest()
        {
            _table = SetupPlayersWithTable();
            _table.Seats.Players[0].PayInfo.UpdateToAllin();
            using (new AssertionScope())
            {
                _table.NextAskWaitingPlayerPosition(0).Should().Be(-1);
                _table.NextAskWaitingPlayerPosition(1).Should().Be(-1);
                _table.NextAskWaitingPlayerPosition(2).Should().Be(-1);
            }
        }

        [TestMethod]
        public void SerializationTest()
        {
            _table = SetupPlayersWithTable();
            _table.Deck.DrawCards(3).ForEach(card => _table.AddCommunityCard(card));
            _table.ShiftDealerButton();
            _table.SetBlindPositions(1, 2);

            var restoredTable = (Table)_table.Clone();
            using (new AssertionScope())
            {
                restoredTable.DealerButton.Should().Be(_table.DealerButton);
                restoredTable.Seats.ActivePlayersCount().Should().Be(_table.Seats.ActivePlayersCount());
                restoredTable.Deck.Size.Should().Be(_table.Deck.Size);
                restoredTable.CommunityCards.Should().BeEquivalentTo(_table.CommunityCards);
                restoredTable.SmallBlindPosition.Should().Be(1);
                restoredTable.BigBlindPosition.Should().Be(2);
            }
        }

        private void SetupTable()
        {
            _table = new Table();
            _table.Deck.DrawCards(5).ForEach(card => _table.AddCommunityCard(card));

        }

        private void SetupPlayer()
        {
            _player = new Player("uuid", 100);
            _player.AddHoleCards(Enumerable.Range(1, 2).Select(Card.FromId).ToArray());
            _player.AddActionHistory(ActionType.CALL, 10);
            _player.PayInfo.UpdateToFold();
            _table.Seats.Sitdown(_player);
        }

        private Table SetupPlayersWithTable()
        {
            var p1 = new Player("uuid1", 100);
            var p2 = new Player("uuid2", 100);
            var p3 = new Player("uuid3", 100);

            p2.PayInfo.UpdateToFold();
            p3.PayInfo.UpdateToAllin();

            _table = new Table();
            _table.Seats.Sitdown(p1);
            _table.Seats.Sitdown(p2);
            _table.Seats.Sitdown(p3);

            return _table;
        }
    }
}
