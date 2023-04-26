using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Engine
{
    public enum StreetType : byte
    {
        PREFLOP = 0,
        FLOP = 1,
        TURN = 2,
        RIVER = 3,
        SHOWDOWN = 4,
        FINISHED = 5
    }
}
