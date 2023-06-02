﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPokerEngine.Types
{
    public class Seats : ICloneable
    {
        private readonly List<Player> _players;

        public int Size => _players.Count;
        public List<Player> Players => _players;

        public Seats()
        {
            _players = new List<Player>();
        }

        public void Sitdown(Player player)
            => _players.Add(player);

        public int ActivePlayersCount()
            => _players.Where(p => p.IsActive()).Count();

        public int AskWaitPlayersCount()
            => _players.Where(p => p.IsWaitingAsk()).Count();

        public object Clone()
        {
            var clone = new Seats();
            _players.ForEach(p => clone.Sitdown((Player)p.Clone()));
            return clone;
        }
    }
}