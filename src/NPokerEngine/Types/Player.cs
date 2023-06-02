using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace NPokerEngine.Types
{
    [DebuggerDisplay("{Name},{Uuid},{Stack}")]
    public class Player : ICloneable
    {
        private static ActionType[] _nonPaidActions = { ActionType.FOLD, ActionType.ANTE };
        private static string __dupHoleMsg = "Hole card is already set";
        private static string __wrongNumHoleMsg = "You passed  %d hole cards";
        private static string __wrongTypeHoleMsg = "You passed not Card object as hole card";
        private static string __collectErrMsg = "Failed to collect %d chips. Because he has only %d chips";

        private readonly string _name;
        private readonly string _uuid;
        private List<Card> _holeCards;
        private float _stack;
        private PayInfo _payInfo;
        private Dictionary<StreetType, List<ActionHistoryEntry>> _roundActionHistories;
        private List<ActionHistoryEntry> _actionHistories;

        public string Name => _name;
        public string Uuid => _uuid;
        public float Stack { get => _stack; set => _stack = value; }
        public PayInfo PayInfo => _payInfo;
        public List<Card> HoleCards => _holeCards;
        public List<ActionHistoryEntry> ActionHistories => _actionHistories;
        public Dictionary<StreetType, List<ActionHistoryEntry>> RoundActionHistories => _roundActionHistories;
        public ActionHistoryEntry LastActionHistory => _actionHistories.LastOrDefault();

        public Player(string uuid, int initialStack, string name = "No Name")
        {
            _uuid = uuid;
            _name = name;
            _stack = initialStack;
            _holeCards = new List<Card>();
            _payInfo = new PayInfo();
            _roundActionHistories = InitRoundActionHistories();
            _actionHistories = new List<ActionHistoryEntry>();
        }

        public void AddHoleCards(params Card[] cards)
        {
            if (_holeCards.Count != 0)
            {
                throw new ArgumentException(__dupHoleMsg);
            }
            if (cards.Length != 2)
            {
                throw new ArgumentException(String.Format(__wrongNumHoleMsg, cards.Length)); ;
            }
            this._holeCards = cards.ToList();
        }

        public void ClearHoleCard()
        {
            this._holeCards = new List<Card>();
        }

        public void AppendChip(float amount)
        {
            this._stack += amount;
        }

        public void CollectBet(float amount)
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

        public void AddActionHistory(ActionType kind, float chipAmount = 0, float addAmount = 0, float sbAmount = 0)
        {
            ActionHistoryEntry history = null;
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
            history.Uuid = this._uuid;
            this._actionHistories.Add(history);
        }

        public void SaveStreetActionHistories(StreetType street)
        {
            this._roundActionHistories[street] = this._actionHistories;
            this._actionHistories = new List<ActionHistoryEntry>();
        }

        public void ClearActionHistories()
        {
            this._roundActionHistories = this.InitRoundActionHistories();
            this._actionHistories = new List<ActionHistoryEntry>();
        }

        public void ClearPayInfo()
        {
            this._payInfo = new PayInfo();
        }

        private Dictionary<StreetType, List<ActionHistoryEntry>> InitRoundActionHistories()
            => new Dictionary<StreetType, List<ActionHistoryEntry>>();

        private ActionHistoryEntry FoldHistory()
            => new ActionHistoryEntry
            {
                ActionType = ActionType.FOLD
            };

        private ActionHistoryEntry CallHistory(float betAmount)
            => new ActionHistoryEntry
            {
                ActionType = ActionType.CALL,
                Amount = betAmount,
                Paid = betAmount - this.PaidSum()
            };

        public ActionHistoryEntry RaiseHistory(float betAmount, float addAmount)
            => new ActionHistoryEntry
            {
                ActionType = ActionType.RAISE,
                Amount = betAmount,
                Paid = betAmount - this.PaidSum(),
                AddAmount = addAmount
            };

        public ActionHistoryEntry BlindHistory(bool smallBlind, float sbAmount)
        {
            Debug.Assert(sbAmount != 0);
            var action = smallBlind ? ActionType.SMALL_BLIND : ActionType.BIG_BLIND;
            var amount = smallBlind ? sbAmount : sbAmount * 2;
            var add_amount = sbAmount;
            return new ActionHistoryEntry
            {
                ActionType = action,
                Amount = amount,
                AddAmount = add_amount
            };
        }

        public ActionHistoryEntry AnteHistory(float payAmount)
        {
            Debug.Assert(payAmount > 0);
            return new ActionHistoryEntry
            {
                ActionType = ActionType.ANTE,
                Amount = payAmount,
            };
        }

        public float PaidSum()
        {
            var payHistoryQuery = this._actionHistories.Where(t => t != null).Where(t => !_nonPaidActions.Contains(t.ActionType));
            return payHistoryQuery.Any() ? Convert.ToSingle(payHistoryQuery.Last().Amount) : 0f;
        }

        public object Clone()
        {
            var clone = new Player(_uuid, (int)_stack, Name);
            clone._holeCards = _holeCards.Select(c => Card.FromId(c.ToId())).ToList();
            foreach (var h in _actionHistories)
            {
                clone._actionHistories.Add((ActionHistoryEntry)h.Clone());
            }
            foreach (var roundHistories in _roundActionHistories)
            {
                clone._roundActionHistories[roundHistories.Key] = new List<ActionHistoryEntry>();
                foreach (var h in roundHistories.Value)
                {
                    clone._roundActionHistories[roundHistories.Key].Add((ActionHistoryEntry)h.Clone());
                }
            }
            clone._payInfo._amount = _payInfo._amount;
            clone._payInfo._status = _payInfo._status;
            return clone;
        }
    }
}
