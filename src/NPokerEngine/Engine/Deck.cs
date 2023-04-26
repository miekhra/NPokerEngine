﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NPokerEngine.Engine
{
    public class Deck
    {
        private List<Card> _deck;
        private readonly ReadOnlyCollection<int> _cheatCardIds;
        private int _popIndex = 0;
        private readonly bool _isCheat;

        public bool IsCheat => _isCheat;
        public int Size => (_deck.Count - _popIndex);

        public Deck(IEnumerable<int> cardIds = null, bool _cheat = false, IEnumerable<int> cheatCardIds = null)
        {
            _isCheat = _cheat;
            _cheatCardIds = cheatCardIds.ToList().AsReadOnly();
            _deck = cardIds != null ? cardIds.Select(Card.FromId).ToList() : SetupDeck();
        }

        public Card DrawCard()
        {
            var ix = _deck.Count - 1 - _popIndex++;
            return _deck[ix];
        }

        public void Shuffle()
        {
            if (_isCheat) return;
            if (_popIndex != 0) throw new InvalidOperationException($"{nameof(_popIndex)}={_popIndex}");

            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = _deck.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                var value = _deck[k];
                _deck[k] = _deck[n];
                _deck[n] = value;
            }
        }

        public void Restore() => _deck = SetupDeck();

        public List<Card> DrawCards(int num)
        {
            return Enumerable.Range(1, num).Select(t => DrawCard()).ToList();
        }

        private List<Card> SetupDeck()
        {
            _popIndex = 0;
            return IsCheat ? _cheatCardIds.Select(Card.FromId).ToList() : Enumerable.Range(1, 52).Select(Card.FromId).ToList();
        }

        // serialize format : [cheat_flg, cheat_card_ids, deck_card_ids]
        public string Serialize()
            => $"[{_isCheat}, {string.Join(";", _cheatCardIds.Select(t => t.ToString()))}, {string.Join(";", _deck.Skip(_popIndex).Select(t => t.ToId().ToString()))}]";

        public static Deck Deserialize(string serial)
        {
            var split = serial.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var isCheat = Convert.ToBoolean(split[0]);
            var cardIds = split[2].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries ).Select(t => Convert.ToInt32(t));
            var cheatCardIds = split[1].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(t => Convert.ToInt32(t));
            return new Deck(cardIds, isCheat, cheatCardIds);
        }
    }
}