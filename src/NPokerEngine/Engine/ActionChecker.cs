using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPokerEngine.Engine
{
    public class ActionChecker
    {
        private static List<string> __raiseActionNames = new List<string>() { "RAISE", "SMALLBLIND", "BIGBLIND" };

        private static ActionChecker _instance;
        public static ActionChecker Instance
        {
            get
            {
                _instance = _instance ?? new ActionChecker();
                return _instance;
            }
        }

        private ActionChecker() { }

        public Tuple<string, int> CorrectAction(
            List<Player> players,
            int playerPosition,
            int sbAmount,
            string action,
            int amount = 0)
        {
            if (this.IsAllin(players[playerPosition], action, amount))
            {
                amount = players[playerPosition].Stack + players[playerPosition].PaidSum();
            }
            else if (this.IsIlLegal(players, playerPosition, sbAmount, action, amount))
            {
                action = "fold";
                amount = 0;
            }
            return Tuple.Create(action, amount);
        }

        public bool IsAllin(Player player, string action, int betAmount)
        {
            if (action == "call")
            {
                return betAmount >= player.Stack + player.PaidSum();
            }
            else if (action == "raise")
            {
                return betAmount == player.Stack + player.PaidSum();
            }
            else
            {
                return false;
            }
        }

        public int NeedAmountForAction(Player player, int amount)
        {
            return amount - player.PaidSum();
        }

        public List<Dictionary<string, object>> LegalActions(List<Player> players, int playerPosition, int sbAmount)
        {
            var min_raise = this.MinRaiseAmount(players, sbAmount);
            var max_raise = players[playerPosition].Stack + players[playerPosition].PaidSum();
            if (max_raise < min_raise)
            {
                min_raise = -1;
            }
            return new List<Dictionary<string, object>> {
                    new Dictionary<string, object> {
                        {
                            "action",
                            "fold"},
                        {
                            "amount",
                            0}},
                    new Dictionary<string, object> {
                        {
                            "action",
                            "call"},
                        {
                            "amount",
                            this.AgreeAmount(players)}},
                    new Dictionary<string, object> {
                        {
                            "action",
                            "raise"},
                        {
                            "amount",
                            new Dictionary<object, object> {
                                {
                                    "min",
                                    min_raise},
                                {
                                    "max",
                                    max_raise}}}}
                };
        }

        internal bool IsLegal(
            List<Player> players,
            int playerPosition,
            int sbAmount,
            string action,
            int amount = 0)
        {
            return !this.IsIlLegal(players, playerPosition, sbAmount, action, amount);
        }

        internal bool IsIlLegal(
            List<Player> players,
            int playerPosition,
            int sbAmount,
            string action,
            int amount = 0)
        {
            if (action == "fold")
            {
                return false;
            }
            else if (action == "call")
            {
                return this.IsShortOfMoney(players[playerPosition], amount) || this.IsIlLegalCall(players, amount);
            }
            else if (action == "raise")
            {
                return this.IsShortOfMoney(players[playerPosition], amount) || this.IsIllegalRaise(players, amount, sbAmount);
            }
            return false;
        }

        public int AgreeAmount(IEnumerable<Player> players)
        {
            var last_raise = this.FetchLastRaise(players);
            return last_raise != null ? (int)last_raise["amount"] : 0;
        }

        private bool IsIlLegalCall(IEnumerable<Player> players, int amount)
        {
            return amount != this.AgreeAmount(players);
        }

        private bool IsIllegalRaise(IEnumerable<Player> players, int amount, int sbAmount)
        {
            return this.MinRaiseAmount(players, sbAmount) > amount;
        }

        private int MinRaiseAmount(IEnumerable<Player> players, int sbAmount)
        {
            var raise = this.FetchLastRaise(players);
            // the least min_raise allowed is BB
            return raise != null ? (int)raise["amount"] + Math.Max((int)raise["add_amount"], (int)(sbAmount * 2)) : sbAmount * 2;
        }

        private bool IsShortOfMoney(Player player, int amount)
        {
            return player.Stack < amount - player.PaidSum();
        }

        private Dictionary<string, object> FetchLastRaise(IEnumerable<Player> players)
        {
            var raiseHistoriesQuery = players
                .SelectMany(p => p.ActionHistories.Select(h => h))
                .Where(h => __raiseActionNames.Contains(h["action"]))
                .ToList();

            if (!raiseHistoriesQuery.Any())
            {
                return null;
            }

            var maxAmount = raiseHistoriesQuery.Select(h => h["amount"]).Max();

            return raiseHistoriesQuery.First(h => h["amount"] == maxAmount);
        }
    }
}
