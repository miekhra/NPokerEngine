using System;
using System.Diagnostics;

namespace NPokerEngine.Types
{
    [DebuggerDisplay("{ActionType},{Amount},{AddAmount},{Paid}")]
    public class ActionHistoryEntry : ICloneable
    {
        public string Uuid { get; set; }
        public ActionType ActionType { get; set; }
        public float Amount { get; set; }
        public float Paid { get; set; }
        public float AddAmount { get; set; }

        public object Clone()
            => new ActionHistoryEntry
            {
                Uuid = Uuid,
                ActionType = ActionType,
                Amount = Amount,
                AddAmount = AddAmount,
                Paid = Paid
            };
    }
}
