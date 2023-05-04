using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NPokerEngine.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
            var msg = (IDictionary)message["message"];

            using (new AssertionScope())
            {
                message["type"].Should().Be("notification");
                msg["message_type"].Should().Be(MessageBuilder.GAME_START_MESSAGE);
                ((Dictionary<string, object>)msg["game_information"]).Should().BeEquivalentTo(DataEncoder.Instance.EncodeGameInformation(config, seats));
            }
        }

        [TestMethod]
        public void RoundStartMessageTest()
        {
            var seats = SetupSeats();

            var message = MessageBuilder.Instance.BuildRoundStartMessage(7, 1, seats);
            var msg = (IDictionary)message["message"];

            using (new AssertionScope())
            {
                message["type"].Should().Be("notification");
                msg["message_type"].Should().Be(MessageBuilder.ROUND_START_MESSAGE);
                msg["round_count"].Should().Be(7);
                msg["seats"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeSeats(seats)["seats"]);
                msg["hole_card"].Should().BeEquivalentTo(new List<string> { "CA", "C2" });
            }
        }

        [TestMethod]
        public void StreetStartMessageTest()
        {
            var state = SetupState();

            var message = MessageBuilder.Instance.BuildStreetStartMessage(state);
            var msg = (IDictionary)message["message"];

            using (new AssertionScope())
            {
                message["type"].Should().Be("notification");
                msg["message_type"].Should().Be(MessageBuilder.STREET_START_MESSAGE);
                msg["street"].Should().Be("flop");
                msg["round_state"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeRoundState(state));
            }
        }

        [TestMethod]
        public void AskStartMessageTest()
        {
            var state = SetupState();
            var table = (Table)state["table"];

            var message = MessageBuilder.Instance.BuildAskMessage(1, state);
            var msg = (IDictionary)message["message"];

            using (new AssertionScope())
            {
                message["type"].Should().Be("ask");
                msg["message_type"].Should().Be(MessageBuilder.ASK_MESSAGE);
                msg["hole_card"].Should().BeEquivalentTo(new List<string> { "CA", "C2" });
                ((ICollection)msg["valid_actions"]).Count.Should().Be(3);
                msg["round_state"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeRoundState(state));
                msg["action_histories"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeActionHistories(table));
            }
        }

        [TestMethod]
        public void GameUpdateMessageTest()
        {
            var state = SetupState();
            var table = (Table)state["table"];
            var player = table.Seats.Players[1];

            var message = MessageBuilder.Instance.BuildGameUpdateMessage(1, "call", 10, state);
            var msg = (IDictionary)message["message"];

            using (new AssertionScope())
            {
                message["type"].Should().Be("notification");
                msg["message_type"].Should().Be(MessageBuilder.GAME_UPDATE_MESSAGE);
                msg["action"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeAction(player, "call", 10));
                msg["round_state"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeRoundState(state));
                msg["action_histories"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeActionHistories(table));
            }
        }

        [TestMethod]
        public void RoundResultMessageTest()
        {
            var state = SetupState();
            var winners = ((Table)state["table"]).Seats.Players.Skip(1).Take(1);
            var handInfo = new string[] { "dummy", "info" };

            var message = MessageBuilder.Instance.BuildRoundResultMessage(7, winners, handInfo, state);
            var msg = (IDictionary)message["message"];

            using (new AssertionScope())
            {
                message["type"].Should().Be("notification");
                msg["message_type"].Should().Be(MessageBuilder.ROUND_RESULT_MESSAGE);
                msg["round_count"].Should().Be(7);
                msg["hand_info"].Should().BeEquivalentTo(handInfo);
                msg["winners"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeWinners(winners)["winners"]);
                msg["round_state"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeRoundState(state));
            }
        }

        [TestMethod]
        public void GameResultMessageTest()
        {
            var config = SetupConfig();
            var state = SetupState();
            var seats = SetupSeats();

            var message = MessageBuilder.Instance.BuildGameResultMessage(config, seats);
            var msg = (IDictionary)message["message"];

            using (new AssertionScope())
            {
                message["type"].Should().Be("notification");
                msg["message_type"].Should().Be(MessageBuilder.GAME_RESULT_MESSAGE);
                msg["game_information"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeGameInformation(config, seats));
            }
        }

        private Dictionary<string, object> SetupState()
            => new Dictionary<string, object>
            {
                { "street", 1 },
                { "next_player", 2},
                { "round_count", 3 },
                { "small_blind_amount", 4 },
                { "table", SetupTable() }
            };

        private Table SetupTable()
        {
            var table = new Table();
            table._seats = SetupSeats();
            table.AddCommunityCard(Card.FromId(1));
            table.SetBlindPositions(1, 2);
            return table;
        }

        private Dictionary<string, object> SetupConfig()
            => new Dictionary<string, object>
            {
                { "initial_stack", 100 },
                { "max_round", 10 },
                { "small_blind_amount", 5 },
                { "ante", 3 },
                { "blind_structure", null }
            };

        private Seats SetupSeats()
        {
            var seats = new Seats();
            SetupPlayers().ForEach(p =>  seats.Sitdown(p));
            return seats;
        }

        private List<Player> SetupPlayers()
        {
            var hole = new Card[] { Card.FromId(1), Card.FromId(2) };
            var players = Enumerable.Range(0, 3).Select(ix => SetupPlayer()).ToList();
            players[1].AddHoleCards(hole);
            return players;
        }

        private Player SetupPlayer() => new Player("uuid", 100, "hoge");
    }
}
