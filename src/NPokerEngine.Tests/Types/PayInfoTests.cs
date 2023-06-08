using FluentAssertions;
using FluentAssertions.Execution;

namespace NPokerEngine.Tests.Types
{
    [TestClass]
    public class PayInfoTests
    {
        private PayInfo _payInfo;

        [TestInitialize]
        public void Initialize()
        {
            _payInfo = new PayInfo();
        }

        [TestMethod]
        public void UpdateByPayTest()
        {
            _payInfo.UpdateByPay(10);
            using (new AssertionScope())
            {
                _payInfo.Amount.Should().Be(10);
                _payInfo.Status.Should().Be(PayInfoStatus.PAY_TILL_END);
            }
        }

        [TestMethod]
        public void UpdateByAllInTest()
        {
            _payInfo.UpdateToAllin();
            using (new AssertionScope())
            {
                _payInfo.Amount.Should().Be(0);
                _payInfo.Status.Should().Be(PayInfoStatus.ALLIN);
            }
        }

        [TestMethod]
        public void UpdateToFoldTest()
        {
            _payInfo.UpdateToFold();
            using (new AssertionScope())
            {
                _payInfo.Amount.Should().Be(0);
                _payInfo.Status.Should().Be(PayInfoStatus.FOLDED);
            }
        }

        [TestMethod]
        public void SerializationTest()
        {
            _payInfo.UpdateByPay(100);
            _payInfo.UpdateToAllin();
            var copy = (PayInfo)_payInfo.Clone();
            using (new AssertionScope())
            {
                object.ReferenceEquals(copy, _payInfo).Should().BeFalse();
                copy.Amount.Should().Be(100);
                copy.Status.Should().Be(PayInfoStatus.ALLIN);
            }
        }
    }
}
