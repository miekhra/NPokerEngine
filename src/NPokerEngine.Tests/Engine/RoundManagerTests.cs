﻿using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NPokerEngine.Messages;
using System.Collections;

namespace NPokerEngine.Tests.Engine
{
    [TestClass]
    public class RoundManagerTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            HandEvaluatorResolver.ResoreDefault();
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
                players[1].Stack.Should().Be(100 - sbAmount * 2);
                players[0].LastActionHistory.ActionType.Should().Be(ActionType.SMALL_BLIND);
                players[1].LastActionHistory.ActionType.Should().Be(ActionType.BIG_BLIND);
                players[0].PayInfo.Amount.Should().Be(sbAmount);
                players[1].PayInfo.Amount.Should().Be(sbAmount * 2);
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
            var messages = StartRound().messages;

            messages.Should().SatisfyRespectively(
                    first => first.Should().BeEquivalentTo(new RoundStartMessage { PlayerUuid = "uuid0" }, options => options.Including(m => m.PlayerUuid)),
                    second => second.Should().BeEquivalentTo(new RoundStartMessage { PlayerUuid = "uuid1" }, options => options.Including(m => m.PlayerUuid)),
                    third => third.Should().BeEquivalentTo(new RoundStartMessage { PlayerUuid = "uuid2" }, options => options.Including(m => m.PlayerUuid)),
                    fourth => fourth.Should().BeEquivalentTo(new StreetStartMessage { Street = StreetType.PREFLOP }, options => options.Including(m => m.Street)),
                    fifth => fifth.Should().BeEquivalentTo(new AskMessage { PlayerUuid = "uuid2" }, options => options.Including(m => m.PlayerUuid))
                );
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
            var (state, _) = this.StartRound();
            var (_, msgs) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            msgs.Should().SatisfyRespectively(
                    first => first.Should().BeOfType<GameUpdateMessage>(),
                    second => second.Should().BeEquivalentTo(new AskMessage { PlayerUuid = "uuid0" }, options => options.Including(m => m.PlayerUuid))
                );
        }

        [TestMethod]
        public void StateAfterApplyCallTest()
        {
            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);

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
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 15);

            using (new AssertionScope())
            {
                state.NextPlayerIx.Should().Be(0);
                state.Table.Seats.Players[2].ActionHistories[0].ActionType.Should().Be(ActionType.RAISE);
            }
        }

        [TestMethod]
        public void MessageAfterForwardToFlopTest()
        {
            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);

            msgs.Should().SatisfyRespectively(
                    first => first.Should().BeOfType<GameUpdateMessage>(),
                    second => second.Should().BeOfType<StreetStartMessage>(),
                    third => third.Should().BeOfType<AskMessage>()
                );
        }

        [TestMethod]
        public void StateAfterForwardToFlopTest()
        {
            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);

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
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);

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
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);

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
            var handEvalMock = new Mock<IHandEvaluator>();
            GameEvaluatorTests.SetupEvalHandSequence(handEvalMock, new int[] { 1, 0 }, 3);
            HandEvaluatorResolver.Register(handEvalMock.Object);

            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);

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
            var handEvalMock = new Mock<IHandEvaluator>();
            GameEvaluatorTests.SetupEvalHandSequence(handEvalMock, new int[] { 1, 0 }, 3);
            HandEvaluatorResolver.Register(handEvalMock.Object);

            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);

            msgs.Should().SatisfyRespectively(
                    first => first.Should().BeOfType<GameUpdateMessage>(),
                    second => second.Should().BeOfType<RoundResultMessage>()
                );
        }

        [TestMethod]
        public void TableResetAfterShowdownTest()
        {
            var handEvalMock = new Mock<IHandEvaluator>();
            GameEvaluatorTests.SetupEvalHandSequence(handEvalMock, new int[] { 1, 0 }, 3);
            HandEvaluatorResolver.Register(handEvalMock.Object);

            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);

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
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);

            using (new AssertionScope())
            {
                state.Street.Should().Be(StreetType.FINISHED);
                //((List<object>)msgs).Where(m => m is KeyValuePair<string, object>)
                //    .Select(m => (KeyValuePair<string, object>)m)
                //    .Where(m => m.Key == "message")
                //    .Select(m => m.Value as Dictionary<string, object>)
                //    .Where(m => m.ContainsKey("message_type"))
                //    .Should()
                //    .AllSatisfy(m => m["message_type"].Should().NotBe("street_start_message"));
            }
        }

        [TestMethod]
        public void AskPlayerTargetWhenDealerBtnPlayerIsFoldedTest()
        {
            var (state, _) = this.StartRound();
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);

            msgs.Last().Should().BeEquivalentTo(new AskMessage { PlayerUuid = "uuid1" }, options => options.Including(m => m.PlayerUuid));
        }

        [TestMethod]
        public void SkipAskingToAllinPlayerTest()
        {
            var (state, _) = this.StartRound();

            // Round 1
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 50);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 50);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            var stacksAfterRound1 = state.Table.Seats.Players.Select(p => p.Stack).ToList();

            // Round 1
            state.Table.ShiftDealerButton();
            state.Table.SetBlindPositions(1, 2);
            (state, _) = RoundManager.Instance.StartNewRound(2, 5, 0, state.Table);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 40);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 40);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 70);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 70);
            var stacksAfterRound2 = state.Table.Seats.Players.Select(p => p.Stack).ToList();

            using (new AssertionScope())
            {
                stacksAfterRound1.Should().BeEquivalentTo(new List<float> { 95, 40, 165 });
                stacksAfterRound2.Should().BeEquivalentTo(new List<float> { 25, 0, 95 });
                state.Street.Should().Be(StreetType.FLOP);
                msgs.Last().Should().BeEquivalentTo(new AskMessage { PlayerUuid = "uuid2" }, options => options.Including(m => m.PlayerUuid));
            }
        }

        [TestMethod]
        public void WhenOnlyOnePlayerIsWaitingAskTest()
        {
            var (state, _) = this.StartRound();

            // Round 1
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 50);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 50);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            var stacksAfterRound1 = state.Table.Seats.Players.Select(p => p.Stack).ToList();

            // Round 2
            //state.Table.ShiftDealerButton();
            //(state, _) = RoundManager.Instance.StartNewRound(2, 5, 0, state.Table);
            //(state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 40);
            //(state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 40);
            //(state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 70);
            //(state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 70);
            //(state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 0);
            //(state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 10);
            //(state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            //(state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 85);
            //(state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 85);

            using (new AssertionScope())
            {
                stacksAfterRound1.Should().BeEquivalentTo(new List<float> { 95, 40, 165 });
            }
        }

        [TestMethod]
        public void AskBigBlindInPreflopTest()
        {
            var (state, _) = this.StartRound();

            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);
            (state, var msgs) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 10);

            using (new AssertionScope())
            {
                state.Street.Should().Be(StreetType.PREFLOP);
                msgs.Last().Should().BeEquivalentTo(new AskMessage { PlayerUuid = "uuid1" }, options => options.Including(m => m.PlayerUuid));
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
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 15);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 20);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 25);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 30);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 50);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 50);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.RAISE, 125);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.CALL, 125);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);
            (state, _) = RoundManager.Instance.ApplyAction(state, ActionType.FOLD, 0);

            using (new AssertionScope())
            {
                state.Street.Should().Be(StreetType.FINISHED);
            }
        }

        //[TestMethod]
        //[Ignore("understandable test flow")]
        //public void AddAmountCalculationWhenRaiseOnAnteTest()
        //{
        //    var table = this.SetupTable();
        //    Func<GameState, object> potAmount = state
        //        => GameEvaluator.Instance.CreatePot(state.Table.Seats.Players)[0].Amount;

        //    var (state, _) = RoundManager.Instance.StartNewRound(1, 10, 5, table);

        //    using (new AssertionScope())
        //    {
        //        potAmount(state).Should().Be(45);
        //        state.Table.Seats.Players.Select(p => p.Stack).Should().BeEquivalentTo(new List<float> { 85, 75, 95 });

        //        var (folded_state, _) = RoundManager.Instance.ApplyAction(state, "fold", 0);
        //        var (called_state, _) = RoundManager.Instance.ApplyAction(state, "call", 20);

        //        potAmount(called_state).Should().Be(55);
        //        //((Table)state["table"]).Seats.Players.Select(p => p.Stack).Should().BeEquivalentTo(new List<float> { 85, 75, 95 });
        //        (called_state, _) = RoundManager.Instance.ApplyAction(state, "call", 20);

        //        called_state.Table.Seats.Players[2].ActionHistories.Last().Paid.Should().Be(20);
        //    }
        //}
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

        private (GameState roundState, List<IMessage> messages) StartRound()
        {
            var table = SetupTable();
            return RoundManager.Instance.StartNewRound(
                round_count: 1,
                small_blind_amount: 5f,
                ante_amount: 0f,
                table: table);
        }

        private Table SetupTable()
        {
            var players = Enumerable.Range(0, 3).Select(ix => new Player($"uuid{ix}", 100)).ToList();
            var deck = new Deck(cheat: true, cheatCardIds: Enumerable.Range(1, 52).ToList());
            var table = new Table(cheatDeck: deck);
            players.ForEach(p => table.Seats.Sitdown(p));
            table._dealerButton = 2;
            table.SetBlindPositions(0, 1);
            return table;
        }
    }
}
