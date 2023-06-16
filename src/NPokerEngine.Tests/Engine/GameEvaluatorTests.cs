using FluentAssertions;
using FluentAssertions.Execution;
using Moq;

namespace NPokerEngine.Tests.Engine
{
    [TestClass]
    public class GameEvaluatorTests
    {
        private GameEvaluator _gameEvaluator = GameEvaluator.Instance;

        [TestInitialize]
        public void Initialize()
        {
            HandEvaluatorResolver.ResoreDefault();
        }

        [TestCleanup]
        public void Cleanup()
        {
            HandEvaluator.Instance._evalFunc = null;
        }

        [TestMethod]
        public void JudgeWithoutAllinTest()
        {
            var players = Enumerable.Range(0, 3).Select(ix => CreatePlayerWithPayInfo(ix.ToString(), 5, PayInfoStatus.PAY_TILL_END)).ToList();
            var table = SetupTable(players);

            var handEvalMock = new Mock<IHandEvaluator>();
            SetupEvalHandSequence(handEvalMock, new int[] { 0, 1, 0 }, 3);
            HandEvaluatorResolver.Register(handEvalMock.Object);

            var judgeResult = _gameEvaluator.Judge(table);

            using (new AssertionScope())
            {
                judgeResult.winners.Count.Should().Be(1);
                judgeResult.winners.Should().Contain(players[1]);
                judgeResult.prizeMap[1].Should().Be(15);
            }
        }

        [TestMethod]
        public void JudjeWithoutAllinButWinnerFoldedTest()
        {
            var players = Enumerable.Range(0, 3).Select(ix => CreatePlayerWithPayInfo(ix.ToString(), 5, PayInfoStatus.PAY_TILL_END)).ToList();
            players[1].PayInfo.UpdateToFold();
            var table = SetupTable(players);

            var handEvalMock = new Mock<IHandEvaluator>();
            SetupEvalHandSequence(handEvalMock, new int[] { 0, 0 }, 4);
            HandEvaluatorResolver.Register(handEvalMock.Object);

            var judgeResult = _gameEvaluator.Judge(table);

            using (new AssertionScope())
            {
                judgeResult.winners.Count.Should().Be(2);
                judgeResult.handInfoMap.ElementAt(0).Value.HandStrength.Should().Be(HandRankType.HIGHCARD);
                judgeResult.handInfoMap.ElementAt(1).Value.HandStrength.Should().Be(HandRankType.HIGHCARD);
                //((IDictionary)(((IDictionary)(judgeResult.handInfo[0]["hand"]))["hand"]))["strength"].Should().Be("HIGHCARD");
                //((IDictionary)(((IDictionary)(judgeResult.handInfo[1]["hand"]))["hand"]))["strength"].Should().Be("HIGHCARD");
                judgeResult.prizeMap[0].Should().Be(7.5f);
                judgeResult.prizeMap[1].Should().Be(0);
                judgeResult.prizeMap[2].Should().Be(7.5f);
            }
        }

        //""" B win (hand rank = B > C > A) """
        [TestMethod]
        public void JudjeWithAllinWhenAllinWinsCase1Test()
        {
            var players = SetupPlayersForJudge();
            var table = SetupTable(players);

            var handEvalMock = new Mock<IHandEvaluator>();
            SetupEvalHandSequence(handEvalMock, new int[] { 0, 2, 1 }, 6);
            HandEvaluatorResolver.Register(handEvalMock.Object);

            var judgeResult = _gameEvaluator.Judge(table);

            using (new AssertionScope())
            {
                judgeResult.handInfoMap.ElementAt(0).Value.HoleLow.Should().Be(0);
                judgeResult.handInfoMap.ElementAt(1).Value.HoleLow.Should().Be(2);
                judgeResult.handInfoMap.ElementAt(2).Value.HoleLow.Should().Be(1);
                //((IDictionary)(((IDictionary)(((IDictionary)(judgeResult.handInfo[0]))["hand"]))["hole"]))["low"].Should().Be(0);
                //((IDictionary)(((IDictionary)(((IDictionary)(judgeResult.handInfo[1]))["hand"]))["hole"]))["low"].Should().Be(2);
                //((IDictionary)(((IDictionary)(((IDictionary)(judgeResult.handInfo[2]))["hand"]))["hole"]))["low"].Should().Be(1);
                judgeResult.prizeMap[0].Should().Be(20);
                judgeResult.prizeMap[1].Should().Be(60);
                judgeResult.prizeMap[2].Should().Be(20);
            }
        }

        //""" B win (hand rank = B > A > C) """
        [TestMethod]
        public void JudjeWithAllinWhenAllinWinsCase2Test()
        {
            var players = SetupPlayersForJudge();
            var table = SetupTable(players);

            var handEvalMock = new Mock<IHandEvaluator>();
            SetupEvalHandSequence(handEvalMock, new int[] { 1, 2, 0 }, multiplier: 3, extra: new int[] { 1, 0, 0 });
            HandEvaluatorResolver.Register(handEvalMock.Object);

            var judgeResult = _gameEvaluator.Judge(table);

            using (new AssertionScope())
            {
                judgeResult.handInfoMap.ElementAt(0).Value.HoleLow.Should().Be(1);
                judgeResult.handInfoMap.ElementAt(1).Value.HoleLow.Should().Be(2);
                judgeResult.handInfoMap.ElementAt(2).Value.HoleLow.Should().Be(0);
                //((IDictionary)(((IDictionary)(((IDictionary)(judgeResult.handInfo[0]))["hand"]))["hole"]))["low"].Should().Be(1);
                //((IDictionary)(((IDictionary)(((IDictionary)(judgeResult.handInfo[1]))["hand"]))["hole"]))["low"].Should().Be(2);
                //((IDictionary)(((IDictionary)(((IDictionary)(judgeResult.handInfo[2]))["hand"]))["hole"]))["low"].Should().Be(0);
                judgeResult.prizeMap[0].Should().Be(40);
                judgeResult.prizeMap[1].Should().Be(60);
                judgeResult.prizeMap[2].Should().Be(0);
            }
        }

