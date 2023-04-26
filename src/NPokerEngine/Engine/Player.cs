using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace NPokerEngine.Engine
{
    public class Player
    {
        public const string ACTION_FOLD_STR = "FOLD";
        public const string ACTION_CALL_STR = "CALL";
        public const string ACTION_RAISE_STR = "RAISE";
        public const string ACTION_SMALL_BLIND = "SMALLBLIND";
        public const string ACTION_BIG_BLIND = "BIGBLIND";
        public const string ACTION_ANTE = "ANTE";

        private static string __dupHoleMsg = "Hole card is already set";
        private static string __wrongNumHoleMsg = "You passed  %d hole cards";
        private static string __wrongTypeHoleMsg = "You passed not Card object as hole card";
        private static string __collectErrMsg = "Failed to collect %d chips. Because he has only %d chips";

        private readonly string _name;
        private readonly string _uuid;
        private List<Card> _holeCards;
        private int _stack;
        private PayInfo _payInfo;
        private Dictionary<StreetType, List<Dictionary<string, object>>>  _roundActionHistories;
        private List<Dictionary<string, object>> _actionHistories;

        public string Name => _name;
        public string Uuid => _uuid;
        public int Stack { get => _stack; set => _stack = value; }
        public PayInfo PayInfo => _payInfo;
        public List<Card> HoleCards => _holeCards;
        public List<Dictionary<string, object>> ActionHistories => _actionHistories;
        public Dictionary<StreetType, List<Dictionary<string, object>>> RoundActionHistories => _roundActionHistories;

		public Player(string uuid, int initialStack, string name = "No Name")
        {
            _uuid = uuid;
            _name = name;
            _stack = initialStack;
            _holeCards = new List<Card>();
            _payInfo = new PayInfo();
            _roundActionHistories = InitRoundActionHistories();
            _actionHistories = new List<Dictionary<string, object>>();
        }

        public void AddHoleCards(List<Card> cards)
        {
            if (_holeCards.Count != 0)
            {
                throw new ArgumentException(__dupHoleMsg);
            }
            if (cards.Count != 2)
            {
                throw new ArgumentException(String.Format(__wrongNumHoleMsg, cards.Count)); ;
            }
            this._holeCards = cards.ToList();
        }

        public void ClearHoleCard()
        {
            this._holeCards = new List<Card>();
        }

        public void AppendChip(int amount)
        {
            this._stack += amount;
        }

        public void CollectBet(int amount)
        {
            if (this._stack < amount)
            {
                throw new ArgumentException(String.Format(__collectErrMsg, amount, this._stack));
            }
            this._stack -= amount;
        }

        public bool IsActive()
        {
            return this._payInfo.Status != PayInfo.FOLDED;
        }

        public bool IsWaitingAsk()
        {
            return this._payInfo.Status == PayInfo.PAY_TILL_END;
        }

        public void AddActionHistory(ActionType kind, int chipAmount = 0, int addAmount = 0, int sbAmount = 0)
        {
            Dictionary<string, object> history = null;
            if (kind == ActionType.FOLD)
            {
                history = this.FoldHistory();
            }
            else if (kind == ActionType.CALL)
            {
                history = this.CallHistory(chipAmount);
            }
            else if (kind == ActionType.RAISE)
            {
                history = this.RaiseHistory(chipAmount, addAmount);
            }
            else if (kind == ActionType.SMALL_BLIND)
            {
                history = this.BlindHistory(true, sbAmount);
            }
            else if (kind == ActionType.BIG_BLIND)
            {
                history = this.BlindHistory(false, sbAmount);
            }
            else if (kind == ActionType.ANTE)
            {
                history = this.AnteHistory(chipAmount);
            }
            else
            {
                throw new ArgumentException(String.Format("UnKnown action history is added (kind = %s)", kind));
            }
            history["uuid"] = this._uuid;
            this._actionHistories.Add(history);
        }

        public void SaveStreetActionHistories(StreetType street)
        {
            this._roundActionHistories[street] = this._actionHistories;
            this._actionHistories = new List<Dictionary<string, object>>();
        }

        public void ClearActionHistories()
        {
            this._roundActionHistories = this.InitRoundActionHistories();
            this._actionHistories = new List<Dictionary<string, object>>();
        }

        public void ClearPayInfo()
        {
            this._payInfo = new PayInfo();
        }

        private Dictionary<StreetType, List<Dictionary<string, object>>> InitRoundActionHistories() 
            => new Dictionary<StreetType, List<Dictionary<string, object>>>();

        private Dictionary<string, object> FoldHistory()
            => new Dictionary<string, object> 
            {
                { "action", ACTION_FOLD_STR }
            };

        private Dictionary<string, object> CallHistory(int betAmount)
            => new Dictionary<string, object>
            {
                { "action", ACTION_CALL_STR },
                { "amount", betAmount },
                { "paid", betAmount - PaidSum() }
            };

        public Dictionary<string, object> RaiseHistory(int betAmount, int addAmount)
            => new Dictionary<string, object> 
            {
                { "action", ACTION_RAISE_STR },
                { "amount", betAmount },
                { "paid", betAmount - this.PaidSum()},
                { "add_amount",addAmount}
            };

        public Dictionary<string, object> BlindHistory(bool smallBlind, int sbAmount)
        {
            Debug.Assert(sbAmount != 0);
            var action = smallBlind ? ACTION_SMALL_BLIND : ACTION_BIG_BLIND;
            var amount = smallBlind ? sbAmount : sbAmount * 2;
            var add_amount = sbAmount;
            return new Dictionary<string, object> 
            {
                { "action", action },
                { "amount", amount },
                { "add_amount", add_amount }
            };
        }

        public Dictionary<string, object> AnteHistory(int payAmount)
        {
            Debug.Assert(payAmount > 0);
            return new Dictionary<string, object> 
            {
                { "action", ACTION_ANTE},
                { "amount", payAmount}
            };
        }

        private static string[] _nonPaidActions = { ACTION_FOLD_STR, ACTION_ANTE };

        public int PaidSum()
        {
            var payHistoryQuery = this._actionHistories.Where(t => !_nonPaidActions.Contains((string)t["action"])); //object.Equals(t["action"], ACTION_FOLD_STR) && object.Equals(t["action"], ACTION_ANTE));
            return payHistoryQuery.Any() ? Convert.ToInt32(payHistoryQuery.Last()["amount"]) : 0;
        }
    }
}
