﻿using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
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

            var message = MessageBuilder.Instance.BuildStreetStartMessage(state.ToDictionary());
            var msg = (IDictionary)message["message"];

            using (new AssertionScope())
            {
                message["type"].Should().Be("notification");
                msg["message_type"].Should().Be(MessageBuilder.STREET_START_MESSAGE);
                msg["street"].Should().Be("flop");
                msg["round_state"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeRoundState(state.ToDictionary()));
            }
        }

        [TestMethod]
        public void AskStartMessageTest()
        {
            var state = SetupState();

            var message = MessageBuilder.Instance.BuildAskMessage(1, state);
            var msg = (IDictionary)message["message"];

            using (new AssertionScope())
            {
                message["type"].Should().Be("ask");
                msg["message_type"].Should().Be(MessageBuilder.ASK_MESSAGE);
                msg["hole_card"].Should().BeEquivalentTo(new List<string> { "CA", "C2" });
                ((ICollection)msg["valid_actions"]).Count.Should().Be(3);
                msg["round_state"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeRoundState(state.ToDictionary()));
                msg["action_histories"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeActionHistories(state.Table));
            }
        }

        [TestMethod]
        public void GameUpdateMessageTest()
        {
            var state = SetupState();
            var player = state.Table.Seats.Players[1];

            var message = MessageBuilder.Instance.BuildGameUpdateMessage(1, "call", 10, state.ToDictionary());
            var msg = (IDictionary)message["message"];

            using (new AssertionScope())
            {
                message["type"].Should().Be("notification");
                msg["message_type"].Should().Be(MessageBuilder.GAME_UPDATE_MESSAGE);
                msg["action"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeAction(player, "call", 10));
                msg["round_state"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeRoundState(state.ToDictionary()));
                msg["action_histories"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeActionHistories(state.Table));
            }
        }

        [TestMethod]
        public void RoundResultMessageTest()
        {
            var state = SetupState();
            var winners = state.Table.Seats.Players.Skip(1).Take(1);
            var handInfo = new string[] { "dummy", "info" };

            var message = MessageBuilder.Instance.BuildRoundResultMessage(7, winners, handInfo, state.ToDictionary());
            var msg = (IDictionary)message["message"];

            using (new AssertionScope())
            {
                message["type"].Should().Be("notification");
                msg["message_type"].Should().Be(MessageBuilder.ROUND_RESULT_MESSAGE);
                msg["round_count"].Should().Be(7);
                msg["hand_info"].Should().BeEquivalentTo(handInfo);
                msg["winners"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeWinners(winners)["winners"]);
                msg["round_state"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeRoundState(state.ToDictionary()));
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
            var players = Enumerable.Range(0, 3).Select(ix => SetupPlayer(ix)).ToList();
            players[1].AddHoleCards(hole);
            return players;
        }

        private Player SetupPlayer(int? ix = null) => new Player($"uuid{ix}", 100, "hoge");
    }
}
