using NPokerEngine.Engine;
using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Players
{
    public abstract class PokerPlayer : BasePokerPlayer
    {
        public Guid Guid => Guid.Parse(Uuid);

        public PokerPlayer(string name, Guid uuid = default)
        {
            Name = name;
            Uuid = uuid == default ? Guid.NewGuid().ToString() : uuid.ToString();
        }
    }
}
