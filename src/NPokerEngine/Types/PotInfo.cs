using System.Collections.Generic;

namespace NPokerEngine.Types
{
    public class PotInfo
    {
        public float Amount { get; set; }
        public List<Player> Eligibles { get; set; }
    }
}
