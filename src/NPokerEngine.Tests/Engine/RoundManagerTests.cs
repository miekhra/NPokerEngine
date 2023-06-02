using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace NPokerEngine.Tests.Engine
{
    [TestClass]
    public class RoundManagerTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            RoundManager.Instance.SetMessageBuilder(null);
            GameEvaluator.Instance.SetHandEvaluator(null);
        }

        [TestMethod]
        public void CollectBlindTest()
        {
            var (roundState, _) = StartRound();
            var players = roundState.Table.Seats.Players;
            var sbAmount = 5;

            using (new AssertionScope())
            {
                players[0].Stack.Should().Be(100 - sbAmount);
                players[1].Stack.Should().Be(100 - sbAmount*2);
                players[0].LastActionHistory.ActionType.Should().Be(ActionType.SMALL_BLIND);
                players[1].LastActionHistory.ActionType.Should().Be(ActionType.BIG_BLIND);
                players[0].PayInfo.Amount.Should().Be(sbAmount);
                players[1].PayInfo.Amount.Should().Be(sbAmount*2);
            }
        }

        [TestMethod]
        public void CollectAnteTest()
        {
            var ante = 10;
            var sbAmount = 5;
            var table = SetupTable();

            var (roundState, _) = RoundManager.Instance.StartNewRound(1, sbAmount, ante, table);
            var players = roundState.Table.Seats.Players;

            using (new AssertionScope())
            {
                players[0].Stack.Should().Be(100 - sbAmount - ante);
                players[1].Stack.Should().Be(100 - sbAmount * 2 - ante);
                players[2].Stack.Should().Be(100 - ante);
                players[0].ActionHistories[0].ActionType.Should().Be(ActionType.ANTE);
                players[1].ActionHistories[0].ActionType.Should().Be(ActionType.ANTE);
                players[2].ActionHistories[0].ActionType.Should().Be(ActionType.ANTE);
                players[0].PayInfo.Amount.Should().Be(sbAmount + ante);
                players[1].PayInfo.Amount.Should().Be(sbAmount * 2 + ante);
                players[2].PayInfo.Amount.Should().Be(ante);
                GameEvaluator.Instance.CreatePot(players)[0].Amount.Should().Be(sbAmount + sbAmount * 2 + ante * 3);
            }
        }

        [TestMethod]
        public void CollectAnteSkipLoserTest()
        {
            var ante = 10;
            var sbAmount = 5;
            var table = SetupTable();
            table.Seats.Players[2].Stack = 0;
            table.Seats.Players[2].PayInfo._status = PayInfoStatus.FOLDED;

            var (roundState, _) = RoundManager.Instance.StartNewRound(1, sbAmount, ante, table);
            var players = roundState.Table.Seats.Players;

            using (new AssertionScope())
            {
                GameEvaluator.Instance.CreatePot(players)[0].Amount.Should().Be(sbAmount + sbAmount * 2 + ante * 2);
            }
        }

        [TestMethod]
        public void DealHolecardsTest()
        {
            var (roundState, _) = StartRound();
            var players = roundState.Table.Seats.Players;

            using (new AssertionScope())
            {
                players[0].HoleCards.Should().BeEquivalentTo(new List<Card> { Card.FromId(1), Card.FromId(2) });
                players[1].HoleCards.Should().BeEquivalentTo(new List<Card> { Card.FromId(3), Card.FromId(4) });
            }
        }

        [TestMethod]
        public void MessageAfterStartRoundTest()
        {
            var messageBuilderMock = new Mock<IMessageBuilder>();
            messageBuilderMock.Setup(mock => mock.BuildRoundStartMessage(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Seats>()))
                .Returns<int, int, Seats>((roundCount, playerPos, seats) => new Dictionary<string, object> { { seats.Players[playerPos].Uuid, "hoge" } });
            messageBuilderMock.Setup(mock => mock.BuildStreetStartMessage(It.IsAny<Dictionary<string, object>>()))
                .Returns<Dictionary<string, object>>((state) => new Dictionary<string, object> { { "-1", "fuga" } });
            messageBuilderMock.Setup(mock => mock.BuildAskMessage(It.IsAny<int>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<int, Dictionary<string, object>>((playerPos, state) => new Dictionary<string, object> { { ((Table)state["table"]).Seats.Players[playerPos].Uuid, "bar" } });
            RoundManager.Instance.SetMessageBuilder(messageBuilderMock.Object);

            var messages = StartRound().messages;

            using (new AssertionScope())
            {
                messages[0].Should().Be(("uuid0", "hoge"));
                messages[1].Should().Be(("uuid1", "hoge"));
                messages[2].Should().Be(("uuid2", "hoge"));
                messages[3].Should().Be(("-1", "fuga"));
                messages[4].Should().Be(("uuid2", "bar"));
            }
        }

        [TestMethod]
        public void StateAfterStartRoundTest()
        {
            var (state, messages) = this.StartRound();

            using (new AssertionScope())
            {
                state.NextPlayerIx.Should().Be(2);
                state.Table.Seats.Players[0].ActionHistories[0].ActionType.Should().Be(ActionType.SMALL_BLIND);
                state.Table.Seats.Players[1].ActionHistories[0].ActionType.Should().Be(ActionType.BIG_BLIND);
            }
        }

        [TestMethod]
        public void MessageAfterApplyActionTest()
        {
            var messageBuilderMock = new Mock<IMessageBuilder>();
            messageBuilderMock.Setup(mock => mock.BuildRoundStartMessage(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Seats>()))
                .Returns<int, int, Seats>((roundCount, playerPos, seats) => new Dictionary<string, object> { { seats.Players[playerPos].Uuid, "hoge" } });
            messageBuilderMock.Setup(mock => mock.BuildStreetStartMessage(It.IsAny<Dictionary<string, object>>()))
                .Returns<Dictionary<string, object>>((state) => new Dictionary<string, object> { { "-1", "fuga" } });
            messageBuilderMock.Setup(mock => mock.BuildAskMessage(It.IsAny<int>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<int, Dictionary<string, object>>((playerPos, state) => new Dictionary<string, object> { { ((Table)state["table"]).Seats.Players[playerPos].Uuid, "bar" } });
            messageBuilderMock.Setup(mock => mock.BuildGameUpdateMessage(It.IsAny<int>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<int, object, object, Dictionary<string, object>>((playerPos, action, amount, state) => new Dictionary<string, object> { { nameof(IMessageBuilder.BuildGameUpdateMessage), "boo" } });
            RoundManager.Instance.SetMessageBuilder(messageBuilderMock.Object);

            var (state, _) = this.StartRound();
            var (_, msgs) = RoundManager.Instance.ApplyAction(state, "call", 10);
            using (new AssertionScope())
            {
                ((List<Tuple<string, object>>)msgs)[0].Should().BeEquivalentTo(("-1", "boo"));
                ((List<Tuple<string, object>>)msgs)[1].Should().BeEquivalentTo(("uuid0", "bar"));
            }
        }

        [TestMethod]
        public void StateAfterApplyCallTest()
        {
            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);

            using (new AssertionScope())
            {
                state.NextPlayerIx.Should().Be(0);
                state.Table.Seats.Players[2].ActionHistories[0].ActionType.Should().Be(ActionType.CALL);
            }
        }

        [TestMethod]
        public void StateAfterApplyRaiseTest()
        {
            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, "raise", 15);

            using (new AssertionScope())
            {
                state.NextPlayerIx.Should().Be(0);
                state.Table.Seats.Players[2].ActionHistories[0].ActionType.Should().Be(ActionType.RAISE);
            }
        }

        [TestMethod]
        public void MessageAfterForwardToFlopTest()
        {
            var messageBuilderMock = new Mock<IMessageBuilder>();
            messageBuilderMock.Setup(mock => mock.BuildStreetStartMessage(It.IsAny<Dictionary<string, object>>()))
                .Returns<Dictionary<string, object>>((state) => new Dictionary<string, object> { { "-1", "fuga" } });
            messageBuilderMock.Setup(mock => mock.BuildAskMessage(It.IsAny<int>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<int, Dictionary<string, object>>((playerPos, state) => new Dictionary<string, object> { { ((Table)state["table"]).Seats.Players[playerPos].Uuid, "bar" } });
            messageBuilderMock.Setup(mock => mock.BuildGameUpdateMessage(It.IsAny<int>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<int, object, object, Dictionary<string, object>>((playerPos, action, amount, state) => new Dictionary<string, object> { { nameof(IMessageBuilder.BuildGameUpdateMessage), "boo" } });
            RoundManager.Instance.SetMessageBuilder(messageBuilderMock.Object);

            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, "call", 10);

            using (new AssertionScope())
            {
                ((List<Tuple<string, object>>)msgs)[0].Should().BeEquivalentTo(("-1", "boo"));
                ((List<Tuple<string, object>>)msgs)[1].Should().BeEquivalentTo(("-1", "fuga"));
                ((List<Tuple<string, object>>)msgs)[2].Should().BeEquivalentTo(("uuid0", "bar"));
            }
        }

        [TestMethod]
        public void StateAfterForwardToFlopTest()
        {
            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);

            Player fetchPlayer(string uuid) => state.Table.Seats.Players.Single(p => p.Uuid == uuid);

            using (new AssertionScope())
            {
                state.Street.Should().Be(StreetType.FLOP);
                state.NextPlayerIx.Should().Be(0);
                state.Table.CommunityCards.Should().BeEquivalentTo(Enumerable.Range(7, 3).Select(Card.FromId));
                state.Table.Seats.Players.All(p => !p.ActionHistories.Any()).Should().BeTrue();
                fetchPlayer("uuid0").RoundActionHistories[StreetType.PREFLOP].Count.Should().Be(2);
                fetchPlayer("uuid1").RoundActionHistories[StreetType.PREFLOP].Count.Should().Be(2);
                fetchPlayer("uuid2").RoundActionHistories[StreetType.PREFLOP].Count.Should().Be(1);
                fetchPlayer("uuid0").RoundActionHistories.ContainsKey(StreetType.TURN).Should().BeFalse();
            }
        }

        [TestMethod]
        public void StateAfterForwardToTurnTest()
        {
            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, "call", 0);

            Player fetchPlayer(string uuid) => state.Table.Seats.Players.Single(p => p.Uuid == uuid);

            using (new AssertionScope())
            {
                state.Street.Should().Be(StreetType.TURN);
                state.Table.CommunityCards.Should().BeEquivalentTo(Enumerable.Range(7, 4).Select(Card.FromId));
                ((IList)msgs).Count.Should().Be(3);
                state.Table.Seats.Players.All(p => !p.ActionHistories.Any()).Should().BeTrue();
                fetchPlayer("uuid0").RoundActionHistories[StreetType.PREFLOP].Count.Should().Be(2);
                fetchPlayer("uuid1").RoundActionHistories[StreetType.PREFLOP].Count.Should().Be(2);
                fetchPlayer("uuid2").RoundActionHistories[StreetType.PREFLOP].Count.Should().Be(1);
                fetchPlayer("uuid0").RoundActionHistories[StreetType.FLOP].Count.Should().Be(1);
                fetchPlayer("uuid1").RoundActionHistories[StreetType.FLOP].Count.Should().Be(1);
                fetchPlayer("uuid2").RoundActionHistories[StreetType.FLOP].Count.Should().Be(0);
                fetchPlayer("uuid0").RoundActionHistories.ContainsKey(StreetType.TURN).Should().BeFalse();
            }
        }

        [TestMethod]
        public void StateAfterForwardToRiverTest()
        {
            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, "call", 0);

            Player fetchPlayer(string uuid) => state.Table.Seats.Players.Single(p => p.Uuid == uuid);

            using (new AssertionScope())
            {
                state.Street.Should().Be(StreetType.RIVER);
                state.Table.CommunityCards.Should().BeEquivalentTo(Enumerable.Range(7, 5).Select(Card.FromId));
                ((IList)msgs).Count.Should().Be(3);
                fetchPlayer("uuid0").RoundActionHistories[StreetType.PREFLOP].Count.Should().Be(2);
                fetchPlayer("uuid1").RoundActionHistories[StreetType.PREFLOP].Count.Should().Be(2);
                fetchPlayer("uuid2").RoundActionHistories[StreetType.PREFLOP].Count.Should().Be(1);
                fetchPlayer("uuid0").RoundActionHistories[StreetType.FLOP].Count.Should().Be(1);
                fetchPlayer("uuid1").RoundActionHistories[StreetType.FLOP].Count.Should().Be(1);
                fetchPlayer("uuid2").RoundActionHistories[StreetType.FLOP].Count.Should().Be(0);
                fetchPlayer("uuid0").RoundActionHistories[StreetType.TURN].Count.Should().Be(1);
                fetchPlayer("uuid1").RoundActionHistories[StreetType.TURN].Count.Should().Be(1);
                fetchPlayer("uuid2").RoundActionHistories[StreetType.TURN].Count.Should().Be(0);
                fetchPlayer("uuid0").RoundActionHistories.ContainsKey(StreetType.RIVER).Should().BeFalse();
            }
        }

        [TestMethod]
        public void StateAfterShowdownTest()
        {
            var messageBuilderMock = new Mock<IMessageBuilder>();
            messageBuilderMock.Setup(mock => mock.BuildStreetStartMessage(It.IsAny<Dictionary<string, object>>()))
                .Returns<Dictionary<string, object>>((state) => MessageBuilder.Instance.BuildStreetStartMessage(state));
            messageBuilderMock.Setup(mock => mock.BuildAskMessage(It.IsAny<int>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<int, Dictionary<string, object>>((playerPos, state) => MessageBuilder.Instance.BuildAskMessage(playerPos, state));
            messageBuilderMock.Setup(mock => mock.BuildRoundResultMessage(It.IsAny<object>(), It.IsAny<IEnumerable<Player>>(), It.IsAny<object>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<object, IEnumerable<Player>, object, Dictionary<string, object>>((round_count, winners, hand_info, state) => new Dictionary<string, object> { { nameof(IMessageBuilder.BuildRoundResultMessage), "bogo" } });
            RoundManager.Instance.SetMessageBuilder(messageBuilderMock.Object);

            var handEvalMock = new Mock<IHandEvaluator>();
            GameEvaluatorTests.SetupEvalHandSequence(handEvalMock, new int[] { 1, 0 }, 3);
            GameEvaluator.Instance.SetHandEvaluator(handEvalMock.Object);

            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);

            using (new AssertionScope())
            {
                state.Street.Should().Be(StreetType.FINISHED);
                state.Table.Seats.Players[0].Stack.Should().Be(110);
                state.Table.Seats.Players[1].Stack.Should().Be(90);
                state.Table.Seats.Players[02].Stack.Should().Be(100);
                state.Table.Seats.Players.Should().AllSatisfy(p => p.ActionHistories.Should().BeEmpty());

            }
        }

        [TestMethod]
        public void MessageAfterShowdownTest()
        {
            var messageBuilderMock = new Mock<IMessageBuilder>();
            messageBuilderMock.Setup(mock => mock.BuildStreetStartMessage(It.IsAny<Dictionary<string, object>>()))
                .Returns<Dictionary<string, object>>((state) => MessageBuilder.Instance.BuildStreetStartMessage(state));
            messageBuilderMock.Setup(mock => mock.BuildAskMessage(It.IsAny<int>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<int, Dictionary<string, object>>((playerPos, state) => MessageBuilder.Instance.BuildAskMessage(playerPos, state));
            messageBuilderMock.Setup(mock => mock.BuildGameUpdateMessage(It.IsAny<int>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<int, object, object, Dictionary<string, object>>((playerPos, action, amount, state) => new Dictionary<string, object> { { nameof(IMessageBuilder.BuildGameUpdateMessage), "boo" } });
            messageBuilderMock.Setup(mock => mock.BuildRoundResultMessage(It.IsAny<object>(), It.IsAny<IEnumerable<Player>>(), It.IsAny<object>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<object, IEnumerable<Player>, object, Dictionary<string, object>>((round_count, winners, hand_info, state) => new Dictionary<string, object> { { "-1", "foo" } });
            RoundManager.Instance.SetMessageBuilder(messageBuilderMock.Object);

            var handEvalMock = new Mock<IHandEvaluator>();
            GameEvaluatorTests.SetupEvalHandSequence(handEvalMock, new int[] { 1, 0 }, 3);
            GameEvaluator.Instance.SetHandEvaluator(handEvalMock.Object);

            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, "call", 0);

            using (new AssertionScope())
            {
                ((List<Tuple<string, object>>)msgs)[0].Should().BeEquivalentTo(("-1", "boo"));
                ((List<Tuple<string, object>>)msgs)[1].Should().BeEquivalentTo(("-1", "foo"));
            }
        }

        [TestMethod]
        public void TableResetAfterShowdownTest()
        {
            var messageBuilderMock = new Mock<IMessageBuilder>();
            messageBuilderMock.Setup(mock => mock.BuildStreetStartMessage(It.IsAny<Dictionary<string, object>>()))
                .Returns<Dictionary<string, object>>((state) => MessageBuilder.Instance.BuildStreetStartMessage(state));
            messageBuilderMock.Setup(mock => mock.BuildAskMessage(It.IsAny<int>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<int, Dictionary<string, object>>((playerPos, state) => MessageBuilder.Instance.BuildAskMessage(playerPos, state));
            messageBuilderMock.Setup(mock => mock.BuildGameUpdateMessage(It.IsAny<int>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<int, object, object, Dictionary<string, object>>((playerPos, action, amount, state) => new Dictionary<string, object> { { nameof(IMessageBuilder.BuildGameUpdateMessage), "boo" } });
            messageBuilderMock.Setup(mock => mock.BuildRoundResultMessage(It.IsAny<object>(), It.IsAny<IEnumerable<Player>>(), It.IsAny<object>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<object, IEnumerable<Player>, object, Dictionary<string, object>>((round_count, winners, hand_info, state) => new Dictionary<string, object> { { "-1", "foo" } });
            RoundManager.Instance.SetMessageBuilder(messageBuilderMock.Object);

            var handEvalMock = new Mock<IHandEvaluator>();
            GameEvaluatorTests.SetupEvalHandSequence(handEvalMock, new int[] { 1, 0 }, 3);
            GameEvaluator.Instance.SetHandEvaluator(handEvalMock.Object);

            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);

            using (new AssertionScope())
            {
                state.Table.Deck.Size.Should().Be(52);
                state.Table.CommunityCards.Should().BeNullOrEmpty();
                state.Table.Seats.Players[0].HoleCards.Should().BeNullOrEmpty();
                state.Table.Seats.Players[0].ActionHistories.Should().BeNullOrEmpty();
                state.Table.Seats.Players[0].PayInfo.Status.Should().Be(PayInfoStatus.PAY_TILL_END);
            }
        }

        [TestMethod]
        public void MessageSkipWhenOnlyOnePlayerIsActiveTest()
        {
            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, "fold", 0);

            using (new AssertionScope())
            {
                state.Street.Should().Be(StreetType.FINISHED);
                ((List<object>)msgs).Where(m => m is KeyValuePair<string, object>)
                    .Select(m => (KeyValuePair<string, object>)m)
                    .Where(m => m.Key == "message")
                    .Select(m => m.Value as Dictionary<string, object>)
                    .Where(m => m.ContainsKey("message_type"))
                    .Should()
                    .AllSatisfy(m => m["message_type"].Should().NotBe("street_start_message"));
            }
        }

        [TestMethod]
        public void AskPlayerTargetWhenDealerBtnPlayerIsFoldedTest()
        {
            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, "call", 0);

            using (new AssertionScope())
            {
                ((KeyValuePair<string, object>)((List<object>)msgs).Last()).Key.Should().Be("uuid1");
            }
        }

        [TestMethod]
        public void SkipAskingToAllinPlayerTest()
        {
            var (state, _) = this.StartRound();

            // Round 1
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "raise", 50);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 50);
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            var stacksAfterRound1 = state.Table.Seats.Players.Select(p => p.Stack).ToList();

            // Round 1
            state.Table.ShiftDealerButton();
            state.Table.SetBlindPositions(1, 2);
            (state, _) = RoundManager.Instance.StartNewRound(2, 5, 0, state.Table);
            (state, _) = RoundManager.Instance.ApplyAction(state, "raise", 40);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 40);
            (state, _) = RoundManager.Instance.ApplyAction(state, "raise", 70);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, "call", 70);
            var stacksAfterRound2 = state.Table.Seats.Players.Select(p => p.Stack).ToList();

            using (new AssertionScope())
            {
                stacksAfterRound1.Should().BeEquivalentTo(new List<float> { 95, 40, 165 });
                stacksAfterRound2.Should().BeEquivalentTo(new List<float> { 25, 0, 95 });
                state.Street.Should().Be(StreetType.FLOP);
                ((KeyValuePair<string, object>)((List<object>)msgs).Last()).Key.Should().Be("uuid2");
            }
        }

        [TestMethod]
        public void WhenOnlyOnePlayerIsWaitingAskTest()
        {
            var (state, _) = this.StartRound();

            // Round 1
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "raise", 50);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 50);
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            var stacksAfterRound1 = state.Table.Seats.Players.Select(p => p.Stack).ToList();

            // Round 2
            //state.Table.ShiftDealerButton();
            //(state, _) = RoundManager.Instance.StartNewRound(2, 5, 0, state.Table);
            //(state, _) = RoundManager.Instance.ApplyAction(state, "raise", 40);
            //(state, _) = RoundManager.Instance.ApplyAction(state, "call", 40);
            //(state, _) = RoundManager.Instance.ApplyAction(state, "raise", 70);
            //(state, _) = RoundManager.Instance.ApplyAction(state, "call", 70);
            //(state, _) = RoundManager.Instance.ApplyAction(state, "call", 0);
            //(state, _) = RoundManager.Instance.ApplyAction(state, "raise", 10);
            //(state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            //(state, _) = RoundManager.Instance.ApplyAction(state, "raise", 85);
            //(state, _) = RoundManager.Instance.ApplyAction(state, "call", 85);

            using (new AssertionScope())
            {
                stacksAfterRound1.Should().BeEquivalentTo(new List<float> { 95, 40, 165 });
            }
        }

        [TestMethod]
        public void AskBigBlindInPreflopTest()
        {
            var (state, _) = this.StartRound();

            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 10);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, "call", 10);

            using (new AssertionScope())
            {
                state.Street.Should().Be(StreetType.PREFLOP);
                ((ValueTuple<string, Dictionary<string, object>>)((List<object>)msgs).Last()).Item1.Should().Be("uuid1");
            }
        }

        [TestMethod]
        public void EveryoneAgreeLogicRegressionTest()
        {
            var players = Enumerable.Range(0, 4).Select(ix => new Player($"uuid{ix}", 100)).ToList();
            players[0].Stack = 150;
            players[1].Stack = 150;
            players[2].Stack = 50;
            players[3].Stack = 50;

            var deck = new Deck(cheat: true, cheatCardIds: Enumerable.Range(1, 52).ToList());
            var table = new Table(cheatDeck: deck);
            players.ForEach(p => table.Seats.Sitdown(p));
            table._dealerButton = 3;
            table.SetBlindPositions(0, 1);

            var (state, _) = RoundManager.Instance.StartNewRound(1, 5, 0, table);
            (state, _) = RoundManager.Instance.ApplyAction(state, "raise", 15);
            (state, _) = RoundManager.Instance.ApplyAction(state, "raise", 20);
            (state, _) = RoundManager.Instance.ApplyAction(state, "raise", 25);
            (state, _) = RoundManager.Instance.ApplyAction(state, "raise", 30);
            (state, _) = RoundManager.Instance.ApplyAction(state, "raise", 50);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 50);
            (state, _) = RoundManager.Instance.ApplyAction(state, "raise", 125);
            (state, _) = RoundManager.Instance.ApplyAction(state, "call", 125);
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);

            using (new AssertionScope())
            {
                state.Street.Should().Be(StreetType.FINISHED);
            }
        }

        [TestMethod]
        [Ignore("understandable test flow")]
        public void AddAmountCalculationWhenRaiseOnAnteTest()
        {
            var table = this.SetupTable();
            Func<GameState, object> potAmount = state 
                => GameEvaluator.Instance.CreatePot(state.Table.Seats.Players)[0].Amount;
            
            var (state, _) = RoundManager.Instance.StartNewRound(1, 10, 5, table);

            using (new AssertionScope())
            {
                potAmount(state).Should().Be(45);
                state.Table.Seats.Players.Select(p => p.Stack).Should().BeEquivalentTo(new List<float> { 85, 75, 95 });

                var (folded_state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
                var (called_state, _) = RoundManager.Instance.ApplyAction(state, "call", 20);

                potAmount(called_state).Should().Be(55);
                //((Table)state["table"]).Seats.Players.Select(p => p.Stack).Should().BeEquivalentTo(new List<float> { 85, 75, 95 });
                (called_state, _) = RoundManager.Instance.ApplyAction(state, "call", 20);

                called_state.Table.Seats.Players[2].ActionHistories.Last().Paid.Should().Be(20);
            }
        }
        //  def test_add_amount_calculationl_when_raise_on_ante(self) :
        //    table = self.__setup_table()
        //    pot_amount = lambda state: GameEvaluator.create_pot(state["table"].seats.players)[0]
        //        ["amount"]
        //        stack_check = lambda expected, state: self.eq(expected, [p.stack for p in state["table"].seats.players])
        //        start_state, _ = RoundManager.start_new_round(1, 10, 5, table)
        //    self.eq(45, pot_amount(start_state))
        //    stack_check([85, 75, 95], start_state)
        //    folded_state, _ = RoundManager.apply_action(start_state, "fold", 0)
        //    called_state, _ = RoundManager.apply_action(folded_state, "call", 20)
        //    self.eq(55, pot_amount(called_state))
        //    stack_check([85, 75, 95], start_state)

        //    called_state, _ = RoundManager.apply_action(start_state, "call", 20)
        //    self.eq(20, called_state["table"].seats.players[2].action_histories[-1]["paid"])
        //    self.eq(65, pot_amount(called_state))
        //    raised_state, _ = RoundManager.apply_action(start_state, "raise", 30)
        //    self.eq(30, raised_state["table"].seats.players[2].action_histories[-1]["paid"])
        //    self.eq(75, pot_amount(raised_state))

        [TestMethod]
        public void DeepcopyStateTest()
        {
            var table = this.SetupTable();

            var original = RoundManager.Instance.GenerateInitialState(2, 5, table);
            var copy = RoundManager.Instance.DeepCopyState(original);

            using (new AssertionScope())
            {
                original.RoundCount.Should().Be(copy.RoundCount);
                original.SmallBlindAmount.Should().Be(copy.SmallBlindAmount);
                original.Street.Should().Be(copy.Street);
                original.NextPlayerIx.Should().Be(copy.NextPlayerIx);
            }
        }

        private (GameState roundState, List<(string, object)> messages) StartRound()
        {
            var table = SetupTable();
            return RoundManager.Instance.StartNewRound(
                round_count: 1, 
                small_blind_amount: 5, 
                ante_amount: 0, 
                table: table);
        }

        private Table SetupTable()
        {
            var players = Enumerable.Range(0, 3).Select(ix => new Player($"uuid{ix}", 100)).ToList();
            var deck = new Deck(cheat: true, cheatCardIds: Enumerable.Range(1,52).ToList());
            var table = new Table(cheatDeck: deck);
            players.ForEach(p => table.Seats.Sitdown(p));
            table._dealerButton = 2;
            table.SetBlindPositions(0, 1);
            return table;
        }
    }
}
