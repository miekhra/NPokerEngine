using FluentAssertions;
using FluentAssertions.Execution;
using NPokerEngine.Engine;
using NPokerEngine.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace NPokerEngine.Tests.Engine
{
    [TestClass]
    public class PlayerTests
    {
        private Player _player;

        [TestInitialize]
        public void Initialize()
        {
            _player = new Player("uuid", 100);
        }

        [TestMethod]
        public void AddHoleCardsTest()
        {
            var cards = Enumerable.Range(1, 2).Select(Card.FromId).ToArray();
            _player.AddHoleCards(cards);
            using (new AssertionScope())
            {
                _player.HoleCards.Contains(cards[0]).Should().BeTrue();
                _player.HoleCards.Contains(cards[1]).Should().BeTrue();
            }
        }

        [TestMethod]
        public void AddSingleHoleCardTest()
        {
            Action act = () => _player.AddHoleCards(Card.FromId(1));

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void AddTooManyHoleCardsTest()
        {
            Action act = () => _player.AddHoleCards(Enumerable.Range(1,3).Select(Card.FromId).ToArray());

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void AddHoleCardTwiceTest()
        {
            Action act = () =>
            {
                _player.AddHoleCards(Enumerable.Range(1, 2).Select(Card.FromId).ToArray());
                _player.AddHoleCards(Enumerable.Range(1, 2).Select(Card.FromId).ToArray());
            };

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void ClearHoleCardTest()
        {
            _player.AddHoleCards(Enumerable.Range(1, 2).Select(Card.FromId).ToArray());
            _player.ClearHoleCard();
            _player.HoleCards.Count.Should().Be(0);
        }

        [TestMethod]
        public void AppendChipTest()
        {
            _player.AppendChip(10);
            _player.Stack.Should().Be(110);
        }

        [TestMethod]
        public void CollectBetTest()
        {
            _player.CollectBet(10);
            _player.Stack.Should().Be(90);
        }

        [TestMethod]
        public void CollectTooMuchBetTest()
        {
            Action act = () => _player.CollectBet(200);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void IsActiveTest()
        {
            _player.PayInfo.UpdateByPay(10);

            _player.IsActive().Should().BeTrue();
        }

        [TestMethod]
        public void IfAllinPlayerIsActiveTest()
        {
            _player.PayInfo.UpdateToAllin();

            _player.IsActive().Should().BeTrue();
        }

        [TestMethod]
        public void IfFoldedPlayerIsNotActiveTest()
        {
            _player.PayInfo.UpdateToFold();

            _player.IsActive().Should().BeFalse();
        }

        [TestMethod]
        public void IfNoMoneyPlayerIsActiveTest()
        {
            _player.CollectBet(100);

            _player.IsActive().Should().BeTrue();
        }

        [TestMethod]
        public void IsWaitingAskTest()
        {
            _player.PayInfo.UpdateByPay(10);

            _player.IsWaitingAsk().Should().BeTrue();
        }

        [TestMethod]
        public void IsAllinPlayerIsNotWaitingAskTest()
        {
            _player.PayInfo.UpdateToAllin();

            _player.IsWaitingAsk().Should().BeFalse();
        }

        [TestMethod]
        public void IsFoldedPlayerIsNotWaitingAskTest()
        {
            _player.PayInfo.UpdateToFold();

            _player.IsWaitingAsk().Should().BeFalse();
        }

        [TestMethod]
        public void AddFoldActionHistoryTest()
        {
            _player.AddActionHistory(ActionType.FOLD);

            _player.ActionHistories.Last().ActionType.Should().Be(ActionType.FOLD);
        }

        [TestMethod]
        public void AddCallActionHistoryTest()
        {
            _player.AddActionHistory(ActionType.CALL, 10);

            using (new AssertionScope())
            {
                _player.LastActionHistory.ActionType.Should().Be(ActionType.CALL);
                _player.LastActionHistory.Amount.Should().Be(10);
                _player.LastActionHistory.Paid.Should().Be(10);
            }
        }

        [TestMethod]
        public void AddCallActionHistoryAfterPaidTest()
        {
            _player.AddActionHistory(ActionType.CALL, 10);
            _player.AddActionHistory(ActionType.CALL, 20);

            using (new AssertionScope())
            {
                _player.LastActionHistory.Amount.Should().Be(20);
                _player.LastActionHistory.Paid.Should().Be(10);
            }
        }

        [TestMethod]
        public void AddRaiseActionHistoryTest()
        {
            _player.AddActionHistory(ActionType.RAISE, 10, 5);

            using (new AssertionScope())
            {
                _player.LastActionHistory.ActionType.Should().Be(ActionType.RAISE);
                _player.LastActionHistory.Amount.Should().Be(10);
                _player.LastActionHistory.Paid.Should().Be(10);
                _player.LastActionHistory.AddAmount.Should().Be(5);
            }
        }

        [TestMethod]
        public void AddRaiseActionHistoryAfterPaidTest()
        {
            _player.AddActionHistory(ActionType.CALL, 10);
            _player.AddActionHistory(ActionType.RAISE, 20, 10);

            using (new AssertionScope())
            {
                _player.LastActionHistory.Amount.Should().Be(20);
                _player.LastActionHistory.Paid.Should().Be(10);
            }
        }

        [TestMethod]
        public void AddSmallBlindHistoryTest()
        {
            _player.AddActionHistory(ActionType.SMALL_BLIND, sbAmount: 5);

            using (new AssertionScope())
            {
                _player.LastActionHistory.ActionType.Should().Be(ActionType.SMALL_BLIND);
                _player.LastActionHistory.Amount.Should().Be(5);
                _player.LastActionHistory.AddAmount.Should().Be(5);
            }
        }

        [TestMethod]
        public void AddBigBlindHistoryTest()
        {
            _player.AddActionHistory(ActionType.BIG_BLIND, sbAmount: 5);

            using (new AssertionScope())
            {
                _player.LastActionHistory.ActionType.Should().Be(ActionType.BIG_BLIND);
                _player.LastActionHistory.Amount.Should().Be(10);
                _player.LastActionHistory.AddAmount.Should().Be(5);
            }
        }

        [TestMethod]
        public void AddAnteHistoryTest()
        {
            _player.AddActionHistory(ActionType.ANTE, 10);

            using (new AssertionScope())
            {
                _player.LastActionHistory.ActionType.Should().Be(ActionType.ANTE);
                _player.LastActionHistory.Amount.Should().Be(10);
            }
        }

        [TestMethod]
        public void AddEmptyAnteHistoryTest()
        {
            Action act = () => _player.AddActionHistory(ActionType.ANTE, 0);

            act.Should().Throw<Exception>();
        }

        [TestMethod]
        public void SaveStreetActionHistoriesTest()
        {
            using (new AssertionScope())
            {
                _player.RoundActionHistories.Should().NotContainKey(StreetType.PREFLOP);

                _player.AddActionHistory(ActionType.BIG_BLIND, sbAmount: 5);
                _player.SaveStreetActionHistories(StreetType.PREFLOP);

                _player.RoundActionHistories[StreetType.PREFLOP].Should().HaveCount(1);
                _player.RoundActionHistories[StreetType.PREFLOP][0].ActionType.Should().Be(ActionType.BIG_BLIND);

                _player.ActionHistories.Should().HaveCount(0);
            }
        }

        [TestMethod]
        public void ClearActionHistoriesTest()
        {
            _player.AddActionHistory(ActionType.BIG_BLIND, sbAmount: 5);
            _player.SaveStreetActionHistories(StreetType.PREFLOP);
            _player.AddActionHistory(ActionType.CALL, sbAmount: 10);

            using (new AssertionScope())
            {
                _player.RoundActionHistories.Should().ContainKey(StreetType.PREFLOP);
                _player.ActionHistories.Should().NotBeEmpty();
                _player.ClearActionHistories();
                _player.RoundActionHistories.Should().NotContainKey(StreetType.PREFLOP);
                _player.ActionHistories.Should().BeEmpty();
            }
        }

        [TestMethod]
        public void PaidSumTest()
        {
            using (new AssertionScope())
            {
                _player.PaidSum().Should().Be(0);
                _player.AddActionHistory(ActionType.BIG_BLIND, sbAmount: 5);
                _player.PaidSum().Should().Be(10);
                _player.ClearActionHistories();
                _player.PaidSum().Should().Be(0);
                _player.AddActionHistory(ActionType.ANTE, 3);
                _player.PaidSum().Should().Be(0);
                _player.AddActionHistory(ActionType.BIG_BLIND, sbAmount: 5);
                _player.PaidSum().Should().Be(10);
            }
        }

        [TestMethod]
        public void SerializationTest()
        {
            var player = SetupPlayerForSerialization();

            var clone = (Player)ObjectUtils.DeepCopyByReflection(player);

            using (new AssertionScope())
            {
                player.Name.Should().Be(clone.Name);
                player.Uuid.Should().Be(clone.Uuid);
                player.Stack.Should().Be(clone.Stack);
                player.HoleCards.Should().BeEquivalentTo(clone.HoleCards);
                player.ActionHistories.Should().BeEquivalentTo(clone.ActionHistories);
                player.RoundActionHistories.Should().BeEquivalentTo(clone.RoundActionHistories);
                player.PayInfo.Amount.Should().Be(clone.PayInfo.Amount);
                player.PayInfo.Status.Should().Be(clone.PayInfo.Status);
            }
        }

        private Player SetupPlayerForSerialization()
        {
            var player = new Player("uuid", 50, "hoge");
            player.AddHoleCards(Enumerable.Range(1,2).Select(Card.FromId).ToArray());
            player.AddActionHistory(ActionType.SMALL_BLIND, sbAmount: 5);
            player.SaveStreetActionHistories(StreetType.PREFLOP);
            player.AddActionHistory(ActionType.CALL, 10);
            player.AddActionHistory(ActionType.RAISE, 10, 5);
            player.AddActionHistory(ActionType.FOLD);
            player.PayInfo.UpdateByPay(15);
            player.PayInfo.UpdateToFold();
            return player;
        }
    }
}
