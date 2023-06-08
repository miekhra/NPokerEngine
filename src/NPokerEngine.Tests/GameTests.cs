using FluentAssertions;
using NPokerEngine.Engine;
using NPokerEngine.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPokerEngine.Tests
{
    [TestClass]
    public class GameTests
    {
        [TestMethod]
        public void StartPokerTest()
        {
            var game = new Game();
            game.SetupConfig(1, 100, 10);
            var gameResult = game.StartPoker(Enumerable.Range(1, 2).Select(ix => new FoldPlayer($"p{ix}")).ToArray());

            gameResult.Seats.Players
                .Should()
                .SatisfyRespectively(
                    first => first.Should().BeEquivalentTo(new Player(string.Empty, 110, "p1"), options => options.Including(p => p.Stack).Including(p => p.Name)),
                    second => second.Should().BeEquivalentTo(new Player(string.Empty, 90, "p2"), options => options.Including(p => p.Stack).Including(p => p.Name))
                );
        }

        [TestMethod]
        public void StartPokerWithAnteTest()
        {
            var game = new Game();
            game.SetupConfig(1, 100, 10, 15);
            var gameResult = game.StartPoker(Enumerable.Range(1, 2).Select(ix => new FoldPlayer($"p{ix}")).ToArray());

            gameResult.Seats.Players
                .Should()
                .SatisfyRespectively(
                    first => first.Should().BeEquivalentTo(new Player(string.Empty, 125, "p1"), options => options.Including(p => p.Stack).Including(p => p.Name)),
                    second => second.Should().BeEquivalentTo(new Player(string.Empty, 75, "p2"), options => options.Including(p => p.Stack).Including(p => p.Name))
                );
        }

        [TestMethod]
        public void SetBlindStructureTest()
        {
            var game = new Game();
            game.SetupConfig(1, 100, 10);
            game.SetBlindStructure(new Dictionary<object, object> { { 1, new Dictionary<string, float> { { "ante", 5 }, { "small_blind", 10 } } } });
            var gameResult = game.StartPoker(Enumerable.Range(1, 2).Select(ix => new FoldPlayer($"p{ix}")).ToArray());

            gameResult.Seats.Players
                .Should()
                .SatisfyRespectively(
                    first => first.Should().BeEquivalentTo(new Player(string.Empty, 115, "p1"), options => options.Including(p => p.Stack).Including(p => p.Name)),
                    second => second.Should().BeEquivalentTo(new Player(string.Empty, 85, "p2"), options => options.Including(p => p.Stack).Including(p => p.Name))
                );
        }

        [TestMethod]
        public void StartPokerValidationWithNoPlayerTest()
        {
            var game = new Game();
            game.SetupConfig(1, 100, 10);

            Action startAction = () => game.StartPoker(null);

            startAction.Should().Throw<Exception>();
        }

        [TestMethod]
        public void StartPokerValidationWhenOnePlayerTest()
        {
            var game = new Game();
            game.SetupConfig(1, 100, 10);

            Action startAction = () => game.StartPoker(new FoldPlayer("p1"));

            startAction.Should().Throw<Exception>();
        }
    }
}