        [TestMethod]
        public void JudjeWithAllinWhenAllinDoesNotWinTest()
        {
            var players = SetupPlayersForJudge();
            var table = SetupTable(players);

            var handEvalMock = new Mock<IHandEvaluator>();
            SetupEvalHandSequence(handEvalMock, new int[] { 2, 1, 0 }, 3, extra: new int[] { 2, 0, 2 });
            HandEvaluatorResolver.Register(handEvalMock.Object);

            var judgeResult = _gameEvaluator.Judge(table);

            using (new AssertionScope())
            {
                judgeResult.handInfoMap.ElementAt(0).Value.HoleLow.Should().Be(2);
                judgeResult.handInfoMap.ElementAt(1).Value.HoleLow.Should().Be(1);
                judgeResult.handInfoMap.ElementAt(2).Value.HoleLow.Should().Be(0);
                //((IDictionary)(((IDictionary)(((IDictionary)(judgeResult.handInfo[0]))["hand"]))["hole"]))["low"].Should().Be(2);
                //((IDictionary)(((IDictionary)(((IDictionary)(judgeResult.handInfo[1]))["hand"]))["hole"]))["low"].Should().Be(1);
                //((IDictionary)(((IDictionary)(((IDictionary)(judgeResult.handInfo[2]))["hand"]))["hole"]))["low"].Should().Be(0);
                judgeResult.prizeMap[0].Should().Be(100);
                judgeResult.prizeMap[1].Should().Be(0);
                judgeResult.prizeMap[2].Should().Be(0);
            }
        }

        [TestMethod]
        public void FindWinnerTest()
        {
            var players = SetupPlayers();
            var dummyCommunity = new List<Card>();

            var handEvalMock = new Mock<IHandEvaluator>();
            SetupEvalHandSequence(handEvalMock, new int[] { 0, 1, 0 });
            HandEvaluatorResolver.Register(handEvalMock.Object);

            var winners = _gameEvaluator.FindWinnersFrom(players, dummyCommunity);

            using (new AssertionScope())
            {
                winners.Should().ContainSingle();
                winners.Should().Contain(players[1]);
            }
        }

        [TestMethod]
        public void FindWinnersTest()
        {
            var players = SetupPlayers();
            var dummyCommunity = new List<Card>();

            var handEvalMock = new Mock<IHandEvaluator>();
            SetupEvalHandSequence(handEvalMock, new int[] { 0, 1, 1 });
            HandEvaluatorResolver.Register(handEvalMock.Object);

            var winners = _gameEvaluator.FindWinnersFrom(players, dummyCommunity);

            using (new AssertionScope())
            {
                winners.Count.Should().Be(2);
                winners.Should().Contain(players[1]);
                winners.Should().Contain(players[2]);
            }
        }

        private Table SetupTable(IEnumerable<Player> players)
        {
            var table = new Table();
            players.ToList().ForEach(p => table.Seats.Sitdown(p));
            return table;
        }

        private Player[] SetupPlayers()
        {
            return Enumerable.Range(0, 3).Select(t => new Player("uuid", 100, name: t.ToString())).ToArray();
        }

        private IEnumerable<Player> SetupPlayersForJudge()
        {
            return new Player[]
            {
                CreatePlayerWithPayInfo("A", 50, PayInfoStatus.PAY_TILL_END),
                CreatePlayerWithPayInfo("B", 20, PayInfoStatus.ALLIN),
                CreatePlayerWithPayInfo("C", 30, PayInfoStatus.ALLIN)
            };
        }

        private Player CreatePlayerWithPayInfo(string name, float amount, PayInfoStatus status)
        {
            var player = new Player($"uuid{name}", 100, name);
            player.PayInfo._amount = amount;
            player.PayInfo._status = status;
            return player;
        }

        internal static void SetupEvalHandSequence(Mock<IHandEvaluator> evaluatorMock, int[] returns, int multiplier = 1, IEnumerable<int> extra = null)
        {
            var returnSequence = returns.ToList();
            for (int ix = 1; ix < multiplier; ix++)
            {
                returnSequence = returnSequence.Concat(returns).ToList();
            }
            if (extra != null)
                returnSequence = returnSequence.Concat(extra).ToList();

            var seqSetup = evaluatorMock.SetupSequence(handEval => handEval.EvalHand(It.IsAny<IEnumerable<Card>>(), It.IsAny<IEnumerable<Card>>()));
            returnSequence.ForEach(ix => seqSetup.Returns(ix));
            seqSetup.Throws(new NotImplementedException());
            HandEvaluator.Instance._evalFunc = evaluatorMock.Object.EvalHand;
            evaluatorMock
                .Setup(handEval => handEval.GenHandRankInfo(It.IsAny<IEnumerable<Card>>(), It.IsAny<IEnumerable<Card>>()))
                .Returns<IEnumerable<Card>, IEnumerable<Card>>((hole, community) => HandEvaluator.Instance.GenHandRankInfo(hole, community));
        }
    }
}
