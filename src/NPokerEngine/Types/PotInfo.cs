using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Types
{
    public class PotInfo
    {
        public float Amount { get; set; }
        public List<Player> Eligibles { get; set; }
    }
}
