using NPokerEngine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace NPokerEngine.Engine
{
    public class RoundManager
    {

        private static RoundManager _instance;

        private RoundManager()
        {
        }

        public static RoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RoundManager();
                }

                return _instance;
            }
        }

        public object StartNewRound(int round_count, int small_blind_amount, int ante_amount, Table table)
        {
            var _state = this.GenerateenInitialState(round_count, small_blind_amount, table);
            var state = this.DeepCopyState(_state);
            table = (Table)state["table"];
            table.Deck.Shuffle();
            this.CorrectAnte(ante_amount, table.Seats.Players);
            this.CorrectBlind(small_blind_amount, table);
            this.DealHolecard(table.Deck, table.Seats.Players);
            var start_msg = this.RoundStartMessage(round_count, table);
            var _tup_1 = this.StartStreet(state);
            state = _tup_1.Item1;
            var street_msgs = _tup_1.Item2;
            return Tuple.Create(state, start_msg + street_msgs);
        }

        public Tuple<Dictionary<string, object>, object> ApplyAction(Dictionary<string, object> original_state, string action, int bet_amount)
        {

            var state = this.DeepCopyState(original_state);
            state = this.UpdateStateByAction(state, action, bet_amount);
            var table = (Table)state["table"];
            var update_msg = this.UpdateMessage(state, action, bet_amount);
            if (this.IsEveryoneAgreed(state))
            {
                table.Seats.Players.ForEach(p => p.SaveStreetActionHistories((StreetType)state["street"]));
                state["street"] = (int)state["street"] + 1;
                var _tup_1 = this.StartStreet(state);
                state = _tup_1.Item1;
                var street_msgs = _tup_1.Item2;
                return Tuple.Create(state, (object)(new List<object> {
                        update_msg
                    } + street_msgs));
            }
            else
            {
                state["next_player"] = table.NextAskWaitingPlayerPosition((int)state["next_player"]);
                var next_player_pos = (int)state["next_player"];
                var next_player = table.Seats.Players[next_player_pos];
                var ask_message = (next_player.Uuid, MessageBuilder.Instance.BuildAskMessage(next_player_pos, state));
                return Tuple.Create(state, (object)(new List<object> {
                        update_msg,
                        ask_message
                    }));
            }
        }

        private void CorrectAnte(int ante_amount, IEnumerable<Player> players)
        {
            if (ante_amount == 0)
            {
                return;
            }
            var active_players = (from player in players
                                  where player.IsActive()
                                  select player).ToList();
            foreach (var player in active_players)
            {
                player.CollectBet(ante_amount);
                player.PayInfo.UpdateByPay(ante_amount);
                player.AddActionHistory(ActionType.ANTE, ante_amount);
            }
        }

        private void CorrectBlind(int sb_amount, Table table)
        {
            this.BlindTransaction(table.Seats.Players[table.SmallBlindPosition], true, sb_amount);
            this.BlindTransaction(table.Seats.Players[table.BigBlindPosition], false, sb_amount);
        }

        private void BlindTransaction(Player player, bool small_blind, int sb_amount)
        {
            var action = small_blind ? ActionType.SMALL_BLIND : ActionType.BIG_BLIND;
            var blind_amount = small_blind ? sb_amount : sb_amount * 2;
            player.CollectBet(blind_amount);
            player.AddActionHistory(action, sbAmount: sb_amount);
            player.PayInfo.UpdateByPay(blind_amount);
        }

        private void DealHolecard(Deck deck, IEnumerable<Player> players)
        {
            foreach (var player in players)
            {
                player.AddHoleCards(deck.DrawCards(2));
            }
        }

        private Tuple<Dictionary<string, object>, string> StartStreet(Dictionary<string, object> state)
        {
            var next_player_pos = ((Table)state["table"]).NextAskWaitingPlayerPosition(((Table)state["table"]).SmallBlindPosition - 1);
            state["next_player"] = next_player_pos;
            var street = (int)state["street"];
            if (street == (int)StreetType.PREFLOP)
            {
                return this.PreFlop(state);
            }
            else if (street == (int)StreetType.FLOP)
            {
                return this.Flop(state);
            }
            else if (street == (int)StreetType.TURN)
            {
                return this.Turn(state);
            }
            else if (street == (int)StreetType.RIVER)
            {
                return this.River(state);
            }
            else if (street == (int)StreetType.SHOWDOWN)
            {
                return this.Showdown(state);
            }
            else
            {
                throw new ArgumentException(String.Format("Street is already finished [street = %d]", street));
            }
        }

        private Tuple<Dictionary<string, object>, string> PreFlop(Dictionary<string, object> state)
        {
            foreach (var i in Enumerable.Range(0, 2))
            {
                state["next_player"] = ((Table)state["table"]).NextAskWaitingPlayerPosition((int)state["next_player"]);
            }
            return this.ForwardStreet(state);
        }

        private Tuple<Dictionary<string, object>, string> Flop(Dictionary<string, object> state)
        {
            var table = (Table)state["table"];
            foreach (var card in table.Deck.DrawCards(3))
            {
                table.AddCommunityCard(card);
            }
            // BB goes first in Heads-Up
            if (table.Seats.Players.Count == 2)
            {
                if (state["next_player"] is int)
                {
                    state["next_player"] = table.NextAskWaitingPlayerPosition((int)state["next_player"]);
                }
            }
            return this.ForwardStreet(state);
        }

        private Tuple<Dictionary<string, object>, string> Turn(Dictionary<string, object> state)
        {
            var table = (Table)state["table"];
            table.AddCommunityCard(table.Deck.DrawCard());
            // BB goes first in Heads-Up
            if (table.Seats.Players.Count == 2)
            {
                if (state["next_player"] is int)
                {
                    state["next_player"] = table.NextAskWaitingPlayerPosition((int)state["next_player"]);
                }
            }
            return this.ForwardStreet(state);
        }

        private Tuple<Dictionary<string, object>, string> River(Dictionary<string, object> state)
        {
            var table = (Table)state["table"];
            table.AddCommunityCard(table.Deck.DrawCard());
            // BB goes first in Heads-Up
            if (table.Seats.Players.Count == 2)
            {
                if (state["next_player"] is int)
                {
                    state["next_player"] = table.NextAskWaitingPlayerPosition((int)state["next_player"]);
                }
            }
            return this.ForwardStreet(state);
        }

        private Tuple<Dictionary<string, object>, string> Showdown(Dictionary<string, object> state)
        {
            var _tup_1 = GameEvaluator.Instance.Judge((Table)state["table"]);
            var winners = _tup_1.Item1;
            var hand_info = _tup_1.Item2;
            var prize_map = _tup_1.Item3;
            this.PrizeToWinners(((Table)state["table"]).Seats.Players, prize_map);
            var result_message = MessageBuilder.Instance.BuildRoundResultMessage(state["round_count"], winners, hand_info, state);
            ((Table)state["table"]).Reset();
            state["street"] = (int)state["street"] + 1;
            return Tuple.Create(state, result_message.ToString());
        }

        private void PrizeToWinners(List<Player> players, Dictionary<int, int> prize_map)
        {
            foreach (var _tup_1 in prize_map)
            {
                var idx = _tup_1.Key;
                var prize = _tup_1.Value;
                players[idx].AppendChip(prize);
            }
        }

        private Dictionary<string, Dictionary<string, object>> RoundStartMessage(int round_count, Table table)
        {
            var players = table.Seats.Players;
            Func<int, (string, Dictionary<string, object>)> gen_msg = idx => (players[idx].Uuid, MessageBuilder.Instance.BuildRoundStartMessage(round_count, idx, table.Seats));
            var aggregation = new Dictionary<string, Dictionary<string, object>>();
            for (var ix=0; ix < players.Count; ix++)
            {
                var tuple = gen_msg(ix);
                aggregation[tuple.Item1] = tuple.Item2;
            }

            return aggregation;
        }

        private Tuple<Dictionary<string, object>, string> ForwardStreet(Dictionary<string, object> state)
        {
            var table = (Table)state["table"];
            var street_start_msg = MessageBuilder.Instance.BuildStreetStartMessage(state);
            if (table.Seats.ActivePlayersCount() == 1)
            {
                street_start_msg = new Dictionary<string, object>();
            }
            if (table.Seats.AskWaitPlayersCount() <= 1)
            {
                state["street"] = (int)state["street"] + 1;
                var _tup_1 = this.StartStreet(state);
                state = _tup_1.Item1;
                var messages = _tup_1.Item2;
                return Tuple.Create(state, street_start_msg + messages);
            }
            else
            {
                var next_player_pos = (int)state["next_player"];
                var next_player = table.Seats.Players[next_player_pos];
                var ask_message = new List<object> {
                        (next_player.Uuid, MessageBuilder.Instance.BuildAskMessage(next_player_pos,state))
                    };
                return Tuple.Create(state, DictionaryUtils.Update(street_start_msg, new Dictionary<string, object> { { "uuid", next_player.Uuid } }, MessageBuilder.Instance.BuildAskMessage(next_player_pos, state)).ToString());
            }
        }

        private Dictionary<string, object> UpdateStateByAction(Dictionary<string, object> state, string action, int bet_amount)
        {
            var table = (Table)state["table"];
            var _tup_1 = ActionChecker.Instance.CorrectAction(table.Seats.Players, (int)state["next_player"], (int)state["small_blind_amount"], action, bet_amount);
            action = _tup_1.Item1;
            bet_amount = _tup_1.Item2;
            var next_player = table.Seats.Players[(int)state["next_player"]];
            if (ActionChecker.Instance.IsAllin(next_player, action, bet_amount))
            {
                next_player.PayInfo.UpdateToAllin();
            }
            return this.AcceptAction(state, action, bet_amount);
        }

        private Dictionary<string, object> AcceptAction(Dictionary<string, object> state, string action, int bet_amount)
        {
            var player = ((Table)state["table"]).Seats.Players[(int)state["next_player"]];
            if (action == "call")
            {
                this.ChipTransaction(player, bet_amount);
                player.AddActionHistory(ActionType.CALL, bet_amount);
            }
            else if (action == "raise")
            {
                this.ChipTransaction(player, bet_amount);
                var add_amount = bet_amount - ActionChecker.Instance.AgreeAmount(((Table)state["table"]).Seats.Players);
                player.AddActionHistory(ActionType.RAISE, bet_amount, add_amount);
            }
            else if (action == "fold")
            {
                player.AddActionHistory(ActionType.FOLD);
                player.PayInfo.UpdateToFold();
            }
            else
            {
                throw new ArgumentException(String.Format("Unexpected action %s received", action));
            }
            return state;
        }

        private void ChipTransaction(Player player, int bet_amount)
        {
            var need_amount = ActionChecker.Instance.NeedAmountForAction(player, bet_amount);
            player.CollectBet(need_amount);
            player.PayInfo.UpdateByPay(need_amount);
        }

        private object UpdateMessage(Dictionary<string, object> state, object action, object bet_amount)
        {
            return (-1, MessageBuilder.Instance.BuildGameUpdateMessage((int)state["next_player"], action, bet_amount, state));
        }

        private bool IsEveryoneAgreed(Dictionary<string, object> state)
        {
            this.AgreeLogicBugCatch(state);
            var players = ((Table)state["table"]).Seats.Players;
            Player nextPlayer = null;
            try
            {
                var next_player_pos = ((Table)state["table"]).NextAskWaitingPlayerPosition((int)state["next_player"]);
                nextPlayer = players[next_player_pos];
            }
            catch
            {
            }

            var max_pay = (from p in players
                               select p.PaidSum()).Max();
            var everyone_agreed = players.Count == (from p in players
                                                    where this.IsAgreed(max_pay, p)
                                                    select p).ToList().Count;
            var lonely_player = ((Table)state["table"]).Seats.ActivePlayersCount() == 1;
            var no_need_to_ask = ((Table)state["table"]).Seats.AskWaitPlayersCount() == 1 && nextPlayer != null && nextPlayer.IsWaitingAsk() && nextPlayer.PaidSum() == max_pay;
            return everyone_agreed || lonely_player || no_need_to_ask;
        }

        private void AgreeLogicBugCatch(Dictionary<string, object> state)
        {
            if (((Table)state["table"]).Seats.ActivePlayersCount() == 0)
            {
                throw new Exception ("[__is_everyone_agreed] no-active-players!!");
            }
        }

        private bool IsAgreed(int maxPay, Player player)
        {
            // BigBlind should be asked action at least once
            var is_preflop = player.RoundActionHistories.Any() && player.RoundActionHistories[0] == null;
            var bb_ask_once = player.ActionHistories.Any() && player.ActionHistories.Count == 1 && player.ActionHistories[0]["action"] == Player.ACTION_BIG_BLIND;
            var bb_ask_check = !is_preflop || !bb_ask_once;
            return bb_ask_check && player.PaidSum() == maxPay && player.ActionHistories.Count != 0 || new List<object> {
                    PayInfo.FOLDED,
                    PayInfo.ALLIN
                }.Contains(player.PayInfo.Status);
        }

        private Dictionary<string, object> GenerateenInitialState(int roundCount, int smallBlindAmount, Table table)
        {
            return new Dictionary<string, object> {
                    {
                        "round_count",
                        roundCount},
                    {
                        "small_blind_amount",
                        smallBlindAmount},
                {
                        "street",
                        StreetType.PREFLOP},
                    {
                        "next_player",
                        table.NextAskWaitingPlayerPosition(table.BigBlindPosition)
                },
                    {
                        "table",
                        table}
            };
        }

        private Dictionary<string, object> DeepCopyState(Dictionary<string, object> state)
        {
            var tableDeepcopy = ObjectUtils.DeepCopyByReflection(state["table"]);
            return new Dictionary<string, object> {
                    {
                        "round_count",
                        state["round_count"]},
                    {
                        "small_blind_amount",
                        state["small_blind_amount"]},
                    {
                        "street",
                        state["street"]},
                    {
                        "next_player",
                        state["next_player"]},
                    {
                        "table",
                        tableDeepcopy}};
        }
    }
}
