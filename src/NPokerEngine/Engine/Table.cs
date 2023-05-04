using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NPokerEngine.Engine
{
    public class Table
    {
        internal static string __playerNotFound = "not_found";
        public static string __exceedCardSizeMsg = "Community card is already full";

        private Deck _deck;
        internal int _dealerButton;
        private int? _sbPosition, _bbPosition;
        internal Seats _seats;
        private List<Card> _communityCards;

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
            this._dealerButton = this.NextActivePlayerPos(this._dealerButton);
        }

        public int NextActivePlayerPos(int startPosition)
        {
            return this.FindEntitledPlayerPosition(startPosition, player => player.IsActive() && player.Stack != 0);
        }

        public int NextAskWaitingPlayerPosition(int startPosition)
        {
            return this.FindEntitledPlayerPosition(startPosition, player => player.IsWaitingAsk());
        }

        private int FindEntitledPlayerPosition(int startPosition, Func<Player, bool> checkMethod)
        {
            var searchTargets = this._seats.Players
                .Concat(this._seats.Players)
                .Skip(startPosition + 1)
                .Take(this._seats.Players.Count)
                .ToList();
            Debug.Assert(searchTargets.Count == this._seats.Players.Count);
            var matchedPlayers = searchTargets.Where(t => checkMethod(t));
            if (!matchedPlayers.Any())
            {
                return -1;
                //throw new KeyNotFoundException(__playerNotFound);
            }
            return this._seats.Players.IndexOf(matchedPlayers.First());
        }
    }
}
