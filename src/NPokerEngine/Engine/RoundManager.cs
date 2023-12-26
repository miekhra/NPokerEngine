using NPokerEngine.Messages;
using NPokerEngine.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NPokerEngine.Engine
{
    internal class RoundManager : Singleton<RoundManager>
    {
        public (GameState roundState, List<IMessage> messages) StartNewRound(int round_count, float small_blind_amount, float ante_amount, Table table)
        {
            var _state = this.GenerateInitialState(round_count, small_blind_amount, table);
            var state = this.DeepCopyState(_state);
            table = state.Table;
            table.Deck.Shuffle();
            this.CorrectAnte(ante_amount, table.Seats.Players);
            this.CorrectBlind(small_blind_amount, table);
            this.DealHolecard(table.Deck, table.Seats.Players);
            var startMessages = this.RoundStartMessage(round_count, table);
            var (startState, street_msgs) = this.StartStreet(state);
            state = startState;
            startMessages.AddRange(street_msgs);

            return (state, startMessages);
        }

        public Tuple<GameState, List<IMessage>> ApplyAction(GameState original_state, ActionType action, float bet_amount)
        {
            ActionHistoryEntry appliedActionHistoryEntry;
            return ApplyAction(original_state, action, bet_amount, out appliedActionHistoryEntry);
        }

        public Tuple<GameState, List<IMessage>> ApplyAction(GameState original_state, ActionType action, float bet_amount, out ActionHistoryEntry appliedActionHistoryEntry)
        {

            var state = this.DeepCopyState(original_state);
            state = this.UpdateStateByAction(state, action, bet_amount, out appliedActionHistoryEntry);
            var table = state.Table;
            var update_msg = this.UpdateMessage(state, action, bet_amount);
            if (this.IsEveryoneAgreed(state))
            {
                table.Seats.Players.ForEach(p => p.SaveStreetActionHistories(state.Street));
                state.Street = (StreetType)(Convert.ToByte(state.Street) + 1);
                var (updatedState, startStreetMessages) = this.StartStreet(state);
                state = updatedState;
                var messagesList = new List<IMessage>() { update_msg };
                messagesList.AddRange(startStreetMessages);

                return Tuple.Create(state, messagesList);
            }
            else
            {
                state.NextPlayerIx = table.NextAskWaitingPlayerPosition(state.NextPlayerIx);
                var next_player_pos = state.NextPlayerIx;
                var next_player = table.Seats.Players[next_player_pos];
                var ask_message = MessageBuilder.Instance.BuildAskMessage(next_player_pos, state);
                var messagesList = new List<IMessage>() { update_msg, ask_message };
                return Tuple.Create(state, messagesList);
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
            var isHeadsUp = table.Seats.ActivePlayersCount() == 2;
            if (isHeadsUp)
            {
                (table._sbPosition, table._bbPosition) = (table._bbPosition, table._sbPosition);
            }
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

        private (GameState state, List<IMessage> messages) StartStreet(GameState state)
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

        private (GameState state, List<IMessage> messages) PreFlop(GameState state)
        {
            foreach (var i in Enumerable.Range(0, 2))
            {
                state.NextPlayerIx = state.Table.NextAskWaitingPlayerPosition(state.NextPlayerIx);
            }
            return this.ForwardStreet(state);
        }

        private (GameState state, List<IMessage> messages) Flop(GameState state)
        {
            foreach (var card in state.Table.Deck.DrawCards(3))
            {
                state.Table.AddCommunityCard(card);
            }
            // BB goes first in Heads-Up
            if (state.Table.DealerButton == state.Table.SmallBlindPosition.Value)
            {
                if (state.NextPlayerIx != -1)
                {
                    state.NextPlayerIx = state.Table.NextAskWaitingPlayerPosition(state.NextPlayerIx);
                }
            }
            return this.ForwardStreet(state);
        }

        private (GameState state, List<IMessage> messages) Turn(GameState state)
        {
            state.Table.AddCommunityCard(state.Table.Deck.DrawCard());
            // BB goes first in Heads - Up
            if (state.Table.DealerButton == state.Table.SmallBlindPosition.Value)
            {
                if (state.NextPlayerIx != -1)
                {
                    state.NextPlayerIx = state.Table.NextAskWaitingPlayerPosition(state.NextPlayerIx);
                }
            }
            return this.ForwardStreet(state);
        }

        private (GameState state, List<IMessage> messages) River(GameState state)
        {
            state.Table.AddCommunityCard(state.Table.Deck.DrawCard());
            // BB goes first in Heads - Up
            if (state.Table.DealerButton == state.Table.SmallBlindPosition.Value)
            {
                if (state.NextPlayerIx != -1)
                {
                    state.NextPlayerIx = state.Table.NextAskWaitingPlayerPosition(state.NextPlayerIx);
                }
            }
            return this.ForwardStreet(state);
        }

        private (GameState state, List<IMessage> messages) Showdown(GameState state)
        {
            var (winners, hand_info, prize_map) = GameEvaluator.Instance.Judge((Table)state.Table);
            this.PrizeToWinners(state.Table.Seats.Players, prize_map);
            var result_message = MessageBuilder.Instance.BuildRoundResultMessage(state.RoundCount, winners, hand_info, (GameState)state.Clone(), prize_map);
            state.Table.Reset();
            state.Street = (StreetType)(Convert.ToByte(state.Street) + 1);
            return (state, new List<IMessage>() { result_message });
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

        private List<IMessage> RoundStartMessage(int round_count, Table table)
            => table.Seats.Players
                .Select((player, ix) => MessageBuilder.Instance.BuildRoundStartMessage(round_count, ix, table.Seats))
                .Cast<IMessage>()
                .ToList();

        private (GameState state, List<IMessage> messages) ForwardStreet(GameState state)
        {
            var street_start_msg = MessageBuilder.Instance.BuildStreetStartMessage(state);
            var messagesList = new List<IMessage>();
            if (state.Table.Seats.ActivePlayersCount() == 1)
            {
                street_start_msg = null;
            }
            if (street_start_msg != null)
                messagesList.Add(street_start_msg);
            if (state.Table.Seats.AskWaitPlayersCount() <= 1)
            {
                state.Street = (StreetType)(Convert.ToByte(state.Street) + 1);
                var _tup_1 = this.StartStreet(state);
                state = _tup_1.Item1;
                var messages = _tup_1.Item2;
                messagesList.AddRange(messages);
                return (state, messagesList);
            }
            else
            {
                var next_player_pos = state.NextPlayerIx;
                var next_player = state.Table.Seats.Players[next_player_pos];
                messagesList.Add(MessageBuilder.Instance.BuildAskMessage(next_player_pos, state));
                return (state, messagesList);
            }
        }

        private GameState UpdateStateByAction(GameState state, ActionType action, float bet_amount, out ActionHistoryEntry actionHistoryEntry)
        {
            (action, bet_amount) = ActionChecker.Instance.CorrectAction(state.Table.Seats.Players, state.NextPlayerIx, state.SmallBlindAmount, action, bet_amount);
            var next_player = state.Table.Seats.Players[state.NextPlayerIx];
            if (ActionChecker.Instance.IsAllin(next_player, action, bet_amount))
            {
                next_player.PayInfo.UpdateToAllin();
            }
            return this.AcceptAction(state, action, bet_amount, out actionHistoryEntry);
        }

        private GameState AcceptAction(GameState state, ActionType action, float bet_amount, out ActionHistoryEntry actionHistoryEntry)
        {
            var player = state.Table.Seats.Players[state.NextPlayerIx];
            if (action == ActionType.CALL)
            {
                this.ChipTransaction(player, bet_amount);
                actionHistoryEntry = player.AddActionHistory(ActionType.CALL, bet_amount);
            }
            else if (action == ActionType.RAISE)
            {
                this.ChipTransaction(player, bet_amount);
                var add_amount = bet_amount - ActionChecker.Instance.AgreeAmount(state.Table.Seats.Players);
                actionHistoryEntry = player.AddActionHistory(ActionType.RAISE, bet_amount, add_amount);
            }
            else if (action == ActionType.FOLD)
            {
                actionHistoryEntry = player.AddActionHistory(ActionType.FOLD);
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

        private GameUpdateMessage UpdateMessage(GameState state, ActionType action, float bet_amount)
        {
            return MessageBuilder.Instance.BuildGameUpdateMessage(state.NextPlayerIx, action, bet_amount, state);
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
                throw new Exception("[__is_everyone_agreed] no-active-players!!");
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
        }
    }
}
