﻿using NPokerEngine.Types;
using System.Collections.Generic;
using System.Linq;

namespace NPokerEngine.Engine
{
    internal class ActionChecker : Singleton<ActionChecker>
    {
        private static List<ActionType> __raiseActionTypes = new List<ActionType>() { ActionType.RAISE, ActionType.SMALL_BLIND, ActionType.BIG_BLIND };

        public (ActionType action, float amount) CorrectAction(
            List<Player> players,
            int playerPosition,
            float sbAmount,
            ActionType action,
            float amount = 0)
        {
            if (this.IsAllin(players[playerPosition], action, amount))
            {
                amount = players[playerPosition].Stack + players[playerPosition].PaidSum();
            }
            else if (this.IsIlLegal(players, playerPosition, sbAmount, action, amount))
            {
                action = ActionType.FOLD;
                amount = 0;
            }
            return (action, amount);
        }

        public bool IsAllin(Player player, ActionType action, float betAmount)
        {
            if (action == ActionType.CALL)
            {
                return betAmount >= player.Stack + player.PaidSum();
            }
            else if (action == ActionType.RAISE)
            {
                return betAmount == player.Stack + player.PaidSum();
            }
            else
            {
                return false;
            }
        }

        public float NeedAmountForAction(Player player, float amount)
        {
            return amount - player.PaidSum();
        }

        public Dictionary<ActionType, AmountInterval> LegalActions(List<Player> players, int playerPosition, float sbAmount)
        {
            var min_raise = this.MinRaiseAmount(players, sbAmount);
            var max_raise = players[playerPosition].Stack + players[playerPosition].PaidSum();
            if (max_raise < min_raise)
            {
                min_raise = max_raise = -1;
            }
            return new Dictionary<ActionType, AmountInterval>
            {
                { ActionType.FOLD, AmountInterval.Empty },
                { ActionType.CALL, new AmountInterval(this.AgreeAmount(players)) },
                { ActionType.RAISE, new AmountInterval(min_raise, max_raise) }
            };
        }

        internal bool IsLegal(
            List<Player> players,
            int playerPosition,
            int sbAmount,
            ActionType action,
            int amount = 0)
        {
            return !this.IsIlLegal(players, playerPosition, sbAmount, action, amount);
        }

        internal bool IsIlLegal(
            List<Player> players,
            int playerPosition,
            float sbAmount,
            ActionType action,
            float amount = 0)
        {
            if (action == ActionType.FOLD)
            {
                return false;
            }
            else if (action == ActionType.CALL)
            {
                return this.IsShortOfMoney(players[playerPosition], amount) || this.IsIlLegalCall(players, amount);
            }
            else if (action == ActionType.RAISE)
            {
                return this.IsShortOfMoney(players[playerPosition], amount) || this.IsIllegalRaise(players, amount, sbAmount);
            }
            return false;
        }

        public float AgreeAmount(IEnumerable<Player> players)
        {
            var last_raise = this.FetchLastRaise(players);
            return last_raise != null ? last_raise.Amount : 0;
        }

        private bool IsIlLegalCall(IEnumerable<Player> players, float amount)
        {
            return amount != this.AgreeAmount(players);
        }

        private bool IsIllegalRaise(IEnumerable<Player> players, float amount, float sbAmount)
        {
            return this.MinRaiseAmount(players, sbAmount) > amount;
        }

        private float MinRaiseAmount(IEnumerable<Player> players, float sbAmount)
        {
            var raise = this.FetchLastRaise(players);
            // the least min_raise allowed is BB
            //return raise != null ? (float)raise["amount"] + Math.Max((float)raise["add_amount"], (float)(sbAmount * 2)) : sbAmount * 2;
            return raise != null ? (raise.Amount + raise.AddAmount) : (sbAmount * 2);
        }

        private bool IsShortOfMoney(Player player, float amount)
        {
            return player.Stack < amount - player.PaidSum();
        }

        private ActionHistoryEntry FetchLastRaise(IEnumerable<Player> players)
        {
            var raiseHistoriesQuery = players
                .SelectMany(p => p.ActionHistories.Select(h => h))
                .Where(h => h != null)
                .Where(h => __raiseActionTypes.Contains(h.ActionType))
                .ToList();

            if (!raiseHistoriesQuery.Any())
            {
                return null;
            }

            var maxAmount = raiseHistoriesQuery.Select(h => h.Amount).Max();

            return raiseHistoriesQuery.First(h => h.Amount == maxAmount);
        }
    }
}
