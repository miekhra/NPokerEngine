using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NPokerEngine.Messages;
using System.Collections;

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
        public void PublishMsgTest()
        {
            _dealer = new Dealer(1, 100);
            _dealer.Table._dealerButton = 1;
            var algos = Enumerable.Range(0, 2).Select(ix => new RecordMan()).ToList();

            _dealer.RegisterPlayer("hoge", algos[0]);
            _dealer.RegisterPlayer("fuga", algos[1]);

            var players = _dealer.Table.Seats.Players;
            _dealer.StartGame(1);

            var first_player_expected = new string[]
            {
                nameof(BasePokerPlayer.ReceiveGameStartMessage),
                nameof(BasePokerPlayer.ReceiveRoundStartMessage),
                nameof(BasePokerPlayer.ReceiveStreetStartMessage),
                nameof(BasePokerPlayer.DeclareAction),
                nameof(BasePokerPlayer.ReceiveGameUpdateMessage),
                nameof(BasePokerPlayer.ReceiveRoundResultMessage)
            };
            var second_player_expected = new string[]
            {
                nameof(BasePokerPlayer.ReceiveGameStartMessage),
                nameof(BasePokerPlayer.ReceiveRoundStartMessage),
                nameof(BasePokerPlayer.ReceiveStreetStartMessage),
                nameof(BasePokerPlayer.ReceiveGameUpdateMessage),
                nameof(BasePokerPlayer.ReceiveRoundResultMessage)
            };

            algos.Should().SatisfyRespectively(
                first => first._receivedMessages.Should().BeEquivalentTo(first_player_expected),
                second => second._receivedMessages.Should().BeEquivalentTo(second_player_expected)
                );
        }

        [TestMethod]
        public void PlayRoundTest()
        {
            var algos = Enumerable.Range(0, 2).Select(ix => new RecordMan()).ToList();

            _dealer.RegisterPlayer("hoge", algos[0]);
            _dealer.RegisterPlayer("fuga", algos[1]);

            var players = _dealer.Table.Seats.Players;
            _dealer.Table._dealerButton = 1;
            var (result, message) = _dealer.StartGame(1);

            using (new AssertionScope())
            {
                result.Seats.Players[0].Stack.Should().Be(95);
                result.Seats.Players[1].Stack.Should().Be(105);
            }
        }

        [TestMethod]
        public void PlayTwoRoundsTest()
        {
            var algos = Enumerable.Range(0, 2).Select(ix => new RecordMan()).ToList();

            _dealer.RegisterPlayer("hoge", algos[0]);
            _dealer.RegisterPlayer("fuga", algos[1]);

            var players = _dealer.Table.Seats.Players;
            _dealer.Table._dealerButton = 1;
            var (result, message) = _dealer.StartGame(2);

            using (new AssertionScope())
            {
                result.Seats.Players[0].Stack.Should().Be(100);
                result.Seats.Players[1].Stack.Should().Be(100);
            }
        }

        [TestMethod]
        public void ExcludeShortOfMoneyPlayerTest()
        {
            var algos = Enumerable.Range(0, 7).Select(ix => new RecordMan()).ToList();
            for (int ix = 0; ix < algos.Count; ix++)
            {
                _dealer.RegisterPlayer($"algo-{ix}", algos[ix]);
            }
            _dealer.Table._dealerButton = 5;

            // initialize stack
            var stacks = new float[] { 11, 7, 9, 11, 9, 7, 100 };
            for (int ix = 0; ix < stacks.Length; ix++)
            {
                _dealer.Table.Seats[ix].Stack = stacks[ix];
            }

            Func<GameResultMessage, IEnumerable<float>> fecthStacks = result => result.Seats.Players.Select(p => p.Stack).ToList();

            // -- NOTICE --
            // dealer.start_game does not change the internal table.
            // So running dealer.start_game twice returns same result
            // dealer_btn progress
            // round-1 => sb:player6, bb:player0
            // round-2 => sb:player0, bb:player3
            // round-3 => sb:player3, bb:player6
            // round-3 => sb:player6, bb:player0
            var (result1, _) = _dealer.StartGame(1);
            var (result2, _) = _dealer.StartGame(2);
            var (result3, _) = _dealer.StartGame(3);
            var (result4, _) = _dealer.StartGame(4);

            using (new AssertionScope())
            {
                fecthStacks(result1).Should().BeEquivalentTo(new float[] { 16, 7, 9, 11, 9, 7, 95 });
                fecthStacks(result2).Should().BeEquivalentTo(new float[] { 11, 0, 0, 16, 9, 7, 95 });
                fecthStacks(result3).Should().BeEquivalentTo(new float[] { 11, 0, 0, 11, 0, 0, 100 });
                fecthStacks(result4).Should().BeEquivalentTo(new float[] { 16, 0, 0, 11, 0, 0, 95 });
            }
        }

        [TestMethod]
        public void ExcludeShortOfMoneyPlayerWhenAnteOnTest()
        {
            _dealer = new Dealer(5, 100, 20);
            _dealer.SetBlindStructure(new Dictionary<object, object> { { 3, new Dictionary<string, float> { { "ante", 30 }, { "small_blind", 10 } } } });
            var algos = Enumerable.Range(0, 5).Select(ix => new RecordMan()).ToList();
            for (int ix = 0; ix < algos.Count; ix++)
            {
                _dealer.RegisterPlayer($"algo-{ix}", algos[ix]);
            }
            _dealer.Table._dealerButton = 3;

            // initialize stack
            var stacks = new float[] { 1000, 30, 46, 1000, 85 };
            for (int ix = 0; ix < stacks.Length; ix++)
            {
                _dealer.Table.Seats[ix].Stack = stacks[ix];
            }

            Func<GameResultMessage, IEnumerable<float>> fecthStacks = result => result.Seats.Players.Select(p => p.Stack).ToList();

            var (result1, _) = _dealer.StartGame(1);
            var (result2, _) = _dealer.StartGame(2);
            var (result3, _) = _dealer.StartGame(3);
            var (result4, _) = _dealer.StartGame(4);

            using (new AssertionScope())
            {
                fecthStacks(result1).Should().BeEquivalentTo(new float[] { 1085, 10, 26, 980, 60 });
                fecthStacks(result2).Should().BeEquivalentTo(new float[] { 1060, 0, 0, 1025, 40 });
                fecthStacks(result3).Should().BeEquivalentTo(new float[] { 1100, 0, 0, 985, 0 });
                fecthStacks(result4).Should().BeEquivalentTo(new float[] { 1060, 0, 0, 1025, 0 });
            }
        }

        [TestMethod]
        public void ExcludeShortOfMoneyPlayerWhenAnteOn2Test()
        {
            _dealer = new Dealer(5, 100, 20);
            var algos = Enumerable.Range(0, 3).Select(ix => new RecordMan()).ToList();
            for (int ix = 0; ix < algos.Count; ix++)
            {
                _dealer.RegisterPlayer($"algo-{ix}", algos[ix]);
            }
            _dealer.Table._dealerButton = 2;

            // initialize stack
            var stacks = new float[] { 30, 25, 19 };
            for (int ix = 0; ix < stacks.Length; ix++)
            {
                _dealer.Table.Seats[ix].Stack = stacks[ix];
            }

            Func<GameResultMessage, IEnumerable<float>> fecthStacks = result => result.Seats.Players.Select(p => p.Stack).ToList();

            var (result, _) = _dealer.StartGame(1);

            fecthStacks(result).Should().BeEquivalentTo(new float[] { 55, 0, 0 });
        }

        //[TestMethod]
        //[Ignore]
        //public void OnlyOnePlayerIsLeftTest()
        //{
        //    using (new AssertionScope())
        //    {
        //    }
        //}
        //  def test_only_one_player_is_left(self):
        //    algos = [FoldMan() for _ in range(2)]
        //        [self.dealer.register_player(name, algo) for name, algo in zip(["hoge", "fuga"], algos)]
        //    players = self.dealer.table.seats.players
        //    players[0].stack = 14
        //    summary = self.dealer.start_game(2)

        [TestMethod]
        public void SetBlindStructureTest()
        {
            _dealer = new Dealer(5, 100, 3);
            _dealer.SetBlindStructure(new Dictionary<object, object>
            {
                { 3, new Dictionary<string, float> { { "ante", 7 }, { "small_blind", 11 } } },
                { 4, new Dictionary<string, float> { { "ante", 13 }, { "small_blind", 30 } } }
            });
            var algos = Enumerable.Range(0, 3).Select(ix => new RecordMan()).ToList();
            for (int ix = 0; ix < algos.Count; ix++)
            {
                _dealer.RegisterPlayer($"algo-{ix}", algos[ix]);
            }
            _dealer.Table._dealerButton = 2;

            Func<GameResultMessage, IEnumerable<float>> fecthStacks = result => result.Seats.Players.Select(p => p.Stack).ToList();

            var (result1, _) = _dealer.StartGame(1);
            var (result2, _) = _dealer.StartGame(2);
            var (result3, _) = _dealer.StartGame(3);
            var (result4, _) = _dealer.StartGame(4);
            var (result5, _) = _dealer.StartGame(5);

            using (new AssertionScope())
            {
                fecthStacks(result1).Should().BeEquivalentTo(new float[] { 92, 111, 97 });
                fecthStacks(result2).Should().BeEquivalentTo(new float[] { 89, 103, 108 });
                fecthStacks(result3).Should().BeEquivalentTo(new float[] { 114, 96, 90 });
                fecthStacks(result4).Should().BeEquivalentTo(new float[] { 71, 152, 77 });
                fecthStacks(result5).Should().BeEquivalentTo(new float[] { 58, 109, 133 });
            }
        }

        class RecordMan : BasePokerPlayer
        {
            internal readonly List<string> _receivedMessages = new();

            public override Tuple<ActionType, int> DeclareAction(IEnumerable validActions, HoleCards holeCards, object roundState)
            {
                _receivedMessages.Add(nameof(DeclareAction));
                return new Tuple<ActionType, int>(ActionType.FOLD, 0);
            }

            public override void ReceiveGameStartMessage(GameStartMessage gameStartMessage)
            {
                _receivedMessages.Add(nameof(ReceiveGameStartMessage));
            }

            public override void ReceiveGameUpdateMessage(GameUpdateMessage gameUpdateMessage)
            {
                _receivedMessages.Add(nameof(ReceiveGameUpdateMessage));
            }

            public override void ReceiveRoundResultMessage(RoundResultMessage roundResultMessage)
            {
                _receivedMessages.Add(nameof(ReceiveRoundResultMessage));
            }

            public override void ReceiveRoundStartMessage(RoundStartMessage roundStartMessage)
            {
                _receivedMessages.Add(nameof(ReceiveRoundStartMessage));
            }

            public override void ReceiveStreetStartMessage(StreetStartMessage streetStartMessage)
            {
                _receivedMessages.Add(nameof(ReceiveStreetStartMessage));
            }
        }
    }
}
