using FluentAssertions;
using FluentAssertions.Execution;
using NPokerEngine.Messages;
using NPokerEngine.Players;
using NPokerEngine.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace NPokerEngine.Tests
{
    [TestClass]
    public class EmulatorTests
    {
        [TestMethod]
        public void BlindStructureTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(2, 10, 5);
            emulator.SetBlindStructure(new Dictionary<object, object> { { 5, new Dictionary<string, float> { { "ante", 5 }, { "small_blind", 60 } } } });
            var gameState = RestoreGameState(TwoPlayersSample.RoundState);
            gameState.Table.Seats.Players.ForEach(p => AttachHoleCardsFromTable(gameState.Table, p));
            var p1 = new TestPlayer(
                name: "p1",
                testActions: new List<Tuple<ActionType, int>>
                {
                    new Tuple<ActionType, int>(ActionType.FOLD, 0),
                    new Tuple<ActionType, int>(ActionType.RAISE, 55),
                    new Tuple<ActionType, int>(ActionType.CALL, 0)
                },
                uuid: GuidFromIx(1));
            var p2 = new TestPlayer(
                name: "p2",
                testActions: new List<Tuple<ActionType, int>>
                {
                    new Tuple<ActionType, int>(ActionType.CALL, 15),
                    new Tuple<ActionType, int>(ActionType.CALL, 55),
                    new Tuple<ActionType, int>(ActionType.FOLD, 0)
                },
                uuid: GuidFromIx(2));
            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);

            (gameState, _) = emulator.RunUntilRoundFinish(gameState);

            var (gameStateRound2, _) = emulator.StartNewRound(gameState);
            (gameStateRound2, _) = emulator.RunUntilRoundFinish(gameStateRound2);

            var (gameStateRound3, messagesRound3) = emulator.StartNewRound(gameStateRound2);

            using (new AssertionScope())
            {
                gameState.Table.Seats[0].Stack.Should().Be(65);
                gameState.Table.Seats[1].Stack.Should().Be(135);
                gameStateRound2.Table.Seats[0].Stack.Should().Be(120);
                gameStateRound2.Table.Seats[1].Stack.Should().Be(80);
                messagesRound3.First().Should().BeOfType<GameResultMessage>();
                gameStateRound3.Table.Seats[0].Stack.Should().Be(0);
                gameStateRound3.Table.Seats[1].Stack.Should().Be(80);
            }
        }

        [TestMethod]
        public void BlindStructureUpdateTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(max_round: 8, initial_stack: 100, small_blind_amount: 5);

            var p1 = new FoldPlayer("hoge", GuidFromIx(1));
            var p2 = new FoldPlayer("fuga", GuidFromIx(2));

            emulator.SetBlindStructure(new Dictionary<object, object> { 
                { 3, new Dictionary<string, float> { { "ante", 5 }, { "small_blind", 10 } } },
                { 5, new Dictionary<string, float> { { "ante", 10 }, { "small_blind", 20 } } },
            });

            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);

            var initialState = emulator.GenerateInitialState();

            var (stateRound1, _) = emulator.StartNewRound(initialState);
            (stateRound1, _) = emulator.ApplyAction(stateRound1, ActionType.FOLD);
            var (stateRound2, _) = emulator.ApplyAction(stateRound1, ActionType.FOLD);
            var (stateRound3, _) = emulator.ApplyAction(stateRound2, ActionType.FOLD);
            var (stateRound4, _) = emulator.ApplyAction(stateRound3, ActionType.FOLD);
            var (stateRound5, _) = emulator.ApplyAction(stateRound4, ActionType.FOLD);
            var (stateRound6, _) = emulator.ApplyAction(stateRound5, ActionType.FOLD);

            using (new AssertionScope())
            {
                initialState.SmallBlindAmount.Should().Be(5);
                stateRound1.SmallBlindAmount.Should().Be(5);
                stateRound2.SmallBlindAmount.Should().Be(5);
                stateRound3.SmallBlindAmount.Should().Be(10);
                stateRound4.SmallBlindAmount.Should().Be(10);
                stateRound5.SmallBlindAmount.Should().Be(20);
                stateRound5.SmallBlindAmount.Should().Be(20);
            }
        }

        [TestMethod]
        public void ApplyActionTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(10, 100, 5);
            emulator.SetBlindStructure(new Dictionary<object, object> { { 5, new Dictionary<string, float> { { "ante", 5 }, { "small_blind", 60 } } } });
            var gameState = RestoreGameState(TwoPlayersSample.RoundState);
            gameState.Table.Seats.Players.ForEach(p => AttachHoleCardsFromTable(gameState.Table, p));
            var p1 = new FoldPlayer(
                name: "p1",
                uuid: GuidFromIx(1));
            var p2 = new FoldPlayer(
                name: "p2",
                uuid: GuidFromIx(2));
            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);

            var (gameState1, messages1) = emulator.ApplyAction(gameState, ActionType.CALL, 15);
            var (gameState2, messages2) = emulator.ApplyAction(gameState1, ActionType.CALL, 0);
            var (gameState3, messages3) = emulator.ApplyAction(gameState2, ActionType.CALL, 0);

            using (new AssertionScope())
            {
                gameState1.Street.Should().Be(StreetType.RIVER);
                gameState1.Table.Seats[0].RoundActionHistories[StreetType.TURN].Should().SatisfyRespectively(
                    first => new ActionHistoryEntry { ActionType = ActionType.RAISE, Amount = 15, AddAmount = 15, Paid = 15 }
                    );
                messages1.Should().SatisfyRespectively(
                        first => first.Should().BeOfType<GameUpdateMessage>(),
                        second => second.Should().BeOfType<StreetStartMessage>(),
                        third => third.Should().BeOfType<AskMessage>()
                    );
                messages2.Should().SatisfyRespectively(
                        first => first.Should().BeOfType<GameUpdateMessage>(),
                        second => second.Should().BeOfType<AskMessage>()
                    );
                messages3.Should().SatisfyRespectively(
                        first => first.Should().BeOfType<GameUpdateMessage>(),
                        second => second.Should().BeOfType<RoundResultMessage>()
                    );
            }
        }

        [TestMethod]
        public void ApplyActionGameFinishDetectTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(max_round: 3, initial_stack: 100, small_blind_amount: 5);
            var gameState = RestoreGameState(TwoPlayersSample.RoundState);
            gameState.Table.Seats.Players.ForEach(p => AttachHoleCardsFromTable(gameState.Table, p));
            var p1 = new FoldPlayer(
                name: "p1",
                uuid: GuidFromIx(1));
            var p2 = new FoldPlayer(
                name: "p2",
                uuid: GuidFromIx(2));
            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);

            (gameState, var messages) = emulator.ApplyAction(gameState, ActionType.FOLD);

            messages.Last().Should().BeOfType<GameResultMessage>();
        }

        [TestMethod]
        public void ApplyActionStartNextRoundStateTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(max_round: 4, initial_stack: 100, small_blind_amount: 5);
            var gameState = RestoreGameState(TwoPlayersSample.RoundState);
            gameState.Table.Seats.Players.ForEach(p => AttachHoleCardsFromTable(gameState.Table, p));
            var p1 = new FoldPlayer(
                name: "p1",
                uuid: GuidFromIx(1));
            var p2 = new FoldPlayer(
                name: "p2",
                uuid: GuidFromIx(2));
            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);

            var (gameState1, messages1) = emulator.ApplyAction(gameState, ActionType.FOLD);
            var (gameState2, messages2) = emulator.ApplyAction(gameState1, ActionType.RAISE, 20);

            using (new AssertionScope())
            {
                gameState1.Table.Seats[0].Stack.Should().Be(120);
                gameState1.Table.Seats[1].Stack.Should().Be(80);
                messages2.Last().Should().BeOfType<AskMessage>();
                gameState2.Table.Seats[0].Stack.Should().Be(100);
                gameState2.Table.Seats[1].Stack.Should().Be(70);
            }
        }

        [TestMethod]
        public void ApplyActionWhenGameFinishedTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(3, 100, 5);
            var gameState = RestoreGameState(TwoPlayersSample.RoundState);
            gameState.Table.Seats.Players.ForEach(p => AttachHoleCardsFromTable(gameState.Table, p));
            var p1 = new FoldPlayer(
                name: "p1",
                uuid: GuidFromIx(1));
            var p2 = new FoldPlayer(
                name: "p2",
                uuid: GuidFromIx(2));
            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);

            (gameState, _) = emulator.ApplyAction(gameState, ActionType.FOLD);
            Action applyAction2 = () => emulator.ApplyAction(gameState, ActionType.FOLD);

            applyAction2.Should().Throw<Exception>();
        }

        [TestMethod]
        public void RunUntilRoundFinishTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(10, 100, 5);
            var gameState = RestoreGameState(TwoPlayersSample.RoundState);
            gameState.Table.Seats.Players.ForEach(p => AttachHoleCardsFromTable(gameState.Table, p));
            var p1 = new TestPlayer(
                name: "p1",
                testActions: new List<Tuple<ActionType, int>> { new Tuple<ActionType, int> (ActionType.FOLD, 0) },
                uuid: GuidFromIx(1));
            var p2 = new TestPlayer(
                name: "p2",
                testActions: new List<Tuple<ActionType, int>> { new Tuple<ActionType, int>(ActionType.CALL, 15) },
                uuid: GuidFromIx(2));
            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);

            (gameState, var messages) = emulator.RunUntilRoundFinish(gameState);

            messages.Where(t => t is not GameUpdateMessage).Should().SatisfyRespectively(
                    first => first.Should().BeOfType<StreetStartMessage>(),
                    second => second.Should().BeOfType<AskMessage>(),
                    third => third.Should().BeOfType<RoundResultMessage>()
                );
        }

        [TestMethod]
        public void RunUntilRoundFinishWhenAlreadyFinishedTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(10, 100, 5);
            var gameState = RestoreGameState(TwoPlayersSample.RoundState);
            gameState.Table.Seats.Players.ForEach(p => AttachHoleCardsFromTable(gameState.Table, p));
            var p1 = new TestPlayer(
                name: "p1",
                testActions: new List<Tuple<ActionType, int>> { new Tuple<ActionType, int>(ActionType.FOLD, 0) },
                uuid: GuidFromIx(1));
            var p2 = new TestPlayer(
                name: "p2",
                testActions: new List<Tuple<ActionType, int>> { new Tuple<ActionType, int>(ActionType.CALL, 15) },
                uuid: GuidFromIx(2));
            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);

            (gameState, var messages) = emulator.RunUntilRoundFinish(gameState);
            (gameState, messages) = emulator.RunUntilRoundFinish(gameState);

            messages.Should().BeEmpty();
        }

        [TestMethod]
        public void RunUntilRoundFinishGameFinishDetectTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(10, 100, 5);
            var gameState = RestoreGameState(TwoPlayersSample.RoundState);
            gameState.Table.Seats[0].AddHoleCards(Card.FromString("CA"), Card.FromString("D2"));
            AttachHoleCardsFromTable(gameState.Table, gameState.Table.Seats[1]);
            var p1 = new TestPlayer(
                name: "p1",
                testActions: new List<Tuple<ActionType, int>> { new Tuple<ActionType, int>(ActionType.RAISE, 65) },
                uuid: GuidFromIx(1));
            var p2 = new TestPlayer(
                name: "p2",
                testActions: new List<Tuple<ActionType, int>> { new Tuple<ActionType, int>(ActionType.CALL, 15), new Tuple<ActionType, int>(ActionType.CALL, 65) },
                uuid: GuidFromIx(2));
            gameState.Table.Deck._deck.Insert(gameState.Table.Deck._deck.Count - gameState.Table.Deck._popIndex, Card.FromString("C7"));

            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);

            (gameState, var messages) = emulator.RunUntilRoundFinish(gameState);

            using (new AssertionScope())
            {
                messages.Where(t => t is not GameUpdateMessage).Should().SatisfyRespectively(
                    first => first.Should().BeOfType<StreetStartMessage>(),
                    second => second.Should().BeOfType<AskMessage>(),
                    third => third.Should().BeOfType<AskMessage>(),
                    fourth => fourth.Should().BeOfType<RoundResultMessage>(),
                    fifth => fifth.Should().BeOfType<GameResultMessage>()
                    );
                messages.OfType<GameResultMessage>().Last().Seats[0].Stack.Should().Be(0);
                messages.OfType<GameResultMessage>().Last().Seats[1].Stack.Should().Be(200);
            }
        }

        [TestMethod]
        public void RunUntilGameFinishTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(10, 100, 5, 1);
            var gameState = RestoreGameState(TwoPlayersSample.RoundState);
            gameState.Table.Seats.Players.ForEach(p => AttachHoleCardsFromTable(gameState.Table, p));
            var p1 = new FoldPlayer(
                name: "p1",
                uuid: GuidFromIx(1));
            var p2 = new FoldPlayer(
                name: "p2",
                uuid: GuidFromIx(2));
            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);

            (gameState, var messages) = emulator.RunUntilGameFinish(gameState);

            using (new AssertionScope())
            {
                messages.Last().Should().BeOfType<GameResultMessage>();
                gameState.Table.Seats[0].Stack.Should().Be(114);
                gameState.Table.Seats[1].Stack.Should().Be(86);
            }
        }

        [TestMethod]
        public void RunUntilGameFinishWhenOnePlayerIsLeftTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(10, 100, 5, 7);
            var gameState = RestoreGameState(ThreePlayersSample.RoundState);
            gameState.Table.Seats[0].AddHoleCards(Card.FromString("C2"), Card.FromString("C3"));
            gameState.Table.Seats[1].AddHoleCards(Card.FromString("HA"), Card.FromString("CA"));
            gameState.Table.Seats[2].AddHoleCards(Card.FromString("D5"), Card.FromString("H6"));
            var p1 = new TestPlayer(
                name: "p1",
                testActions: new List<Tuple<ActionType, int>> 
                { 
                    new Tuple<ActionType, int>(ActionType.FOLD, 0),
                    new Tuple<ActionType, int> (ActionType.CALL, 10),
                    new Tuple<ActionType, int> (ActionType.CALL, 0),
                    new Tuple<ActionType, int> (ActionType.CALL, 10),
                    new Tuple<ActionType, int> (ActionType.FOLD, 0)
                },
                uuid: GuidFromIx(1));
            var p2 = new TestPlayer(
                name: "p2",
                testActions: new List<Tuple<ActionType, int>>(),
                uuid: GuidFromIx(2));
            var p3 = new TestPlayer(
                name: "p3",
                testActions: new List<Tuple<ActionType, int>>()
                {
                    new Tuple<ActionType, int>(ActionType.RAISE, 10)
                },
                uuid: GuidFromIx(3));

            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);
            emulator.RegisterPlayer(p3);

            gameState.Table.Deck._deck.Insert(gameState.Table.Deck._deck.Count - gameState.Table.Deck._popIndex, Card.FromString("C7"));

            (gameState, var messages) = emulator.RunUntilGameFinish(gameState);

            using (new AssertionScope())
            {
                messages.Last().Should().BeOfType<GameResultMessage>();
                gameState.Table.Seats[0].Stack.Should().Be(0);
                gameState.Table.Seats[1].Stack.Should().Be(0);
                gameState.Table.Seats[2].Stack.Should().Be(292);
            }
        }

        [TestMethod]
        public void RunUntilGameFinishWhenFinalRoundTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(10, 100, 5, 7);
            var gameState = RestoreGameState(ThreePlayersSample.RoundState);
            gameState.Table.Seats[0].AddHoleCards(Card.FromString("C2"), Card.FromString("C3"));
            gameState.Table.Seats[1].AddHoleCards(Card.FromString("HA"), Card.FromString("CA"));
            gameState.Table.Seats[2].AddHoleCards(Card.FromString("D5"), Card.FromString("H6"));
            var p1 = new FoldPlayer(
                name: "p1",
                uuid: GuidFromIx(1));
            var p2 = new FoldPlayer(
                name: "p2",
                uuid: GuidFromIx(2));
            var p3 = new FoldPlayer(
                name: "p3",
                uuid: GuidFromIx(3));

            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);
            emulator.RegisterPlayer(p3);

            gameState.Table.Deck._deck.Insert(gameState.Table.Deck._deck.Count - gameState.Table.Deck._popIndex, Card.FromString("C7"));

            (gameState, var messages) = emulator.RunUntilGameFinish(gameState);

            using (new AssertionScope())
            {
                messages.Last().Should().BeOfType<GameResultMessage>();
                gameState.RoundCount.Should().Be(10);
                gameState.Table.Seats[0].Stack.Should().Be(35);
                gameState.Table.Seats[1].Stack.Should().Be(0);
                gameState.Table.Seats[2].Stack.Should().Be(265);
            }
        }

        [TestMethod]
        public void LastRoundJudgeTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(3, 100, 5, 0);
            var gameState = RestoreGameState(TwoPlayersSample.RoundState);

            using (new AssertionScope())
            {
                emulator.IsLastRound(gameState).Should().BeFalse();
                gameState.Street = StreetType.FINISHED;
                emulator.IsLastRound(gameState).Should().BeTrue();
                gameState.RoundCount = 2;
                emulator.IsLastRound(gameState).Should().BeFalse();
                gameState.Table.Seats[0].Stack = 0;
                emulator.IsLastRound(gameState).Should().BeTrue();
            }
        }

        [TestMethod]
        public void StartNewRoundTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(10, 100, 5, 0);
            var gameState = RestoreGameState(TwoPlayersSample.RoundState);
            gameState.Table.Seats.Players.ForEach(p => AttachHoleCardsFromTable(gameState.Table, p));
            var p1 = new FoldPlayer(
                name: "p1",
                uuid: GuidFromIx(1));
            var p2 = new FoldPlayer(
                name: "p2",
                uuid: GuidFromIx(2));
            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);

            // run until round finish
            (gameState, var messages) = emulator.ApplyAction(gameState, ActionType.CALL, 15);
            (gameState, messages) = emulator.ApplyAction(gameState, ActionType.CALL, 0);
            (gameState, messages) = emulator.ApplyAction(gameState, ActionType.CALL, 0);

            (gameState, messages) = emulator.StartNewRound(gameState);

            using (new AssertionScope())
            {
                gameState.RoundCount.Should().Be(4);
                gameState.Table.DealerButton.Should().Be(1);
                gameState.Street.Should().Be(StreetType.PREFLOP);
                gameState.NextPlayerIx.Should().Be(0);
                messages[2].Should().BeOfType<StreetStartMessage>();
                messages[3].Should().BeOfType<AskMessage>();
                messages[2].Should().BeEquivalentTo(new StreetStartMessage { Street = StreetType.PREFLOP }, options => options.Including(m => m.Street));
                messages[3].Should().BeEquivalentTo(new AskMessage { PlayerUuid = GuidFromIx(1).ToString() }, options => options.Including(m => m.PlayerUuid));
            }
        }

        [TestMethod]
        public void StartNewRoundExcludeNoMoneyPlayers()
        {
            var emulator = new Emulator();
            float sbAmount = 5;
            float ante = 7;
            emulator.SetupConfig(10, 100, sbAmount, ante);
            var gameState = RestoreGameState(ThreePlayersSample.RoundState);
            gameState.Table.Seats.Players.ForEach(p => AttachHoleCardsFromTable(gameState.Table, p));

            var p1 = new FoldPlayer(
                name: "p1",
                uuid: GuidFromIx(1));
            var p2 = new FoldPlayer(
                name: "p2",
                uuid: GuidFromIx(2));
            var p3 = new FoldPlayer(
                name: "p3",
                uuid: GuidFromIx(3));

            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);
            emulator.RegisterPlayer(p3);

            // case1: second player cannot pay small blind
            var (finishState, messages) = emulator.ApplyAction(gameState, ActionType.FOLD);
            finishState.Table.Seats[0].Stack = 11;
            var stacksCase1 = finishState.Table.Seats.Players.Select(p => p.Stack).ToArray();
            (var gameStateCase1, messages) = emulator.StartNewRound(finishState);

            //case2: third player cannot pay big blind
            (finishState, messages) = emulator.ApplyAction(gameState, ActionType.FOLD);
            finishState.Table.Seats[1].Stack = 16;
            var stacksCase2 = finishState.Table.Seats.Players.Select(p => p.Stack).ToArray();
            (var gameStateCase2, messages) = emulator.StartNewRound(finishState);

            using (new AssertionScope())
            {
                gameStateCase1.Table.DealerButton.Should().Be(2);
                gameStateCase1.NextPlayerIx.Should().Be(1);
                gameStateCase1.Table.Seats[1].Stack.Should().Be(stacksCase1[1] - sbAmount - ante);
                gameStateCase1.Table.Seats[2].Stack.Should().Be(stacksCase1[2] - sbAmount*2 - ante);
                gameStateCase1.Table.Seats[0].PayInfo.Status.Should().Be(PayInfoStatus.FOLDED);
                GameEvaluator.Instance.CreatePot(gameStateCase1.Table.Seats.Players)[0].Amount.Should().Be(sbAmount*3 + ante*2);

                gameStateCase2.Table.DealerButton.Should().Be(2);
                gameStateCase2.NextPlayerIx.Should().Be(0);
                gameStateCase2.Table.Seats[0].Stack.Should().Be(stacksCase2[0] - sbAmount - ante);
                gameStateCase2.Table.Seats[2].Stack.Should().Be(stacksCase2[2] - sbAmount*2 - ante);
                gameStateCase2.Table.Seats[1].PayInfo.Status.Should().Be(PayInfoStatus.FOLDED);
                gameStateCase2.Table.Seats[0].PayInfo.Status.Should().Be(PayInfoStatus.PAY_TILL_END);
                GameEvaluator.Instance.CreatePot(gameStateCase2.Table.Seats.Players)[0].Amount.Should().Be(sbAmount * 3 + ante * 2);
            }

        }

        [TestMethod]
        public void StartNewRoundExcludeNoMonewPlayers2Test()
        {
            var emulator = new Emulator();
            float sbAmount = 5;
            float ante = 7;
            emulator.SetupConfig(10, 100, sbAmount, ante);
            var gameState = RestoreGameState(ThreePlayersSample.RoundState);
            gameState.Table.Seats.Players.ForEach(p => AttachHoleCardsFromTable(gameState.Table, p));

            var p1 = new FoldPlayer(
                name: "p1",
                uuid: GuidFromIx(1));
            var p2 = new FoldPlayer(
                name: "p2",
                uuid: GuidFromIx(2));
            var p3 = new FoldPlayer(
                name: "p3",
                uuid: GuidFromIx(3));

            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);
            emulator.RegisterPlayer(p3);

            // case1: second player cannot pay small blind
            var (finishState, messages) = emulator.ApplyAction(gameState, ActionType.FOLD);
            finishState.Table.Seats[2].Stack = 6;
            var stacks = finishState.Table.Seats.Players.Select(p => p.Stack).ToArray();
            (gameState, messages) = emulator.StartNewRound(finishState);

            using (new AssertionScope())
            {
                gameState.Table.DealerButton.Should().Be(0);
                gameState.Table.SmallBlindPosition.Should().Be(1);
                gameState.NextPlayerIx.Should().Be(1);
            }
        }

        [TestMethod]
        public void StartNewRoundGameFinishJudgeTest()
        {
            var emulator = new Emulator();
            float sbAmount = 5;
            float ante = 7;
            emulator.SetupConfig(10, 100, sbAmount, ante);
            var gameState = RestoreGameState(ThreePlayersSample.RoundState);
            gameState.Table.Seats.Players.ForEach(p => AttachHoleCardsFromTable(gameState.Table, p));

            var p1 = new FoldPlayer(
                name: "p1",
                uuid: GuidFromIx(1));
            var p2 = new FoldPlayer(
                name: "p2",
                uuid: GuidFromIx(2));
            var p3 = new FoldPlayer(
                name: "p3",
                uuid: GuidFromIx(3));

            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);
            emulator.RegisterPlayer(p3);

            var (finishState, messages) = emulator.ApplyAction(gameState, ActionType.FOLD);
            finishState.Table.Seats[2].Stack = 11;
            finishState.Table.Seats[1].Stack = 16;
            var stacks = finishState.Table.Seats.Players.Select(p => p.Stack).ToArray();
            (gameState, messages) = emulator.StartNewRound(finishState);

            messages.Last().Should().BeOfType<GameResultMessage>();
        }

        [TestMethod]
        public void GenerateInitialStateTest()
        {
            var emulator = new Emulator();
            emulator.SetupConfig(max_round: 8, initial_stack: 100, small_blind_amount: 5, ante: 3);

            var p1 = new FoldPlayer(
                name: "hoge",
                uuid: GuidFromIx(1));
            var p2 = new FoldPlayer(
                name: "fuga",
                uuid: GuidFromIx(2));

            emulator.RegisterPlayer(p1);
            emulator.RegisterPlayer(p2);

            var state = emulator.GenerateInitialState();
            var (startState, messages) = emulator.StartNewRound(state);
            var (callState, _) = emulator.ApplyAction(startState, ActionType.CALL, 10);

            using (new AssertionScope())
            {
                state.RoundCount.Should().Be(0);
                state.SmallBlindAmount.Should().Be(5);
                state.Table.Seats[0].Stack.Should().Be(100);
                state.Table.Seats[0].Uuid.Should().Be(GuidFromIx(1).ToString());
                state.Table.Seats[1].Stack.Should().Be(100);
                state.Table.Seats[1].Uuid.Should().Be(GuidFromIx(2).ToString());
                state.Table.DealerButton.Should().Be(1);

                startState.Table.DealerButton.Should().Be(0);
                startState.Table.SmallBlindPosition.Should().Be(1);
                startState.Table.BigBlindPosition.Should().Be(0);
                startState.NextPlayerIx.Should().Be(1);
                callState.NextPlayerIx.Should().Be(1);
            }
        }

        [TestMethod]
        public void GeneratePossibleActionsTest()
        {
            var emulator = new Emulator();
            var state1 = RestoreGameState(TwoPlayersSample.RoundState);
            var validActions1 = emulator.GeneratePossibleActions(state1);
            var state2 = RestoreGameState(ThreePlayersSample.RoundState);
            var validActions2 = emulator.GeneratePossibleActions(state2);

            using (new AssertionScope())
            {
                validActions1.Should().SatisfyRespectively(
                    first => first.Should().Be((ActionType.FOLD, AmountInterval.Empty)),
                    second => second.Should().Be((ActionType.CALL, new AmountInterval(15))),
                    third => third.Should().Be((ActionType.RAISE, new AmountInterval(30, 80)))
                    );
                validActions2.Should().SatisfyRespectively(
                    first => first.Should().Be((ActionType.FOLD, AmountInterval.Empty)),
                    second => second.Should().Be((ActionType.CALL, new AmountInterval(10))),
                    third => third.Should().Be((ActionType.RAISE, new AmountInterval(20, 35)))
                    );
            }
        }

        private static GameState RestoreGameState(string json)
        {
            var gameState = new GameState()
            {
                Table = new Table()
            };
            var jsonObject = JsonSerializer.Deserialize<JsonObject>(json.Replace("'", "\"").Replace("\\n", "\n").Replace("\\t", "\t"));
            gameState.Table._dealerButton = jsonObject["dealer_btn"].GetValue<int>();
            gameState.RoundCount = jsonObject["round_count"].GetValue<int>();
            gameState.SmallBlindAmount = jsonObject["small_blind_amount"].GetValue<float>();
            gameState.NextPlayerIx = jsonObject["next_player"].GetValue<int>();
            gameState.Street = Enum.Parse<StreetType>(jsonObject["street"].GetValue<string>(), ignoreCase: true);
            gameState.Table._communityCards = ((JsonArray)jsonObject["community_card"]).Select(x => Card.FromString(x.ToString())).ToList();
            var excludeIds = gameState.Table._communityCards.Select(c => c.ToId()).ToArray();
            gameState.Table.Deck._deck = Enumerable.Range(1, 52).Where(ix => !excludeIds.Contains(ix)).Select(Card.FromId).ToList();
            gameState.Table.SetBlindPositions(jsonObject["small_blind_pos"].GetValue<int>(), jsonObject["big_blind_pos"].GetValue<int>());

            var players = new List<Player>();
            foreach (JsonObject item in (JsonArray)jsonObject["seats"])
            {
                var player = new Player(item["uuid"].GetValue<string>(), item["stack"].GetValue<int>(), item["name"].GetValue<string>());
                player.PayInfo._status = item["state"].GetValue<string>() switch
                {
                    "participating" => PayInfoStatus.PAY_TILL_END,
                    "allin" => PayInfoStatus.ALLIN,
                    _ => PayInfoStatus.PAY_TILL_END
                };
                players.Add(player);
            }

            var actionHistories = (JsonObject)jsonObject["action_histories"];

            var lastActionHistoryType = gameState.Street.ToString(); // actionHistories.Select(h => h.Key).Last();

            foreach (var item in actionHistories)
            {
                var streetType = Enum.Parse<StreetType>(item.Key, ignoreCase: true);
                foreach (JsonObject h in (JsonArray)item.Value)
                {
                    var p = players.Single(t => t.Uuid == h["uuid"].GetValue<string>());
                    if (!p.RoundActionHistories.ContainsKey(streetType))
                        p.RoundActionHistories[streetType] = new List<ActionHistoryEntry>();

                    var historyEntry = new ActionHistoryEntry
                    {
                        ActionType = Enum.Parse<ActionType>(h["action"].GetValue<string>(), ignoreCase: true),
                        Amount = h["amount"].GetValue<float>(),
                        AddAmount = h.ContainsKey("add_amount") ? h["add_amount"].GetValue<float>() : 0,
                        Paid = h.ContainsKey("paid") ? h["paid"].GetValue<float>() : 0,
                        Uuid = h["uuid"].GetValue<string>()
                    };

                    if (string.Compare(item.Key, lastActionHistoryType, ignoreCase: true) != 0) //item.Key != lastActionHistoryType)
                        p.RoundActionHistories[streetType].Add(historyEntry);
                    else
                        p.ActionHistories.Add(historyEntry);

                    p.PayInfo._amount += historyEntry.ActionType switch
                    {
                        ActionType.CALL or ActionType.RAISE => h["paid"].GetValue<float>(),
                        ActionType.SMALL_BLIND or ActionType.BIG_BLIND or ActionType.ANTE => h["amount"].GetValue<float>(),
                        _ => 0f
                    };
                }
            }

            players.ForEach(p => gameState.Table.Seats.Sitdown(p));

            return gameState;
        }

        private static Guid GuidFromIx(int x)
            => Guid.Parse(Guid.Empty.ToString().Replace("0", x.ToString()));

        private void AttachHoleCardsFromTable(Table table, Player player)
        {
            player.AddHoleCards(table.Deck.DrawCards(2).ToArray());
        }

        private static class TwoPlayersSample
        {
            public static string ValidActions = "[{'action': 'fold', 'amount': 0}, {'action': 'call', 'amount': 15}, {'action': 'raise', 'amount': {'max': 80, 'min': 30}}]";
            
            public static string HoleCards = "['CA', 'S3']";

            public static string RoundState = @"{
                'dealer_btn': 0,
                'round_count': 3,
                'small_blind_amount': 5,
                'next_player': 1,
                'small_blind_pos': 0,
                'big_blind_pos': 1,
                'street': 'turn',
                'community_card': ['D5', 'D9', 'H6', 'CK'],
                'pot': {'main': {'amount': 55}, 'side': []},
                'seats': [
                    {'stack': 65, 'state': 'participating', 'name': 'p1', 'uuid': '11111111-1111-1111-1111-111111111111'},
                    {'stack': 80, 'state': 'participating', 'name': 'p2', 'uuid': '22222222-2222-2222-2222-222222222222'}
                    ],
                'action_histories': {
                    'preflop': [
                        {'action': 'SMALL_BLIND', 'amount': 5, 'add_amount': 5, 'uuid': '11111111-1111-1111-1111-111111111111'},
                        {'action': 'BIG_BLIND', 'amount': 10, 'add_amount': 5, 'uuid': '22222222-2222-2222-2222-222222222222'},
                        {'action': 'CALL', 'amount': 10, 'uuid': '11111111-1111-1111-1111-111111111111', 'paid': 5}
                        ],
                    'flop': [
                        {'action': 'RAISE', 'amount': 5, 'add_amount': 5, 'paid': 5, 'uuid': '11111111-1111-1111-1111-111111111111'},
                        {'action': 'RAISE', 'amount': 10, 'add_amount': 5, 'paid': 10, 'uuid': '22222222-2222-2222-2222-222222222222'},
                        {'action': 'CALL', 'amount': 10, 'uuid': '11111111-1111-1111-1111-111111111111', 'paid': 5}
                        ],
                    'turn': [
                        {'action': 'RAISE', 'amount': 15, 'add_amount': 15, 'paid': 15, 'uuid': '11111111-1111-1111-1111-111111111111'}
                        ]
                    }
                }";

            public static string P1ActionHistories = @"[
                {'action': 'RAISE', 'amount': 15, 'add_amount': 15, 'paid': 15, 'uuid': '11111111-1111-1111-1111-111111111111'}
            ]";

            public static string P2ActionHistories = "[]";

            public static string P1RoundActionHistories = @"[
            [
                {'action': 'SMALL_BLIND', 'amount': 5, 'add_amount': 5, 'uuid': '11111111-1111-1111-1111-111111111111'},
                {'action': 'CALL', 'amount': 10, 'uuid': '11111111-1111-1111-1111-111111111111', 'paid': 5}
                ],
            [
                {'action': 'RAISE', 'amount': 5, 'add_amount': 5, 'paid': 5, 'uuid': '11111111-1111-1111-1111-111111111111'},
                {'action': 'CALL', 'amount': 10, 'uuid': '11111111-1111-1111-1111-111111111111', 'paid': 5}
                ]
            ]";

            public static string P2RoundActionHistories = @"[
            [
                {'action': 'BIG_BLIND', 'amount': 10, 'add_amount': 5, 'uuid': '22222222-2222-2222-2222-222222222222'}
                ],
            [
                {'action': 'RAISE', 'amount': 10, 'add_amount': 5, 'paid': 10, 'uuid': '22222222-2222-2222-2222-222222222222'}
                ]
            ]";

            //player_pay_info = [pay_info.status, pay_info.amount]
            public static string P1PayInfo = "[0, 35]";
            public static string P2PayInfo = "[0, 20]";
        }

        public static class ThreePlayersSample
        {
            public static string RoundState = @"{
            'dealer_btn': 1,
            'round_count': 2,
            'next_player': 0,
            'small_blind_pos': 1,
            'big_blind_pos': 2,
            'small_blind_amount': 5,
            'action_histories': {
                'turn': [
                    {'action': 'RAISE', 'amount': 10, 'add_amount': 10, 'paid': 10, 'uuid': '33333333-3333-3333-3333-333333333333'}
                    ],
                'preflop': [
                    {'action': 'SMALL_BLIND', 'amount': 5, 'add_amount': 5, 'uuid': '22222222-2222-2222-2222-222222222222'},
                    {'action': 'BIG_BLIND', 'amount': 10, 'add_amount': 5, 'uuid': '33333333-3333-3333-3333-333333333333'},
                    {'action': 'CALL', 'amount': 10, 'uuid': '11111111-1111-1111-1111-111111111111', 'paid': 10},
                    {'action': 'CALL', 'amount': 10, 'uuid': '22222222-2222-2222-2222-222222222222', 'paid': 5}
                    ],
                'flop': [
                    {'action': 'CALL', 'amount': 0, 'uuid': '22222222-2222-2222-2222-222222222222', 'paid': 0},
                    {'action': 'CALL', 'amount': 0, 'uuid': '33333333-3333-3333-3333-333333333333', 'paid': 0},
                    {'action': 'RAISE', 'amount': 50, 'add_amount': 50, 'paid': 50, 'uuid': '11111111-1111-1111-1111-111111111111'},
                    {'action': 'CALL', 'amount': 40, 'uuid': '22222222-2222-2222-2222-222222222222', 'paid': 40},
                    {'action': 'CALL', 'amount': 50, 'uuid': '33333333-3333-3333-3333-333333333333', 'paid': 50}
                    ]
                },
            'street': 'turn',
            'seats': [
                {'stack': 35, 'state': 'participating', 'name': 'p1', 'uuid': '11111111-1111-1111-1111-111111111111'},
                {'stack': 0, 'state': 'allin', 'name': 'p2', 'uuid': '22222222-2222-2222-2222-222222222222'},
                {'stack': 85, 'state': 'participating', 'name': 'p3', 'uuid': '33333333-3333-3333-3333-333333333333'}
                ],
            'community_card': ['HJ', 'C8', 'D2', 'H4'],
            'pot': {'main': {'amount': 150}, 'side': [{'amount': 30, 'eligibles': [""dummy""]}]}
            }
";
        }
    }
}
