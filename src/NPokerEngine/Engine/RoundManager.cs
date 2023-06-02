﻿using NPokerEngine.Types;
using NPokerEngine.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace NPokerEngine.Engine
{
    public class RoundManager
    {

        private static RoundManager _instance;
        private IMessageBuilder _messageBuilder;

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

        internal void SetMessageBuilder(IMessageBuilder messageBuilder)
            => _messageBuilder = messageBuilder;

        public (GameState roundState, List<(string, object)> messages) StartNewRound(int round_count, float small_blind_amount, float ante_amount, Table table)
        {
            var _state = this.GenerateInitialState(round_count, small_blind_amount, table);
            var state = this.DeepCopyState(_state);
            table = state.Table;
            table.Deck.Shuffle();
            this.CorrectAnte(ante_amount, table.Seats.Players);
            this.CorrectBlind(small_blind_amount, table);
            this.DealHolecard(table.Deck, table.Seats.Players);
            var start_msg = this.RoundStartMessage(round_count, table).ToDictionary(k => k.Key, v =>(object)v.Value);
            var _tup_1 = this.StartStreet(state);
            state = _tup_1.Item1;
            var street_msgs = _tup_1.Item2;

            var messages = new List<(string, object)>();
            foreach (var msg in start_msg)
            {
                if (msg.Value is string str)
                    messages.Add((msg.Key, msg.Value));
                if (msg.Value is Dictionary<string, object> dict && dict.Count > 0)
                    messages.Add((msg.Key, dict.ElementAt(0).Value));
            }
                
            foreach (var msg in (Dictionary<string, object>)street_msgs)
            {
                if (msg.Value is string str)
                    messages.Add((msg.Key, msg.Value));
                if (msg.Value is Dictionary<string, object> dict && dict.Count > 0)
                    messages.Add((msg.Key, dict.ElementAt(0).Value));
            }

            return (state, messages);
        }

        public Tuple<GameState, object> ApplyAction(GameState original_state, string action, int bet_amount)
        {

            var state = this.DeepCopyState(original_state);
            state = this.UpdateStateByAction(state, action, bet_amount);
            var table = state.Table;
            var update_msg = this.UpdateMessage(state, action, bet_amount);
            if (_messageBuilder != null && update_msg is ValueTuple<int, Dictionary<string, object>> tup && tup.Item2 != null)
                update_msg = new Tuple<string, object>(tup.Item1.ToString(), tup.Item2.ElementAt(0).Value);
            if (this.IsEveryoneAgreed(state))
            {
                table.Seats.Players.ForEach(p => p.SaveStreetActionHistories(state.Street));
                state.Street = (StreetType)(Convert.ToByte(state.Street) + 1);
                var _tup_1 = this.StartStreet(state);
                state = _tup_1.Item1;
                var street_msgs = _tup_1.Item2;
                if (_messageBuilder != null)
                {
                    var resultMessages = new List<Tuple<string, object>>();
                    if (update_msg is Tuple<string, object> updateTuple)
                        resultMessages.Add(updateTuple);
                    foreach (var item in (Dictionary<string, object>)street_msgs)
                    {
                        resultMessages.Add(new Tuple<string, object>(item.Key, item.Value));
                    }
                    return Tuple.Create(state, (object)(resultMessages));
                }

                var messagesList = new List<object>()
                {
                    update_msg
                };

                foreach (var item in (IEnumerable)street_msgs)
                {
                    messagesList.Add(item);
                }

                return Tuple.Create(state, (object)(messagesList));

                //return Tuple.Create(state, (object)(new List<object> {
                //        update_msg
                //    } + street_msgs.ToString()));
            }
            else
            {
                state.NextPlayerIx = table.NextAskWaitingPlayerPosition(state.NextPlayerIx);
                var next_player_pos = state.NextPlayerIx;
                var next_player = table.Seats.Players[next_player_pos];
                var ask_message = (next_player.Uuid, (_messageBuilder ?? MessageBuilder.Instance).BuildAskMessage(next_player_pos, state.ToDictionary()));
                if (_messageBuilder != null)
                {
                    Tuple<string, object> askMessage2 = default;
                    if (ask_message is ValueTuple<string, Dictionary<string, object>> tup1)
                        askMessage2 = new Tuple<string, object>(tup1.Item1.ToString(), tup1.Item2.ElementAt(0).Value);
                    if (update_msg is Tuple<string, object> t2 && askMessage2 != default)
                        return Tuple.Create(state, (object)new List<Tuple<string, object>>() { t2, askMessage2 });
                    return Tuple.Create(state, (object)(new List<object> {
                        update_msg,
                        ask_message
                    }));
                }
                var messagesList = new List<object>()
                {
                    update_msg
                };
                messagesList.Add(ask_message);
                return Tuple.Create(state, (object)(messagesList));
            }
        }

        private void CorrectAnte(float ante_amount, IEnumerable<Player> players)
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

        private void CorrectBlind(float sb_amount, Table table)
        {
            this.BlindTransaction(table.Seats.Players[table.SmallBlindPosition.Value], true, sb_amount);
            this.BlindTransaction(table.Seats.Players[table.BigBlindPosition.Value], false, sb_amount);
        }

        private void BlindTransaction(Player player, bool small_blind, float sb_amount)
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
                player.AddHoleCards(deck.DrawCards(2).ToArray());
            }
        }

        private Tuple<GameState, object> StartStreet(GameState state)
        {
            var next_player_pos = state.Table.NextAskWaitingPlayerPosition(state.Table.SmallBlindPosition.Value - 1);
            state.NextPlayerIx = next_player_pos;
            switch (state.Street)
            {
                case StreetType.PREFLOP:
                    return this.PreFlop(state);
                case StreetType.FLOP:
                    return this.Flop(state);
                case StreetType.TURN:
                    return this.Turn(state);
                case StreetType.RIVER:
                    return this.River(state);
                case StreetType.SHOWDOWN:
                    return this.Showdown(state);
                default:
                    throw new ArgumentException(String.Format("Street is already finished [street = %d]", state.Street));
            };
        }

        private Tuple<GameState, object> PreFlop(GameState state)
        {
            foreach (var i in Enumerable.Range(0, 2))
            {
                state.NextPlayerIx = state.Table.NextAskWaitingPlayerPosition(state.NextPlayerIx);
            }
            return this.ForwardStreet(state);
        }

        private Tuple<GameState, object> Flop(GameState state)
        {
            foreach (var card in state.Table.Deck.DrawCards(3))
            {
                state.Table.AddCommunityCard(card);
            }
            // BB goes first in Heads-Up
            if (state.Table.Seats.Players.Count == 2)
            {
                if (state.NextPlayerIx != -1)
                {
                    state.NextPlayerIx = state.Table.NextAskWaitingPlayerPosition(state.NextPlayerIx);
                }
            }
            return this.ForwardStreet(state);
        }

        private Tuple<GameState, object> Turn(GameState state)
        {
            state.Table.AddCommunityCard(state.Table.Deck.DrawCard());
            // BB goes first in Heads-Up
            if (state.Table.Seats.Players.Count == 2)
            {
                if (state.NextPlayerIx != -1)
                {
                    state.NextPlayerIx = state.Table.NextAskWaitingPlayerPosition(state.NextPlayerIx);
                }
            }
            return this.ForwardStreet(state);
        }

        private Tuple<GameState, object> River(GameState state)
        {
            state.Table.AddCommunityCard(state.Table.Deck.DrawCard());
            // BB goes first in Heads-Up
            if (state.Table.Seats.Players.Count == 2)
            {
                if (state.NextPlayerIx != -1)
                {
                    state.NextPlayerIx = state.Table.NextAskWaitingPlayerPosition(state.NextPlayerIx);
                }
            }
            return this.ForwardStreet(state);
        }

        private Tuple<GameState, object> Showdown(GameState state)
        {
            var _tup_1 = GameEvaluator.Instance.Judge((Table)state.Table);
            var winners = _tup_1.Item1;
            var hand_info = _tup_1.Item2;
            var prize_map = _tup_1.Item3;
            this.PrizeToWinners(state.Table.Seats.Players, prize_map);
            var result_message = (_messageBuilder ?? MessageBuilder.Instance).BuildRoundResultMessage(state.RoundCount, winners, hand_info, state.ToDictionary());
            state.Table.Reset();
            state.Street = (StreetType)(Convert.ToByte(state.Street) + 1);
            return Tuple.Create(state, (object)result_message);
        }

        private void PrizeToWinners(List<Player> players, Dictionary<int, float> prize_map)
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
            Func<int, (string, Dictionary<string, object>)> gen_msg = idx => (players[idx].Uuid, (_messageBuilder ?? MessageBuilder.Instance).BuildRoundStartMessage(round_count, idx, table.Seats));
            var aggregation = new Dictionary<string, Dictionary<string, object>>();
            for (var ix=0; ix < players.Count; ix++)
            {
                var tuple = gen_msg(ix);
                aggregation[tuple.Item1] = tuple.Item2;
            }

            return aggregation;
        }

        private Tuple<GameState, object> ForwardStreet(GameState state)
        {
            var street_start_msg = (_messageBuilder ?? MessageBuilder.Instance).BuildStreetStartMessage(state.ToDictionary());
            if (state.Table.Seats.ActivePlayersCount() == 1)
            {
                street_start_msg = new Dictionary<string, object>();
            }
            if (state.Table.Seats.AskWaitPlayersCount() <= 1)
            {
                state.Street = (StreetType)(Convert.ToByte(state.Street) + 1);
                var _tup_1 = this.StartStreet(state);
                state = _tup_1.Item1;
                var messages = _tup_1.Item2;
                if (_messageBuilder != null)
                {
                    var resultMessages = new Dictionary<string, object>();
                    foreach (var item in street_start_msg)
                    {
                        resultMessages[item.Key] = item.Value;
                    }
                    foreach (var item in (Dictionary<string, object>)messages)
                    {
                        resultMessages[item.Key] = item.Value;
                    }
                    return Tuple.Create(state, (object)resultMessages);
                }
                var messagesList = new List<object>() { street_start_msg };
                foreach (var item in (IEnumerable)messages)
                {
                    messagesList.Add(item);
                }
                return Tuple.Create(state, (object)messagesList);
                //return Tuple.Create(state, (object)(street_start_msg.ToString() + messages.ToString()));
            }
            else
            {
                var next_player_pos = state.NextPlayerIx;
                var next_player = state.Table.Seats.Players[next_player_pos];
                var ask_message = new List<object> {
                        (next_player.Uuid, (_messageBuilder ?? MessageBuilder.Instance).BuildAskMessage(next_player_pos,state.ToDictionary()))
                    };
                if (_messageBuilder != null)
                {
                    street_start_msg[next_player.Uuid] = (_messageBuilder ?? MessageBuilder.Instance).BuildAskMessage(next_player_pos, state.ToDictionary()).Values.First();
                    return Tuple.Create(state, (object)street_start_msg);
                }
                else
                {
                    var messageDict = new Dictionary<string, object>();
                    messageDict.Add("-1", MessageBuilder.Instance.BuildStreetStartMessage(state.ToDictionary()));
                    messageDict.Add(((ValueTuple<string, Dictionary<string,object>>)ask_message[0]).Item1, ((ValueTuple<string, Dictionary<string, object>>)ask_message[0]).Item2);

                    return Tuple.Create(state, (object)messageDict);
                    //return Tuple.Create(state, (object)new List<object> { new Tuple<string, object>("-1", MessageBuilder.Instance.BuildStreetStartMessage(state)), ask_message });
                }
                //return Tuple.Create(state, (object)DictionaryUtils.Update(street_start_msg, new Dictionary<string, object> { { "uuid", next_player.Uuid } }, (_messageBuilder ?? MessageBuilder.Instance).BuildAskMessage(next_player_pos, state)).ToString());
            }
        }

        private GameState UpdateStateByAction(GameState state, string action, int bet_amount)
        {
            var _tup_1 = ActionChecker.Instance.CorrectAction(state.Table.Seats.Players, state.NextPlayerIx, state.SmallBlindAmount, action, bet_amount);
            action = _tup_1.Item1;
            bet_amount = (int)_tup_1.Item2;
            var next_player = state.Table.Seats.Players[state.NextPlayerIx];
            if (ActionChecker.Instance.IsAllin(next_player, action, bet_amount))
            {
                next_player.PayInfo.UpdateToAllin();
            }
            return this.AcceptAction(state, action, bet_amount);
        }

        private GameState AcceptAction(GameState state, string action, int bet_amount)
        {
            var player = state.Table.Seats.Players[state.NextPlayerIx];
            if (action == "call")
            {
                this.ChipTransaction(player, bet_amount);
                player.AddActionHistory(ActionType.CALL, bet_amount);
            }
            else if (action == "raise")
            {
                this.ChipTransaction(player, bet_amount);
                var add_amount = bet_amount - ActionChecker.Instance.AgreeAmount(state.Table.Seats.Players);
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

        private void ChipTransaction(Player player, float bet_amount)
        {
            var need_amount = ActionChecker.Instance.NeedAmountForAction(player, bet_amount);
            player.CollectBet(need_amount);
            player.PayInfo.UpdateByPay(need_amount);
        }

        private object UpdateMessage(GameState state, object action, object bet_amount)
        {
            return (-1, (_messageBuilder ?? MessageBuilder.Instance).BuildGameUpdateMessage(state.NextPlayerIx, action, bet_amount, state.ToDictionary()));
        }

        private bool IsEveryoneAgreed(GameState state)
        {
            this.AgreeLogicBugCatch(state);
            var players = state.Table.Seats.Players;
            Player nextPlayer = null;
            try
            {
                var next_player_pos = state.Table.NextAskWaitingPlayerPosition(state.NextPlayerIx);
                nextPlayer = players[next_player_pos];
            }
            catch
            {
            }

            var max_pay = (from p in players
                               select p.PaidSum()).Max();
            var everyone_agreed = players.Count == (from p in players
                                                    where this.IsAgreed((int)max_pay, p)
                                                    select p).ToList().Count;
            var lonely_player = state.Table.Seats.ActivePlayersCount() == 1;
            var no_need_to_ask = state.Table.Seats.AskWaitPlayersCount() == 1 && nextPlayer != null && nextPlayer.IsWaitingAsk() && nextPlayer.PaidSum() == max_pay;
            return everyone_agreed || lonely_player || no_need_to_ask;
        }

        private void AgreeLogicBugCatch(GameState state)
        {
            if (state.Table.Seats.ActivePlayersCount() == 0)
            {
                throw new Exception ("[__is_everyone_agreed] no-active-players!!");
            }
        }

        private bool IsAgreed(int maxPay, Player player)
        {
            // BigBlind should be asked action at least once
            var is_preflop = !player.RoundActionHistories.Any() || (player.RoundActionHistories.Any() && player.RoundActionHistories[0] == null);
            var bb_ask_once = player.ActionHistories.Any() && player.ActionHistories.Count == 1 && player.ActionHistories[0].ActionType == ActionType.BIG_BLIND; // Player.ACTION_BIG_BLIND;
            var bb_ask_check = !is_preflop || !bb_ask_once;
            return bb_ask_check && player.PaidSum() == maxPay && player.ActionHistories.Count != 0 || new List<object> {
                    PayInfoStatus.FOLDED,
                    PayInfoStatus.ALLIN
                }.Contains(player.PayInfo.Status);
        }

        internal GameState GenerateInitialState(int roundCount, float smallBlindAmount, Table table)
        {
            return new GameState
            {
                RoundCount = roundCount,
                SmallBlindAmount = smallBlindAmount,
                Street = StreetType.PREFLOP,
                NextPlayerIx = table.NextAskWaitingPlayerPosition(table.BigBlindPosition.Value),
                Table = table
            };
        }

        internal GameState DeepCopyState(GameState state)
        {
            return (GameState)state.Clone();
            //var tableDeepcopy = ObjectUtils.DeepCopyByReflection(state["table"]);
            //return new Dictionary<string, object> {
            //        {
            //            "round_count",
            //            state["round_count"]},
            //        {
            //            "small_blind_amount",
            //            state["small_blind_amount"]},
            //        {
            //            "street",
            //            state["street"]},
            //        {
            //            "next_player",
            //            state["next_player"]},
            //        {
            //            "table",
            //            tableDeepcopy}};
        }
    }
}
