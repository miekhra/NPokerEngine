using FluentAssertions.Execution;
using FluentAssertions;
using NPokerEngine.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                ActionChecker.Instance.IsIlLegal(players, 0, 2, "call", 0).Should().BeFalse();
                ActionChecker.Instance.NeedAmountForAction(players.First(), 0).Should().Be(0);
                ActionChecker.Instance.NeedAmountForAction(players.Last(), 0).Should().Be(0);
            }
        }

        [TestMethod]
        public void CallTest()
        {
            var players = SetupCleanPlayers();
            ActionChecker.Instance.IsIlLegal(players, 0,2,"call", 10).Should().BeTrue();
        }

        [TestMethod]
        public void TooSmallRaiseTest()
        {
            var players = SetupCleanPlayers();
            ActionChecker.Instance.IsIlLegal(players, 0, 2, "raise", 3).Should().BeTrue();
        }

        [TestMethod]
        public void LegalRaiseTest()
        {
            var players = SetupCleanPlayers();
            ActionChecker.Instance.IsIlLegal(players, 0, 2, "raise", 4).Should().BeFalse();
        }

        [TestMethod]
        public void FoldTest()
        {
            var players = SetupBlindPlayers();
            ActionChecker.Instance.IsIlLegal(players, 0, 2, "fold").Should().BeFalse();
        }

        [TestMethod]
        public void CallTest__()
        {
            var players = SetupBlindPlayers();
            using (new AssertionScope())
            {
                ActionChecker.Instance.IsIlLegal(players, 0, 2, "call", 10).Should().BeFalse();
                ActionChecker.Instance.IsIlLegal(players, 0, 2, "call", 11).Should().BeTrue();
                ActionChecker.Instance.IsIlLegal(players, 0, 2, "call", 12).Should().BeTrue();
                ActionChecker.Instance.NeedAmountForAction(players.First(), 10).Should().Be(5);
                ActionChecker.Instance.NeedAmountForAction(players.Last(), 10).Should().Be(0);
            }
        }

        private List<Player> SetupCleanPlayers()
            => Enumerable.Range(0,2).Select(t => new Player("uuid", 100)).ToList();

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
