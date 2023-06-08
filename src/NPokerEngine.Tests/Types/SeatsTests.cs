using FluentAssertions;

namespace NPokerEngine.Tests.Types
{
    [TestClass]
    public class SeatsTests
    {
        private Seats _seats;
        private Player _p1, _p2, _p3;

        [TestInitialize]
        public void Initialize()
        {
            _seats = new Seats();
            _p1 = new Player("uuid1", 100);
            _p2 = new Player("uuid2", 100);
            _p3 = new Player("uuid3", 100);
        }

        [TestMethod]
        public void SitdownTest()
        {
            _seats.Sitdown(_p1);
            _seats.Players.Contains(_p1).Should().BeTrue();
        }

        [TestMethod]
        public void SizeTest()
        {
            SitdownPlayers();
            _seats.Players.Count().Should().Be(3);
        }

        [TestMethod]
        public void ActivePlayersCountTest()
        {
            SetupPayStatus();
            SitdownPlayers();
            _seats.ActivePlayersCount().Should().Be(2);
        }

        [TestMethod]
        public void AcountAskWaitPlayersTest()
        {
            SetupPayStatus();
            SitdownPlayers();
            _seats.AskWaitPlayersCount().Should().Be(1);
        }

        [TestMethod]
        public void SerializationTest()
        {
            SitdownPlayers();
            var clone = (Seats)_seats.Clone();
            clone.Players
                .Select((p, ix) => new { ix, p })
                .All(obj => ReferenceEquals(obj.p.Clone(), _seats.Players[obj.ix]))
                .Should().BeFalse();
        }

        private void SetupPayStatus()
        {
            _p1.PayInfo.UpdateByPay(10);
            _p2.PayInfo.UpdateToFold();
            _p3.PayInfo.UpdateToAllin();
        }

        private void SitdownPlayers()
        {
            _seats.Sitdown(_p1);
            _seats.Sitdown(_p2);
            _seats.Sitdown(_p3);
        }
    }
}
