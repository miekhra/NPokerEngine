using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Types
{
    public enum HandRankType : int
    {
        HIGHCARD = 0,
        ONEPAIR = 1 << 8,
        TWOPAIR = 1 << 9,
        THREECARD = 1 << 10,
        STRAIGHT = 1 << 11,
        FLASH = 1 << 12,
        FULLHOUSE = 1 << 13,
        FOURCARD = 1 << 14,
        STRAIGHTFLASH = 1 << 15
    }
}
