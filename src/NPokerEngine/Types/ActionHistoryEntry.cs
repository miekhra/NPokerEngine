using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Types
{
    public class ActionHistoryEntry
    {
        public string Uuid { get; set; }
        public ActionType ActionType { get; set; }
        public float Amount { get; set; }
        public float Paid { get; set; }
        public float AddAmount { get; set; }
    }
}
