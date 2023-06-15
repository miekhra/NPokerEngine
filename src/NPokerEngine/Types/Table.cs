using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NPokerEngine.Types
{
    public class Table : ICloneable
    {
        internal static string __playerNotFound = "not_found";
        public static string __exceedCardSizeMsg = "Community card is already full";

        private Deck _deck;
        internal int _dealerButton;
        internal int? _sbPosition, _bbPosition;
        internal Seats _seats;
        internal List<Card> _communityCards;

        public Seats Seats => _seats;
        public int DealerButton => _dealerButton;
        public Deck Deck => _deck;

        public int? SmallBlindPosition => _sbPosition;

        public int? BigBlindPosition => _bbPosition;

        public IReadOnlyCollection<Card> CommunityCards
            => _communityCards.AsReadOnly();

        public Table(Deck cheatDeck = null)
        {
            _dealerButton = 0;
            _sbPosition = null;
            _bbPosition = null;
            _seats = new Seats();
            _deck = cheatDeck ?? new Deck();
            _communityCards = new List<Card>();
        }

        public void SetBlindPositions(int? sbPosition, int? bbPosition)
        {
            _sbPosition = sbPosition;
            _bbPosition = bbPosition;
        }

        public void AddCommunityCard(Card card)
        {
            if (_communityCards.Count >= 5)
            {
                throw new ArgumentException(__exceedCardSizeMsg);
            }
            _communityCards.Add(card);
        }

        public void Reset()
        {
            _deck.Restore();
            _communityCards = new List<Card>();
            _seats.Players.ForEach(p =>
            {
                p.ClearHoleCard();
                p.ClearActionHistories();
                p.ClearPayInfo();
            });
        }

        public void ShiftDealerButton()
        {
            _dealerButton = NextActivePlayerPos(_dealerButton);
        }

        public int NextActivePlayerPos(int startPosition)
        {
            return FindEntitledPlayerPosition(startPosition, player => player.IsActive() && player.Stack != 0);
        }

        public int NextAskWaitingPlayerPosition(int startPosition)
        {
            return FindEntitledPlayerPosition(startPosition, player => player.IsWaitingAsk());
        }

        private int FindEntitledPlayerPosition(int startPosition, Func<Player, bool> checkMethod)
        {
            var searchTargets = _seats.Players
                .Concat(_seats.Players)
                .Skip(startPosition + 1)
                .Take(_seats.Players.Count)
                .ToList();
            Debug.Assert(searchTargets.Count == _seats.Players.Count);
            var matchedPlayers = searchTargets.Where(t => checkMethod(t));
            if (!matchedPlayers.Any())
            {
                return -1;
                //throw new KeyNotFoundException(__playerNotFound);
            }
            return _seats.Players.IndexOf(matchedPlayers.First());
        }

        public object Clone()
        {
            var clone = new Table();
            clone._deck = (Deck)_deck.Clone();
            clone._dealerButton = this._dealerButton;
            clone._sbPosition = this._sbPosition;
            clone._bbPosition = this._bbPosition;
            clone._seats = (Seats)this._seats.Clone();
            clone._communityCards = this.CommunityCards.Select(card => (Card)card.Clone()).ToList();
            return clone;
        }
    }
}
