﻿using FluentAssertions;
using FluentAssertions.Execution;
using NPokerEngine.Engine;
using NPokerEngine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                _payInfo.Status.Should().Be(PayInfo.PAY_TILL_END);
            }
        }

        [TestMethod]
        public void UpdateByAllInTest()
        {
            _payInfo.UpdateToAllin();
            using (new AssertionScope())
            {
                _payInfo.Amount.Should().Be(0);
                _payInfo.Status.Should().Be(PayInfo.ALLIN);
            }
        }

        [TestMethod]
        public void UpdateToFoldTest()
        {
            _payInfo.UpdateToFold();
            using (new AssertionScope())
            {
                _payInfo.Amount.Should().Be(0);
                _payInfo.Status.Should().Be(PayInfo.FOLDED);
            }
        }

        [TestMethod]
        public void SerializationTest()
        {
            _payInfo.UpdateByPay(100);
            _payInfo.UpdateToAllin();
            var copy = (PayInfo)ObjectUtils.DeepCopyByReflection(_payInfo);
            using (new AssertionScope())
            {
                object.ReferenceEquals(copy, _payInfo).Should().BeFalse();
                copy.Amount.Should().Be(100);
                copy.Status.Should().Be(PayInfo.ALLIN);
            }
        }
    }
}