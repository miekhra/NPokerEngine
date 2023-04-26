using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPokerEngine.Engine
{
    public class GameEvaluator
    {
        private static GameEvaluator _instance;
        public static GameEvaluator Instance 
        { 
            get 
            {
				_instance = _instance ?? new GameEvaluator();
				return _instance; 
            } 
        }

		private GameEvaluator() { }

		public (List<Player> winners, List<Dictionary<string, object>> handInfo, Dictionary<int, int> prizeMap) Judge(Table table)
        {
            var winners = this.FindWinnersFrom(table.Seats.Players, table.CommunityCars);
            var hand_info = this.GenHandInfoIfNeeded(table.Seats.Players, table.CommunityCars);
            var prize_map = this.CalcPrizeDistribution(table.Seats.Players, table.CommunityCars);
            return (winners, hand_info, prize_map);
        }

        public List<Dictionary<string, object>> CreatePot(IEnumerable<Player> players)
        {
            var sidePots = this.GetSidePots(players);
            var mainPot = this.GetMainPot(players, sidePots);
            sidePots.Add(mainPot);
            return sidePots;
        }

        private Dictionary<int, int> CalcPrizeDistribution(IEnumerable<Player> players, IEnumerable<Card> community)
        {
            var prize_map = this.CreatePrizeMap(players.Count());
            var pots = this.CreatePot(players);
            foreach (var pot in pots)
            {
                var winners = this.FindWinnersFrom((IEnumerable<Player>)pot["eligibles"], community);
                var prize = Convert.ToInt32((int)pot["amount"] / winners.Count);
                foreach (var winner in winners)
                {
                    prize_map[Array.IndexOf(players.ToArray(), winner)] += prize;
                }
            }
            return prize_map;
        }

        private Dictionary<int, int> CreatePrizeMap(int playerNum)
        {
            return Enumerable.Range(0, playerNum).ToDictionary(k => k, v => 0);
        }

        private List<Player> FindWinnersFrom(IEnumerable<Player> players, IEnumerable<Card> community)
        {
            Func<Player, int> scorePlayer = player => HandEvaluator.Instance.EvalHand(player.HoleCards, community);
            var activePlayers = (from player in players
                                  where player.IsActive()
                                  select player).ToList();
            var scores = (from player in activePlayers
                          select scorePlayer(player)).ToList();
            var bestScore = scores.Max();
            var scoreWithPlayers = scores.Zip(activePlayers, (score, player) => Tuple.Create(score, player));
            var winners = scoreWithPlayers.Where(t => t.Item1 == bestScore).Select(t => t.Item2).ToList();
            return winners;
        }

        private List<Dictionary<string, object>> GenHandInfoIfNeeded(IEnumerable<Player> players, IEnumerable<Card> community)
        {
            var activePlayers = (from player in players
                                  where player.IsActive()
                                  select player).ToList();
            Func<Player, Dictionary<string, object>> genHandInfo = player => new Dictionary<string, object> 
            {
                {
                    "uuid",
                    player.Uuid},
                {
                    "hand",
                    HandEvaluator.Instance.GenHandRankInfo(player.HoleCards, community)
                }
            };
            return activePlayers.Count == 1 ? new List<Dictionary<string, object>>() : (from player in activePlayers
                                                                                        select genHandInfo(player)).ToList();
        }

        private Dictionary<string, object> GetMainPot(IEnumerable<Player> players, IEnumerable<Dictionary<string, object>> sidepots)
        {
            var maxPay = (from pay in this.GetPayInfo(players)
                               select pay.Amount).Max();
            return new Dictionary<string, object> 
            {
                {
                    "amount",
                    this.GetPlayersPaySum(players) - this.GetSidepotsSum(sidepots)
                },
                {
                    "eligibles",
                    (from player in players
                        where player.PayInfo.Amount == maxPay
                        select player).ToList()
                }
            };
        }

        private int GetPlayersPaySum(IEnumerable<Player> players)
        {
            return (from pay in this.GetPayInfo(players)
                    select pay.Amount).ToList().Sum();
        }

        private List<Dictionary<string, object>> GetSidePots(IEnumerable<Player> players)
        {
            var payAmounts = (from payinfo in this.FetchAllInPayInfo(players)
                               select payinfo.Amount).ToList();

            var sidePots = new List<Dictionary<string, object>>();
            foreach (var payAmount in payAmounts)
            {
                sidePots.Add(this.CreateSidepot(players, sidePots, payAmount));
            }

            return sidePots;
        }

        private Dictionary<string, object> CreateSidepot(IEnumerable<Player> players, IEnumerable<Dictionary<string, object>> smallerSidePots, int allinAmount)
        {
            return new Dictionary<string, object> {
                    {
                        "amount",
                        this.CalcSidepotSize(players, smallerSidePots, allinAmount)},
                    {
                        "eligibles",
                        this.SelectEligibles(players, allinAmount)}};
        }

        private int CalcSidepotSize(IEnumerable<Player> players, IEnumerable<Dictionary<string, object>> smallerSidePots, int allinAmount)
        {
            Func<int, Player, int> addChipForPot = (pot, player) => pot + Math.Min(allinAmount, player.PayInfo.Amount);
            var targetPotSize = players.Aggregate(0, addChipForPot);
            return targetPotSize - this.GetSidepotsSum(smallerSidePots);
        }

        private int GetSidepotsSum(IEnumerable<Dictionary<string, object>> sidepots)
        {
            return sidepots.Aggregate(0, (sum_, sidepot) => sum_ + (int)sidepot["amount"]);
        }

        private List<Player> SelectEligibles(IEnumerable<Player> players, int allinAmount)
        {
            return (from player in players
                    where this.IsEligible(player, allinAmount)
                    select player).ToList();
        }

        private bool IsEligible(Player player, int allinAmount)
        {
            return player.PayInfo.Amount >= allinAmount && player.PayInfo.Status != PayInfo.FOLDED;
        }

        private List<PayInfo> FetchAllInPayInfo(IEnumerable<Player> players)
        {
            var payinfo = this.GetPayInfo(players);
            var allinInfo = (from info in payinfo
                              where info.Status == PayInfo.ALLIN
                              select info).ToList();
            return allinInfo.OrderBy(info => info.Amount).ToList();
        }

        private List<PayInfo> GetPayInfo(IEnumerable<Player> players)
            => players.Select(p => p.PayInfo).ToList();
    }
}
