using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Types
{
    public enum ActionType : byte
    {
        FOLD = 0,
        CALL = 1,
        RAISE = 2,
        SMALL_BLIND = 3,
        BIG_BLIND = 4,
        ANTE = 5
    }
}
