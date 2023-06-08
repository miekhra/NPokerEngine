using FluentAssertions;
using FluentAssertions.Execution;
using System.Collections;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace NPokerEngine.Tests.Engine
{
    [TestClass]
    public class SidepotTests
    {

        [TestMethod]
        [TestDescription(" A: $50, B: $20(ALLIN),  C: $30(ALLIN) ")]
        public void Case1Test()
        {
            var players = new Dictionary<string, Player>()
            {
                { "A", CreatePlayerWithInfo("A", 50, PayInfoStatus.PAY_TILL_END ) },
                { "B", CreatePlayerWithInfo("B", 20, PayInfoStatus.ALLIN ) },
                { "C", CreatePlayerWithInfo("C", 30, PayInfoStatus.ALLIN ) }
            };

            var pots = GameEvaluator.Instance.CreatePot(players.Values);

            using (new AssertionScope())
            {
                pots.Count.Should().Be(3);
                SidePotCheck(players, pots[0], 60, new string[] { "A", "B", "C" });
                SidePotCheck(players, pots[1], 20, new string[] { "A", "C" });
                SidePotCheck(players, pots[2], 20, new string[] { "A" });
            }
        }

        [TestMethod]
        [TestDescription(" A: $10, B: $10,  C: $7(ALLIN) ")]
        public void Case2Test()
        {
            var players = new Dictionary<string, Player>()
            {
                { "A", CreatePlayerWithInfo("A", 10, PayInfoStatus.PAY_TILL_END ) },
                { "B", CreatePlayerWithInfo("B", 10, PayInfoStatus.PAY_TILL_END ) },
                { "C", CreatePlayerWithInfo("C", 7, PayInfoStatus.ALLIN ) }
            };

            var pots = GameEvaluator.Instance.CreatePot(players.Values);

            using (new AssertionScope())
            {
                pots.Count.Should().Be(2);
                SidePotCheck(players, pots[0], 21, new string[] { "A", "B", "C" });
                SidePotCheck(players, pots[1], 6, new string[] { "A", "B" });
            }
        }

        [TestMethod]
        [TestDescription(" A: $20(FOLD), B: $30, C: $7(ALLIN), D: $30 ")]
        public void Case3Test()
        {
            var players = new Dictionary<string, Player>()
            {
                { "A", CreatePlayerWithInfo("A", 20, PayInfoStatus.FOLDED ) },
                { "B", CreatePlayerWithInfo("B", 30, PayInfoStatus.PAY_TILL_END ) },
                { "C", CreatePlayerWithInfo("C", 7, PayInfoStatus.ALLIN ) },
                { "D", CreatePlayerWithInfo("D", 30, PayInfoStatus.PAY_TILL_END ) }
            };

            var pots = GameEvaluator.Instance.CreatePot(players.Values);

            using (new AssertionScope())
            {
                pots.Count.Should().Be(2);
                SidePotCheck(players, pots[0], 28, new string[] { "B", "C", "D" });
                SidePotCheck(players, pots[1], 59, new string[] { "B", "D" });
            }
        }

        [TestMethod]
        [TestDescription(" A: $12(ALLIN), B: $30, C: $7(ALLIN), D: $30 ")]
        public void Case4Test()
        {
            var players = new Dictionary<string, Player>()
            {
                { "A", CreatePlayerWithInfo("A", 12, PayInfoStatus.ALLIN ) },
                { "B", CreatePlayerWithInfo("B", 30, PayInfoStatus.PAY_TILL_END ) },
                { "C", CreatePlayerWithInfo("C", 7, PayInfoStatus.ALLIN ) },
                { "D", CreatePlayerWithInfo("D", 30, PayInfoStatus.PAY_TILL_END ) }
            };

            var pots = GameEvaluator.Instance.CreatePot(players.Values);

            using (new AssertionScope())
            {
                pots.Count.Should().Be(3);
                SidePotCheck(players, pots[0], 28, new string[] { "A", "B", "C", "D" });
                SidePotCheck(players, pots[1], 15, new string[] { "A", "B", "D" });
                SidePotCheck(players, pots[2], 36, new string[] { "B", "D" });
            }
        }

        [TestMethod]
        [TestDescription(" A: $5(ALLIN), B: $10, C: $8(ALLIN), D: $10, E: $2(FOLDED) ")]
        public void Case5Test()
        {
            var players = new Dictionary<string, Player>()
            {
                { "A", CreatePlayerWithInfo("A", 5, PayInfoStatus.ALLIN ) },
                { "B", CreatePlayerWithInfo("B", 10, PayInfoStatus.PAY_TILL_END ) },
                { "C", CreatePlayerWithInfo("C", 8, PayInfoStatus.ALLIN ) },
                { "D", CreatePlayerWithInfo("D", 10, PayInfoStatus.PAY_TILL_END ) },
                { "E", CreatePlayerWithInfo("E", 2, PayInfoStatus.FOLDED ) }
            };

            var pots = GameEvaluator.Instance.CreatePot(players.Values);

            using (new AssertionScope())
            {
                pots.Count.Should().Be(3);
                SidePotCheck(players, pots[0], 22, new string[] { "A", "B", "C", "D" });
                SidePotCheck(players, pots[1], 9, new string[] { "B", "C", "D" });
                SidePotCheck(players, pots[2], 4, new string[] { "B", "D" });
            }
        }

        private Player CreatePlayerWithInfo(string name, float amount, PayInfoStatus status)
        {
            var player = new Player("uuid", 100, name);
            player.PayInfo._amount = amount;
            player.PayInfo._status = status;
            return player;
        }

        private void SidePotCheck(Dictionary<string, Player> players, PotInfo pot, float amount, string[] eligibles)
        {
            amount.Should().Be(Convert.ToSingle(pot.Amount));
            eligibles.Length.Should().Be(((ICollection)pot.Eligibles).Count);
            eligibles.Should().BeEquivalentTo(((ICollection<Player>)pot.Eligibles).Select(t => t.Name));
        }
    }
}
