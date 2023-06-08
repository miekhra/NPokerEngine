using FluentAssertions;
using FluentAssertions.Execution;

namespace NPokerEngine.Tests.Engine
{
    [TestClass]
    public class MessageBuilderTests
    {
        [TestMethod]
        public void GameStartMessageTest()
        {
            var config = SetupConfig();
            var seats = SetupSeats();

            var message = MessageBuilder.Instance.BuildGameStartMessage(config, seats);

            using (new AssertionScope())
            {
                MessageBuilder.GetMessageType(message).Should().Be(MessageBuilder.NOTIFICATION);
                message.MessageType.Should().Be(MessageType.GAME_START_MESSAGE);
            }
        }

        [TestMethod]
        public void RoundStartMessageTest()
        {
            var seats = SetupSeats();

            var message = MessageBuilder.Instance.BuildRoundStartMessage(7, 1, seats);

            using (new AssertionScope())
            {
                MessageBuilder.GetMessageType(message).Should().Be(MessageBuilder.NOTIFICATION);
                message.MessageType.Should().Be(MessageType.ROUND_START_MESSAGE);
                message.RoundCount.Should().Be(7);
                message.HoleCards.Should().BeEquivalentTo(new List<Card> { Card.FromString("CA"), Card.FromString("C2") });
            }
        }

        [TestMethod]
        public void StreetStartMessageTest()
        {
            var state = SetupState();

            var message = MessageBuilder.Instance.BuildStreetStartMessage(state);

            using (new AssertionScope())
            {
                MessageBuilder.GetMessageType(message).Should().Be(MessageBuilder.NOTIFICATION);
                message.MessageType.Should().Be(MessageType.STREET_START_MESSAGE);
                message.Street.Should().Be(StreetType.FLOP);
            }
        }

        [TestMethod]
        public void AskStartMessageTest()
        {
            var state = SetupState();

            var message = MessageBuilder.Instance.BuildAskMessage(1, state);

            using (new AssertionScope())
            {
                MessageBuilder.GetMessageType(message).Should().Be(MessageBuilder.ASK);
                message.MessageType.Should().Be(MessageType.ASK_MESSAGE);
                message.State.Table.Seats[message.PlayerUuid].HoleCards.Should().BeEquivalentTo(new List<Card> { Card.FromString("CA"), Card.FromString("C2") });
                message.ValidActions.Count.Should().Be(3);
            }
        }

        [TestMethod]
        public void GameUpdateMessageTest()
        {
            var state = SetupState();
            var player = state.Table.Seats[1];

            var message = MessageBuilder.Instance.BuildGameUpdateMessage(1, ActionType.CALL, 10, state);

            using (new AssertionScope())
            {
                MessageBuilder.GetMessageType(message).Should().Be(MessageBuilder.NOTIFICATION);
                message.MessageType.Should().Be(MessageType.GAME_UPDATE_MESSAGE);
                message.PlayerUuid.Should().Be(player.Uuid);
                message.Action.Should().Be(ActionType.CALL);
                message.Amount.Should().Be(10);
            }
        }

        [TestMethod]
        public void RoundResultMessageTest()
        {
            var state = SetupState();
            var winners = state.Table.Seats.Players.Skip(1).Take(1);
            var handInfo = new string[] { "dummy", "info" };

            var message = MessageBuilder.Instance.BuildRoundResultMessage(7, winners, handInfo, state);

            using (new AssertionScope())
            {
                MessageBuilder.GetMessageType(message).Should().Be(MessageBuilder.NOTIFICATION);
                message.MessageType.Should().Be(MessageType.ROUND_RESULT_MESSAGE);
                message.RoundCount.Should().Be(7);
                message.Winners.Should().BeEquivalentTo(winners);
            }
        }

        [TestMethod]
        public void GameResultMessageTest()
        {
            var config = SetupConfig();
            var state = SetupState();
            var seats = SetupSeats();

            var message = MessageBuilder.Instance.BuildGameResultMessage(config, seats);

            using (new AssertionScope())
            {
                MessageBuilder.GetMessageType(message).Should().Be(MessageBuilder.NOTIFICATION);
                message.MessageType.Should().Be(MessageType.GAME_RESULT_MESSAGE);
            }
        }

        private GameState SetupState()
            => new GameState
            {
                Street = StreetType.FLOP,
                NextPlayerIx = 2,
                RoundCount = 3,
                SmallBlindAmount = 4,
                Table = SetupTable()
            };

        private Table SetupTable()
        {
            var table = new Table();
            table._seats = SetupSeats();
            table.AddCommunityCard(Card.FromId(1));
            table.SetBlindPositions(1, 2);
            return table;
        }

        private GameConfig SetupConfig()
            => new GameConfig
            {
                InitialStack = 100,
                MaxRound = 10,
                SmallBlindAmount = 5,
                Ante = 3,
                BlindStructure = null
            };
        //=> new Dictionary<string, object>
        //{
        //    { "initial_stack", 100 },
        //    { "max_round", 10 },
        //    { "small_blind_amount", 5 },
        //    { "ante", 3 },
        //    { "blind_structure", null }
        //};

        private Seats SetupSeats()
        {
            var seats = new Seats();
            SetupPlayers().ForEach(p => seats.Sitdown(p));
            return seats;
        }

        private List<Player> SetupPlayers()
        {
            var hole = new Card[] { Card.FromId(1), Card.FromId(2) };
            var players = Enumerable.Range(0, 3).Select(ix => SetupPlayer(ix)).ToList();
            players[1].AddHoleCards(hole);
            return players;
        }

        private Player SetupPlayer(int? ix = null) => new Player($"uuid{ix}", 100, "hoge");
    }
}
