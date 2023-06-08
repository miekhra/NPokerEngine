using FluentAssertions;
using FluentAssertions.Execution;

namespace NPokerEngine.Tests.Engine
{
    [TestClass]
    public class ActionCheckerTests
    {
        [TestMethod]
        public void CheckTest()
        {
            var players = SetupCleanPlayers();
            using (new AssertionScope())
            {
                ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.CALL, 0).Should().BeFalse();
                ActionChecker.Instance.NeedAmountForAction(players.First(), 0).Should().Be(0);
                ActionChecker.Instance.NeedAmountForAction(players.Last(), 0).Should().Be(0);
            }
        }

        [TestMethod]
        public void CallTest()
        {
            var players = SetupCleanPlayers();
            ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.CALL, 10).Should().BeTrue();
        }

        [TestMethod]
        public void TooSmallRaiseTest()
        {
            var players = SetupCleanPlayers();
            ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.RAISE, 4).Should().BeTrue();
        }

        [TestMethod]
        public void LegalRaiseTest()
        {
            var players = SetupCleanPlayers();
            ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.RAISE, 5).Should().BeFalse();
        }

        [TestMethod]
        public void _FoldTest()
        {
            var players = SetupBlindPlayers();
            ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.FOLD).Should().BeFalse();
        }

        [TestMethod]
        public void _CallTest()
        {
            var players = SetupBlindPlayers();
            using (new AssertionScope())
            {
                ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.CALL, 9).Should().BeTrue();
                ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.CALL, 10).Should().BeFalse();
                ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.CALL, 11).Should().BeTrue();
                ActionChecker.Instance.NeedAmountForAction(players.First(), 10).Should().Be(5);
                ActionChecker.Instance.NeedAmountForAction(players.Last(), 10).Should().Be(0);
            }
        }

        [TestMethod]
        public void _CallRaiseTest()
        {
            var players = SetupBlindPlayers();
            using (new AssertionScope())
            {
                ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.RAISE, 14).Should().BeTrue();
                ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.RAISE, 15).Should().BeFalse();
                ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.RAISE, 16).Should().BeFalse();
                ActionChecker.Instance.NeedAmountForAction(players.First(), 15).Should().Be(10);
                ActionChecker.Instance.NeedAmountForAction(players.Last(), 15).Should().Be(5);
            }
        }

        [TestMethod]
        public void _ShortOfMoneyTest()
        {
            var players = SetupBlindPlayers();
            players.First().CollectBet(88); //p1 stack is $7
            using (new AssertionScope())
            {
                ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.CALL, 10).Should().BeFalse();
                ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.CALL, 15).Should().BeTrue();
            }
        }

        [TestMethod]
        public void _SmallBlindAllInRaiseTest()
        {
            var players = SetupBlindPlayers();
            ActionChecker.Instance.IsIlLegal(players, 0, 2.5f, ActionType.RAISE, 100).Should().BeFalse();
        }

        [TestMethod]
        public void _BigBlindAllInCallTest()
        {
            var players = SetupBlindPlayers();
            players.First().AddActionHistory(ActionType.RAISE, 100, 95);

            using (new AssertionScope())
            {
                ActionChecker.Instance.IsIlLegal(players, 1, 2.5f, ActionType.CALL, 100).Should().BeFalse();
                players.Last().CollectBet(1);
                ActionChecker.Instance.IsIlLegal(players, 1, 2.5f, ActionType.CALL, 100).Should().BeTrue();
            }
        }

        [TestMethod]
        public void AllinCheckOnCallTest()
        {
            var player = SetupCleanPlayers().First();

            using (new AssertionScope())
            {
                ActionChecker.Instance.IsAllin(player, ActionType.CALL, 99).Should().BeFalse();
                ActionChecker.Instance.IsAllin(player, ActionType.CALL, 100).Should().BeTrue();
                ActionChecker.Instance.IsAllin(player, ActionType.CALL, 101).Should().BeTrue();
            }
        }

        [TestMethod]
        public void AllinCheckOnFoldTest()
        {
            var player = SetupCleanPlayers().First();

            ActionChecker.Instance.IsAllin(player, ActionType.FOLD, 0).Should().BeFalse();
        }

        [TestMethod]
        public void CorrectActionOnAllinCallTest()
        {
            var players = SetupCleanPlayers();

            players.First().AddActionHistory(ActionType.RAISE, 50, 50);
            players.Last().AddActionHistory(ActionType.BIG_BLIND, sbAmount: 5);
            players.Last().Stack = 30;

            (ActionType actionName, float amount) = ActionChecker.Instance.CorrectAction(players, 1, 2.5f, ActionType.CALL, 50);

            using (new AssertionScope())
            {
                actionName.Should().Be(ActionType.CALL);
                amount.Should().Be(40);
            }
        }

        [TestMethod]
        public void CorrectIllegalCallTest()
        {
            var players = SetupCleanPlayers();

            (ActionType actionName, float amount) = ActionChecker.Instance.CorrectAction(players, 0, 2.5f, ActionType.CALL, 10);

            using (new AssertionScope())
            {
                actionName.Should().Be(ActionType.FOLD);
                amount.Should().Be(0);
            }
        }

        [TestMethod]
        public void CorrectCorrectActionOnCallRegressionTest()
        {
            var players = SetupCleanPlayers();
            players.First().Stack = 130;
            players.Last().Stack = 70;

            players.First().CollectBet(5);
            players.First().PayInfo.UpdateByPay(5);
            players.First().AddActionHistory(ActionType.SMALL_BLIND, sbAmount: 5);
            players.Last().CollectBet(10);
            players.Last().PayInfo.UpdateByPay(10);
            players.Last().AddActionHistory(ActionType.BIG_BLIND, sbAmount: 5);
            players.First().CollectBet(55);
            players.First().PayInfo.UpdateByPay(55);
            players.First().AddActionHistory(ActionType.RAISE, 60, 55);

            (ActionType actionName, float amount) = ActionChecker.Instance.CorrectAction(players, 1, 2.5f, ActionType.CALL, 60);

            using (new AssertionScope())
            {
                actionName.Should().Be(ActionType.CALL);
                amount.Should().Be(60);
            }
        }

        [TestMethod]
        public void CorrectIllegalRaiseTest()
        {
            var players = SetupCleanPlayers();

            (ActionType actionName, float amount) = ActionChecker.Instance.CorrectAction(players, 0, 2.5f, ActionType.RAISE, 101);

            using (new AssertionScope())
            {
                actionName.Should().Be(ActionType.FOLD);
                amount.Should().Be(0);
            }
        }

        [TestMethod]
        public void CorrectActionWhenLegalTest()
        {
            var players = SetupCleanPlayers();

            (ActionType actionName, float amount) = ActionChecker.Instance.CorrectAction(players, 0, 2.5f, ActionType.CALL, 0);

            using (new AssertionScope())
            {
                actionName.Should().Be(ActionType.CALL);
                amount.Should().Be(0);
            }
        }

        [TestMethod]
        public void CorrectActionWhenLegal2Test()
        {
            var players = SetupCleanPlayers();

            (ActionType actionName, float amount) = ActionChecker.Instance.CorrectAction(players, 0, 2.5f, ActionType.RAISE, 100);

            using (new AssertionScope())
            {
                actionName.Should().Be(ActionType.RAISE);
                amount.Should().Be(100);
            }
        }

        [TestMethod]
        public void LegalActionsTest()
        {
            var players = SetupBlindPlayers();

            var legalActions = ActionChecker.Instance.LegalActions(players, 0, 2.5f);

            legalActions.Should().SatisfyRespectively
                (
                    first => first.Should().BeEquivalentTo(new KeyValuePair<ActionType, AmountInterval>(ActionType.FOLD, AmountInterval.Empty)),
                    second => second.Should().BeEquivalentTo(new KeyValuePair<ActionType, AmountInterval>(ActionType.CALL, new AmountInterval(10f))),
                    third => third.Should().BeEquivalentTo(new KeyValuePair<ActionType, AmountInterval>(ActionType.RAISE, new AmountInterval(15f, 100f)))
                );
        }

        [TestMethod]
        public void LegalActionsWhenShortOfMoneyTest()
        {
            var players = SetupBlindPlayers();
            players.First().Stack = 9;
            var legalActions = ActionChecker.Instance.LegalActions(players, 0, 2.5f);

            legalActions.Should().SatisfyRespectively
                (
                    first => first.Should().BeEquivalentTo(new KeyValuePair<ActionType, AmountInterval>(ActionType.FOLD, AmountInterval.Empty)),
                    second => second.Should().BeEquivalentTo(new KeyValuePair<ActionType, AmountInterval>(ActionType.CALL, new AmountInterval(10f))),
                    third => third.Should().BeEquivalentTo(new KeyValuePair<ActionType, AmountInterval>(ActionType.RAISE, new AmountInterval(-1, -1)))
                );
        }

        [TestMethod()]
        public void NeedAmountAfterAnteTest()
        {
            //situation => SB=$5 (players[0]), BB=$10 (players[1]), ANTE=$3
            var players = Enumerable.Range(0, 3).Select(t => new Player("uuid", 100, name: "name")).ToList();
            players.ForEach(p =>
            {
                p.CollectBet(3);
                p.AddActionHistory(ActionType.ANTE, 3);
                p.PayInfo.UpdateByPay(3);
            });

            players[0].CollectBet(5);
            players[0].AddActionHistory(ActionType.SMALL_BLIND, sbAmount: 5);
            players[0].PayInfo.UpdateByPay(5);
            players[1].CollectBet(10);
            players[1].AddActionHistory(ActionType.BIG_BLIND, sbAmount: 5);
            players[1].PayInfo.UpdateByPay(10);

            void setStacks(float[] stacks)
            {
                for (int ix = 0; ix < stacks.Length; ix++)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    players[ix].Stack = stacks[ix];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
            }

            setStacks(new float[] { 7, 7, 7 });
            using (new AssertionScope())
            {
                ActionChecker.Instance.CorrectAction(players, 0, 5, ActionType.CALL, 10).Should().BeEquivalentTo((ActionType.CALL, 10));
                ActionChecker.Instance.CorrectAction(players, 1, 5, ActionType.CALL, 10).Should().BeEquivalentTo((ActionType.CALL, 10));
                ActionChecker.Instance.CorrectAction(players, 2, 5, ActionType.CALL, 10).Should().BeEquivalentTo((ActionType.CALL, 7));

                ActionChecker.Instance.IsAllin(players[2], ActionType.CALL, 8).Should().BeTrue();
                ActionChecker.Instance.IsAllin(players[2], ActionType.RAISE, 10).Should().BeFalse();

                ActionChecker.Instance.NeedAmountForAction(players[0], 10).Should().Be(5);
                ActionChecker.Instance.NeedAmountForAction(players[1], 10).Should().Be(0);
                ActionChecker.Instance.NeedAmountForAction(players[2], 10).Should().Be(10);

                setStacks(new float[] { 12, 12, 12 });
                var legalActions = ActionChecker.Instance.LegalActions(players, 2, 5);
                legalActions.ElementAt(2).Value.MaxValue.Should().Be(-1);

                setStacks(new float[] { 10, 5, 12 });
                ActionChecker.Instance.CorrectAction(players, 0, 5, ActionType.RAISE, 15).Should().BeEquivalentTo((ActionType.RAISE, 15));
                ActionChecker.Instance.CorrectAction(players, 1, 5, ActionType.RAISE, 15).Should().BeEquivalentTo((ActionType.RAISE, 15));
                ActionChecker.Instance.CorrectAction(players, 2, 5, ActionType.RAISE, 15).Should().BeEquivalentTo((ActionType.FOLD, 0));
            }
        }

        private List<Player> SetupCleanPlayers()
            => Enumerable.Range(0, 2).Select(t => new Player("uuid", 100)).ToList();

        private List<Player> SetupBlindPlayers()
            => Enumerable.Range(0, 2).Select(t => CreateBlindPlayer(!Convert.ToBoolean(t))).ToList();

        private Player CreateBlindPlayer(bool smallBlind)
        {
            var blind = smallBlind ? 5 : 10;
            var player = new Player("uuid", 100, smallBlind ? "sb" : "bb");
            player.AddActionHistory(ActionType.RAISE, blind, 5);
            player.CollectBet(blind);
            player.PayInfo.UpdateByPay(blind);
            return player;
        }
    }
}
