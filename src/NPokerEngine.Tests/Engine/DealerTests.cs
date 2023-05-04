using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NPokerEngine.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NPokerEngine.Tests.Engine
{
    [TestClass]
    public class DealerTests
    {
        private Dealer _dealer;
        private MessageHandler _messageHandler;

        [TestInitialize]
        public void Initialize()
        {
            _dealer = new Dealer(5, 100);
            _messageHandler = _dealer._messageHandler;
        }

        [TestMethod]
        public void RegisterPokerPlayerTest()
        {
            var algo = new RecordMan();
            var mockDealer = new Mock<Dealer>(_dealer);
            mockDealer.Setup(m => m.FetchUuid()).Returns("a");

            mockDealer.Object.RegisterPlayer("hoge", algo);
            var player = mockDealer.Object.Table.Seats.Players[0];

            using (new AssertionScope())
            {
                player.Name.Should().Be("hoge");
                player.Stack.Should().Be(100);
                _messageHandler.algo_owner_map["a"].Should().Be(algo);
            }
        }

        [TestMethod]
        [Ignore]
        public void PublishMsgTest()
        {
            _dealer = new Dealer(1, 100);
            _dealer.Table._dealerButton = 1;
            var algos = Enumerable.Range(0,2).Select(ix => new RecordMan()).ToList();

            _dealer.RegisterPlayer("hoge", algos[0]);
            _dealer.RegisterPlayer("fuga", algos[1]);

            var players = _dealer.Table.Seats.Players;
            _dealer.StartGame(1);

            var first_player_expected = new string[] 
            {
                "receive_game_start_message",
                "receive_round_start_message",
                "receive_street_start_message",
                "declare_action",
                "receive_game_update_message",
                "receive_round_result_message"
            };
            var second_player_expected = new string[]
            {
                "receive_game_start_message",
                "receive_round_start_message",
                "receive_street_start_message",
                "receive_game_update_message",
                "receive_round_result_message"
            };

            using (new AssertionScope())
            {
                algos[0]._receivedMessages.Should().BeEquivalentTo(first_player_expected);
                algos[1]._receivedMessages.Should().BeEquivalentTo(second_player_expected);
            }
        }
        //  def test_publish_msg(self) :
        //    self.dealer = Dealer(1, 100)
        //    self.dealer.table.dealer_btn = 1
        //    self.mh = self.dealer.message_handler
        //        algos = [RecordMan() for _ in range(2)]
        //        [self.dealer.register_player(name, algo) for name, algo in zip(["hoge", "fuga"], algos)]
        //    players = self.dealer.table.seats.players
        //    _ = self.dealer.start_game(1)

        //    first_player_expected = [
        //        "receive_game_start_message",
        //        "receive_round_start_message",
        //        "receive_street_start_message",
        //        "declare_action",
        //        "receive_game_update_message",
        //        "receive_round_result_message"
        //        ]
        //        second_player_expected = [
        //            "receive_game_start_message",
        //            "receive_round_start_message",
        //            "receive_street_start_message",
        //            "receive_game_update_message",
        //            "receive_round_result_message"
        //        ]

        //    for i, expected in enumerate(first_player_expected) :
        //      self.eq(expected, algos[0].received_msgs[i])
        //    for i, expected in enumerate(second_player_expected) :
        //      self.eq(expected, algos[1].received_msgs[i])

        [TestMethod]
        [Ignore]
        public void PlayRoundTest()
        {
            using (new AssertionScope())
            {
            }
        }
        //  def test_play_a_round(self):
        //    algos = [FoldMan() for _ in range(2)]
        //        [self.dealer.register_player(name, algo) for name, algo in zip(["hoge", "fuga"], algos)]
        //    players = self.dealer.table.seats.players
        //    self.dealer.table.dealer_btn = 1
        //    summary = self.dealer.start_game(1)
        //    player_state = summary["message"]["game_information"]["seats"]
        //    self.eq(95, player_state[0]["stack"])
        //    self.eq(105, player_state[1]["stack"])

        [TestMethod]
        [Ignore]
        public void PlayTwoRoundsTest()
        {
            using (new AssertionScope())
            {
            }
        }
        //  def test_play_two_round(self):
        //    algos = [FoldMan() for _ in range(2)]
        //        [self.dealer.register_player(name, algo) for name, algo in zip(["hoge", "fuga"], algos)]
        //    players = self.dealer.table.seats.players
        //    summary = self.dealer.start_game(2)
        //    player_state = summary["message"]["game_information"]["seats"]
        //    self.eq(100, player_state[0]["stack"])
        //    self.eq(100, player_state[1]["stack"])

        [TestMethod]
        [Ignore]
        public void ExcludeShortOfMoneyPlayerTest()
        {
            using (new AssertionScope())
            {
            }
        }
        //  def test_exclude_short_of_money_player(self):
        //    algos = [FoldMan() for _ in range(7)]
        //        [self.dealer.register_player("algo-%d" % idx, algo) for idx, algo in enumerate(algos)]
        //        self.dealer.table.dealer_btn = 5
        //    # initialize stack
        //    for idx, stack in enumerate([11, 7, 9, 11, 9, 7, 100]) :
        //      self.dealer.table.seats.players[idx].stack = stack
        //    fetch_stacks = lambda res: [p["stack"] for p in res["message"]["game_information"]["seats"]]

        //    # -- NOTICE --
        //    # dealer.start_game does not change the internal table.
        //    # So running dealer.start_game twice returns same result
        //    # dealer_btn progress
        //    # round-1 => sb:player6, bb:player0
        //    # round-2 => sb:player0, bb:player3
        //    # round-3 => sb:player3, bb:player6
        //    # round-3 => sb:player6, bb:player0
        //    result = self.dealer.start_game(1)
        //    self.eq(fetch_stacks(result), [16, 7, 9, 11, 9, 7, 95])
        //    result = self.dealer.start_game(2)
        //    self.eq(fetch_stacks(result), [11, 0, 0, 16, 9, 7, 95])
        //    result = self.dealer.start_game(3)
        //    self.eq(fetch_stacks(result), [11, 0, 0, 11, 0, 0, 100])
        //    result = self.dealer.start_game(4)
        //    self.eq(fetch_stacks(result), [16, 0, 0, 11, 0, 0, 95])

        [TestMethod]
        [Ignore]
        public void ExcludeShortOfMoneyPlayerWhenAnteOnTest()
        {
            using (new AssertionScope())
            {
            }
        }
        //  def test_exclude_short_of_money_player_when_ante_on(self):
        //    dealer = Dealer(5, 100, 20)
        //    blind_structure = { 3:{"ante":30, "small_blind": 10}}
        //    dealer.set_blind_structure(blind_structure)
        //    algos = [FoldMan() for _ in range(5)]
        //    [dealer.register_player("algo-%d" % idx, algo) for idx, algo in enumerate(algos)]
        //    dealer.table.dealer_btn = 3
        //    # initialize stack
        //    for idx, stack in enumerate([1000, 30, 46, 1000, 85]) :
        //      dealer.table.seats.players[idx].stack = stack
        //    fetch_stacks = lambda res: [p["stack"] for p in res["message"]["game_information"]["seats"]]

        //    result = dealer.start_game(1)
        //    self.eq(fetch_stacks(result), [1085, 10, 26, 980, 60])
        //    result = dealer.start_game(2)
        //    self.eq(fetch_stacks(result), [1060, 0, 0, 1025, 40])
        //    result = dealer.start_game(3)
        //    self.eq(fetch_stacks(result), [1100, 0, 0, 985, 0])
        //    result = dealer.start_game(4)
        //    self.eq(fetch_stacks(result), [1060, 0, 0, 1025, 0])

        [TestMethod]
        [Ignore]
        public void ExcludeShortOfMoneyPlayerWhenAnteOn2Test()
        {
            using (new AssertionScope())
            {
            }
        }
        //  def test_exclude_short_of_money_player_when_ante_on2(self):
        //    dealer = Dealer(5, 100, 20)
        //    algos = [FoldMan() for _ in range(3)]
        //        [dealer.register_player("algo-%d" % idx, algo) for idx, algo in enumerate(algos)]
        //        dealer.table.dealer_btn = 2
        //    # initialize stack
        //    for idx, stack in enumerate([30, 25, 19]) :
        //      dealer.table.seats.players[idx].stack = stack
        //    fetch_stacks = lambda res: [p["stack"] for p in res["message"]["game_information"]["seats"]]

        //    result = dealer.start_game(1)
        //    self.eq([55, 0, 0], fetch_stacks(result))

        [TestMethod]
        [Ignore]
        public void OnlyOnePlayerIsLeftTest()
        {
            using (new AssertionScope())
            {
            }
        }
        //  def test_only_one_player_is_left(self):
        //    algos = [FoldMan() for _ in range(2)]
        //        [self.dealer.register_player(name, algo) for name, algo in zip(["hoge", "fuga"], algos)]
        //    players = self.dealer.table.seats.players
        //    players[0].stack = 14
        //    summary = self.dealer.start_game(2)

        [TestMethod]
        [Ignore]
        public void SetBlindStructureTest()
        {
            using (new AssertionScope())
            {
            }
        }
        //  def test_set_blind_structure(self):
        //    dealer = Dealer(5, 100, 3)
        //    dealer.table.dealer_btn = 2
        //    blind_structure = { 3:{"ante":7, "small_blind": 11}, 4:{"ante":13, "small_blind":30} }
        //    dealer.set_blind_structure(blind_structure)
        //    algos = [FoldMan() for _ in range(3)]
        //    [dealer.register_player("algo-%d" % idx, algo) for idx, algo in enumerate(algos)]
        //    fetch_stacks = lambda res: [p["stack"] for p in res["message"]["game_information"]["seats"]]
        //    result = dealer.start_game(1)
        //    self.eq(fetch_stacks(result), [92, 111, 97])
        //    result = dealer.start_game(2)
        //    self.eq(fetch_stacks(result), [89, 103, 108])
        //    result = dealer.start_game(3)
        //    self.eq(fetch_stacks(result), [114, 96, 90])
        //    result = dealer.start_game(4)
        //    self.eq(fetch_stacks(result), [71, 152, 77])
        //    result = dealer.start_game(5)
        //    self.eq(fetch_stacks(result), [58, 109, 133])

        class RecordMan : BasePokerPlayer
        {
            internal readonly List<string> _receivedMessages = new();

            public override Tuple<ActionType, int> DeclareAction(IEnumerable validActions, HoleCards holeCards, object roundState)
            {
                _receivedMessages.Add(nameof(DeclareAction));
                return new Tuple<ActionType, int>(ActionType.FOLD, 0);
            }

            public override void ReceiveGameStartMessage(IDictionary gameInfo)
            {
                _receivedMessages.Add(nameof(ReceiveGameStartMessage));
            }

            public override void ReceiveGameUpdateMessage(ActionType actionType, object roundState)
            {
                _receivedMessages.Add(nameof(ReceiveGameUpdateMessage));
            }

            public override void ReceiveRoundResultMessage(IEnumerable<Player> winners, object handInfo, object roundState)
            {
                _receivedMessages.Add(nameof(ReceiveRoundResultMessage));
            }

            public override void ReceiveRoundStartMessage(int roundCount, HoleCards holeCards, Seats seats)
            {
                _receivedMessages.Add(nameof(ReceiveRoundStartMessage));
            }

            public override void ReceiveStreetStartMessage(StreetType street, object roundState)
            {
                _receivedMessages.Add(nameof(ReceiveStreetStartMessage));
            }
        }
    }
}
