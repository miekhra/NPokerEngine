using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NPokerEngine.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NPokerEngine.Tests.Engine
{
    [TestClass]
    public class DataEncoderTests
    {
        [TestMethod]
        public void EncodePlayerWithoutHoldecardsTest()
        {
            var player = SetupPlayer();
            var hsh = DataEncoder.Instance.EncodePlayer(player);

            using (new AssertionScope())
            {
                hsh["name"].Should().Be(player.Name);
                hsh["uuid"].Should().Be(player.Uuid);
                hsh["stack"].Should().Be(player.Stack);
                hsh["state"].Should().Be("folded");
                hsh.ContainsKey("hole_card").Should().BeFalse();
            }
        }

        [TestMethod]
        public void EncodePlayerWithHolecardsTest()
        {
            var player = SetupPlayer();
            var hsh = DataEncoder.Instance.EncodePlayer(player, holecards: true);

            hsh["hole_card"].Should().BeEquivalentTo(player.HoleCards.Select(card => card.ToString()));
        }

        [TestMethod]
        public void EncodeSeatsTest()
        {
            var seats = SetupSeats();
            var hsh = DataEncoder.Instance.EncodeSeats(seats);

            using (new AssertionScope())
            {
                ((ICollection)hsh["seats"]).Count.Should().Be(3);
                DataEncoder.Instance.EncodePlayer(seats.Players[0]).Should().BeEquivalentTo(((IList)hsh["seats"])[0]);
            }
        }

        [TestMethod]
        public void EncodePotTest()
        {
            var players = SetupPlayersForPot();
            var hsh = DataEncoder.Instance.EncodePot(players);
            var mainPot = (Dictionary<string, object>)hsh["main"];
            var sidePot1 = (Dictionary<string, object>)((IList)hsh["side"])[0];
            var sidePot2 = (Dictionary<string, object>)((IList)hsh["side"])[1];

            using (new AssertionScope())
            {
                mainPot["amount"].Should().Be(22);
                sidePot1["amount"].Should().Be(9);
                ((IList)sidePot1["eligibles"]).Count.Should().Be(3);
                sidePot1["eligibles"].Should().BeEquivalentTo(new List<string> { "uuid1", "uuid2", "uuid3" });
                ((IList)sidePot2["eligibles"]).Count.Should().Be(2);
                sidePot2["eligibles"].Should().BeEquivalentTo(new List<string> { "uuid1", "uuid3" });
            }
        }

        [TestMethod]
        public void EncodeGameInformationTest()
        {
            var config = new Dictionary<string, object>
            {
                { "initial_stack", 100 },
                { "max_round", 10 },
                { "small_blind_amount", 5 },
                { "ante", 1 },
                { "blind_structure", new Dictionary<int, Dictionary<string, object>>
                    {
                        {1, new Dictionary<string, object> { { "ante", 3 }, { "small_blind", 10 } } }
                    }
                }
            };
            var seats = SetupSeats();
            var hsh = DataEncoder.Instance.EncodeGameInformation(config, seats);

            using (new AssertionScope())
            {
                hsh["player_num"].Should().Be(3);
                hsh["seats"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeSeats(seats)["seats"]);
                ((IDictionary)hsh["rule"])["small_blind_amount"].Should().Be(config["small_blind_amount"]);
                ((IDictionary)hsh["rule"])["max_round"].Should().Be(config["max_round"]);
                ((IDictionary)hsh["rule"])["initial_stack"].Should().Be(config["initial_stack"]);
                ((IDictionary)hsh["rule"])["ante"].Should().Be(config["ante"]);
                ((IDictionary)hsh["rule"])["blind_structure"].Should().BeEquivalentTo(config["blind_structure"]);
            }
        }

        [TestMethod]
        public void EncodeValidActionsTest()
        {
            var hsh = DataEncoder.Instance.EncodeValidActions(10, 20, 100);
            var acts = (List<Dictionary<string, object>>)hsh["valid_actions"];

            using (new AssertionScope())
            {
                acts[0]["action"].Should().Be("fold");
                acts[0]["amount"].Should().Be(0);
                acts[1]["action"].Should().Be("call");
                acts[1]["amount"].Should().Be(10);
                acts[2]["action"].Should().Be("raise");
                acts[2]["amount"].Should().BeEquivalentTo(new Dictionary<string, object> { { "min", 20 }, { "max", 100 } });
            }
        }

        [TestMethod]
        public void EncodeActionTest()
        {
            var player = SetupPlayer();
            var hsh = DataEncoder.Instance.EncodeAction(player, "raise", 20);

            using (new AssertionScope())
            {
                hsh["player_uuid"].Should().Be(player.Uuid);
                hsh["action"].Should().Be("raise");
                hsh["amount"].Should().Be(20);
            }
        }

        [TestMethod]
        public void EncodeStreetTest()
        {
            using (new AssertionScope())
            {
                DataEncoder.Instance.EncodeStreet((byte)StreetType.PREFLOP)["street"].Should().Be("preflop");
                DataEncoder.Instance.EncodeStreet((byte)StreetType.FLOP)["street"].Should().Be("flop");
                DataEncoder.Instance.EncodeStreet((byte)StreetType.TURN)["street"].Should().Be("turn");
                DataEncoder.Instance.EncodeStreet((byte)StreetType.RIVER)["street"].Should().Be("river");
                DataEncoder.Instance.EncodeStreet((byte)StreetType.SHOWDOWN)["street"].Should().Be("showdown");
            }
        }

        [TestMethod]
        public void EncodeActionHistoriesTest()
        {
            var table = SetupTable();
            var p1 = table.Seats.Players[0];
            var p2 = table.Seats.Players[1];
            var p3 = table.Seats.Players[2];

            var hsh = DataEncoder.Instance.EncodeActionHistories(table);
            var hsty = (Dictionary<string, List<ActionHistoryEntry>>)hsh["action_histories"];

            (string action, float amount) fetchInfo(ActionHistoryEntry info)
                => (info.ActionType.ToString(), info.Amount);

            using (new AssertionScope())
            {
                ((IList)hsty["preflop"]).Count.Should().Be(4);
                fetchInfo(hsty["preflop"][0]).Should().BeEquivalentTo(("RAISE", 10));
                hsty["preflop"][1].ActionType.Should().Be(ActionType.FOLD);
                fetchInfo(hsty["preflop"][2]).Should().BeEquivalentTo(("RAISE", 20));
                fetchInfo(hsty["preflop"][3]).Should().BeEquivalentTo(("CALL", 20));
                ((IList)hsty["flop"]).Count.Should().Be(2);
                fetchInfo(hsty["flop"][0]).Should().BeEquivalentTo(("CALL", 5));
                fetchInfo(hsty["flop"][1]).Should().BeEquivalentTo(("RAISE", 5));
                hsty.ContainsKey("turn").Should().BeFalse();
                hsty.ContainsKey("river").Should().BeFalse();
            }
        }

        [TestMethod]
        public void EncodeWinnersTest()
        {
            var winners = Enumerable.Range(0,2).Select(ix => SetupPlayer()).ToList();
            var hsh = DataEncoder.Instance.EncodeWinners(winners);

            using (new AssertionScope())
            {
                ((IList)hsh["winners"]).Count.Should().Be(2);
                hsh["winners"].Should().BeEquivalentTo(winners.Select(p => DataEncoder.Instance.EncodePlayer(p)));
            }
        }

        [TestMethod]
        public void EncodeRoundStateTest()
        {
            var state = SetupRoundState();
            ((Table)state["table"]).SetBlindPositions(1, 3);
            var hsh = DataEncoder.Instance.EncodeRoundState(state);

            using (new AssertionScope())
            {
                hsh["street"].Should().Be("flop");
                hsh["pot"].Should().BeEquivalentTo(DataEncoder.Instance.EncodePot(((Table)state["table"]).Seats.Players));
                hsh["seats"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeSeats(((Table)state["table"]).Seats)["seats"]);
                hsh["community_card"].Should().BeEquivalentTo(new string[] { "CA" });
                hsh["dealer_btn"].Should().Be(((Table)state["table"]).DealerButton);
                hsh["next_player"].Should().Be(state["next_player"]);
                hsh["small_blind_pos"].Should().Be(1);
                hsh["big_blind_pos"].Should().Be(3);
                hsh["action_histories"].Should().BeEquivalentTo(DataEncoder.Instance.EncodeActionHistories(((Table)state["table"]))["action_histories"]);
                hsh["round_count"].Should().Be(state["round_count"]);
                hsh["small_blind_amount"].Should().Be(state["small_blind_amount"]);
            }
        }

        private Player SetupPlayer()
        {
            var player = SetupPlayerWithPayInfo(0, "hoge", 50, PayInfo.FOLDED);
            player.AddHoleCards(Card.FromId(1), Card.FromId(2));
            player.AddActionHistory(ActionType.CALL, 50);
            return player;
        }

        private Player SetupPlayerWithPayInfo(int idx, string name, float amount, int status)
        {
            var player = new Player($"uuid{idx}", 100, name);
            player.PayInfo._amount = amount;
            player.PayInfo._status = status;
            return player;
        }

        private List<Player> SetupPlayersForPot()
            => new List<Player>
            { 
                SetupPlayerWithPayInfo(0, "A", 5, PayInfo.ALLIN),
                SetupPlayerWithPayInfo(1, "B", 10, PayInfo.PAY_TILL_END),
                SetupPlayerWithPayInfo(2, "C", 8, PayInfo.ALLIN),
                SetupPlayerWithPayInfo(3, "D", 10, PayInfo.PAY_TILL_END),
                SetupPlayerWithPayInfo(4, "E", 2, PayInfo.FOLDED)
            };

        private Seats SetupSeats()
        {
            var seats = new Seats();
            Enumerable.Range(0, 3).ToList().ForEach(ix => seats.Sitdown(SetupPlayer()));
            return seats;
        }

        private Table SetupTable()
        {
            var table = new Table();
            Enumerable.Range(0, 3).ToList().ForEach(ix => table.Seats.Sitdown(new Player($"uuid{ix}", 100, "hoge")));
            table.AddCommunityCard(Card.FromId(1));
            table._dealerButton = 2;
            table.SetBlindPositions(2, 0);
            var p1 = table.Seats.Players[0];
            var p2 = table.Seats.Players[1];
            var p3 = table.Seats.Players[2];
            p3.AddActionHistory(ActionType.RAISE, 10, 5);
            p1.AddActionHistory(ActionType.FOLD);
            p2.AddActionHistory(ActionType.RAISE, 20, 10);
            p3.AddActionHistory(ActionType.CALL, 20);
            table.Seats.Players.ForEach(p => p.SaveStreetActionHistories(StreetType.PREFLOP));
            p3.AddActionHistory(ActionType.CALL, 5);
            p2.AddActionHistory(ActionType.RAISE, 5, 5);
            return table;
        }

        private Dictionary<string, object> SetupRoundState()
            => new Dictionary<string, object>
            {
                { "street", 1 },
                {"next_player", 2 },
                {"round_count", 3 },
                {"small_blind_amount", 4 },
                {"table", SetupTable() }
            };

    }
}
