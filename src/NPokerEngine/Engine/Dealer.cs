using NPokerEngine.Messages;
using NPokerEngine.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NPokerEngine.Engine
{
    public class Dealer
    {
        private int _smallBlindAmount;
        private int _ante;
        private int _initialStack;
        private List<object> _uuidList;
        internal MessageHandler _messageHandler;
        private MessageSummarizer _messageSummarizer;
        private Table _table;
        private Dictionary<object, object> _blindStructure;

        public Table Table => _table;

        internal Dealer(Dealer dealer)
        {
            this._smallBlindAmount = dealer._smallBlindAmount;
            this._ante = dealer._ante;
            this._initialStack = dealer._initialStack;
            this._uuidList = dealer._uuidList;
            this._messageHandler = dealer._messageHandler;
            this._messageSummarizer = dealer._messageSummarizer;
            this._table = dealer._table;
            this._blindStructure = dealer._blindStructure;
        }

        public Dealer(int smallBlindAmount, int initialStack, int ante = 0)
        {
            this._smallBlindAmount = smallBlindAmount;
            this._ante = ante;
            this._initialStack = initialStack;
            this._uuidList = this.GenerateUuidList();
            this._messageHandler = new MessageHandler();
            this._messageSummarizer = new MessageSummarizer(verbose: 0);
            this._table = new Table();
            this._blindStructure = new Dictionary<object, object>
            {
            };
        }

        public void RegisterPlayer(string player_name, BasePokerPlayer algorithm)
        {
            this.ConfigCheck();
            var uuid = this.EscortPlayerToTable(player_name, algorithm.Uuid);
            //if (algorithm.Uuid != (string)uuid)
            //{
            //    algorithm.Uuid = (string)uuid;
            //}
            this.RegisterAlgorithmToMessageHandler(uuid, algorithm);
        }

        public void SetVerbose(int verbose)
        {
            this._messageSummarizer.Verbose = verbose;
        }

        public (GameResultMessage gameResult, string resultMessage) StartGame(int max_round)
        {
            var table = this._table;
            this.NotifyGameStart(max_round);
            var ante = this._ante;
            var sb_amount = this._smallBlindAmount;
            foreach (var round_count in Enumerable.Range(1, max_round + 1 - 1))
            {
                var _tup_1 = this.UpdateForcedBetAmount(ante, sb_amount, round_count, this._blindStructure);
                ante = _tup_1.Item1;
                sb_amount = _tup_1.Item2;
                table = this.ExcludeShortOfMoneyPlayers(table, ante, sb_amount);
                if (this.IsGameFinished(table))
                {
                    break;
                }
                table = this.PlayRound(round_count, sb_amount, ante, table);
                table.ShiftDealerButton();
            }
            return this.GenerateGameResult(max_round, table.Seats);
        }

        public Table PlayRound(int round_count, float blind_amount, float ante, Table table)
        {
            var (state, messages) = RoundManager.Instance.StartNewRound(round_count, blind_amount, ante, table);
            while (true)
            {
                this.MessageCheck(messages, state.Street);
                if (state.Street != StreetType.FINISHED)
                {
                    // continue the round
                    var _tup_2 = this.PublishMessages(messages);
                    var action = _tup_2.Item1;
                    var bet_amount = _tup_2.Item2;
                    (state, messages) = RoundManager.Instance.ApplyAction(state, action, bet_amount);
                }
                else
                {
                    // finish the round after publish round result
                    this.PublishMessages(messages);
                    break;
                }
            }
            return (Table)state.Table;
        }

        public void SetSmallBlindAmount(int amount)
        {
            this._smallBlindAmount = amount;
        }

        public void SetInitialStack(int amount)
        {
            this._initialStack = amount;
        }

        public void SetBlindStructure(Dictionary<object, object> blind_structure)
        {
            this._blindStructure = blind_structure;
        }

        private Tuple<int, int> UpdateForcedBetAmount(int ante, int sb_amount, int round_count, Dictionary<object, object> blind_structure)
        {
            if (blind_structure != null && blind_structure.ContainsKey(round_count))
            {
                var update_info = (IDictionary)blind_structure[round_count];
                var msg = this._messageSummarizer.SummairzeBlindLevelUpdate(round_count, ante, update_info["ante"], sb_amount, update_info["small_blind"]);
                this._messageSummarizer.PrintMessage(msg);
                ante = Convert.ToInt32(update_info["ante"]);
                sb_amount = Convert.ToInt32(update_info["small_blind"]);
            }
            return Tuple.Create(ante, sb_amount);
        }

        private void RegisterAlgorithmToMessageHandler(object uuid, object algorithm)
        {
            this._messageHandler.RegisterAlgorithm(uuid, algorithm);
        }

        private object EscortPlayerToTable(string player_name, string uuid = null)
        {
            if (string.IsNullOrWhiteSpace(uuid))
            {
                uuid = (string)this.FetchUuid();
            }
            var player = new Player(uuid, this._initialStack, player_name);
            this._table.Seats.Sitdown(player);
            return uuid;
        }

        private void NotifyGameStart(int max_round)
        {
            var config = this.GenConfig(max_round);
            var start_msg = MessageBuilder.Instance.BuildGameStartMessage(config, _table.Seats);
            this._messageHandler.ProcessMessage(start_msg);
            this._messageSummarizer.Summarize(start_msg);
        }

        private bool IsGameFinished(Table table)
        {
            return (from player in table.Seats.Players
                    where player.IsActive()
                    select player).ToList().Count == 1;
        }

        private void MessageCheck(IEnumerable<IMessage> msgs, StreetType street)
        {
            var msg = msgs.Last();
            var invalid = MessageBuilder.GetMessageType(msg) != MessageBuilder.ASK;
            invalid &= street != StreetType.FINISHED; //(street != StreetType.FINISHED || msg.MessageType == MessageType.ROUND_RESULT_MESSAGE);
            if (invalid)
            {
                throw new Exception(String.Format("Last message is not ask type. : %s", msgs));
            }
        }

        private Tuple<ActionType, int> PublishMessages(IEnumerable<IMessage> msgs)
        {
            foreach (var m in msgs.Take(msgs.Count() - 1))
            {
                this._messageHandler.ProcessMessage(m);
            }
            this._messageSummarizer.SummarizeMessages(msgs.ToList());
            return this._messageHandler.ProcessMessage(msgs.Last());
        }

        private Table ExcludeShortOfMoneyPlayers(Table table, int ante, int sb_amount)
        {
            var _tup_1 = this.StealMoneyFromPoorPlayer(table, ante, sb_amount);
            var sb_pos = _tup_1.Item1;
            var bb_pos = _tup_1.Item2;
            this.DisableNoMoneyPlayer(table.Seats.Players);
            table.SetBlindPositions(sb_pos, bb_pos);
            if (table.Seats.Players[table.DealerButton].Stack == 0)
            {
                table.ShiftDealerButton();
            }
            return table;
        }

        private Tuple<int, int> StealMoneyFromPoorPlayer(Table table, int ante, int sb_amount)
        {
            var players = table.Seats.Players;
            // exclude player who cannot pay ante
            foreach (var player in (from p in players
                                    where p.Stack < ante
                                    select p).ToList())
            {
                player.Stack = 0;
            }
            if (players[table.DealerButton].Stack == 0)
            {
                table.ShiftDealerButton();
            }
            var search_targets = players.Concat(players).Concat(players).ToList();
            search_targets = search_targets.Skip(table.DealerButton + 1).Take(players.Count).ToList(); //new ArraySegment<Player>(search_targets.ToArray(), table.DealerButton + 1, players.Count).ToList();
            // exclude player who cannot pay small blind
            var sb_player = this.FindFirstElligiblePlayer(search_targets, sb_amount + ante);
            var sb_relative_pos = search_targets.IndexOf(sb_player);
            foreach (var player in new ArraySegment<Player>(search_targets.ToArray(), 0, sb_relative_pos))
            {
                player.Stack = 0;
            }
            // exclude player who cannot pay big blind
            search_targets = search_targets.Skip(sb_relative_pos + 1).Take(players.Count - 1).ToList(); //new ArraySegment<Player>(search_targets.ToArray(), sb_relative_pos + 1, sb_relative_pos + players.Count).ToList();
            var bb_player = this.FindFirstElligiblePlayer(search_targets, sb_amount * 2 + ante, sb_player);
            if (sb_player == bb_player)
            {
                // no one can pay big blind. So steal money from all players except small blind
                foreach (var player in (from p in players
                                        where p != bb_player
                                        select p).ToList())
                {
                    player.Stack = 0;
                }
            }
            else
            {
                var bb_relative_pos = search_targets.IndexOf(bb_player);
                for (int ix = 0; ix < bb_relative_pos; ix++)
                {
                    search_targets[ix].Stack = 0;
                }
                //foreach (var player in new ArraySegment<Player>(search_targets.ToArray(), 0, bb_relative_pos))
                //{
                //    player.Stack = 0;
                //}
            }
            return Tuple.Create(players.IndexOf(sb_player), players.IndexOf(bb_player));
        }

        private Player FindFirstElligiblePlayer(IEnumerable<Player> players, int need_amount, Player @default = null)
        {
            if (@default != null && players.Contains(@default))
            {
                return (from player in players
                        where player.Stack >= need_amount
                        select player).SkipWhile(p => p != @default).FirstOrDefault();
            }
            return (from player in players
                    where player.Stack >= need_amount
                    select player).FirstOrDefault();
        }

        private void DisableNoMoneyPlayer(IEnumerable<Player> players)
        {
            var no_money_players = (from player in players
                                    where player.Stack == 0
                                    select player).ToList();
            foreach (var player in no_money_players)
            {
                player.PayInfo.UpdateToFold();
            }
        }

        private (GameResultMessage gameResult, string resultMessage) GenerateGameResult(int max_round, Seats seats)
        {
            var config = this.GenConfig(max_round);
            var result_message = MessageBuilder.Instance.BuildGameResultMessage(config, seats);
            this._messageSummarizer.Summarize(result_message);
            //return result_message;
            return (result_message, this._messageSummarizer.Summarize(result_message));
        }

        private GameConfig GenConfig(int max_round)
        {
            return new GameConfig
            {
                MaxRound = max_round,
                Ante = this._ante,
                InitialStack = this._initialStack,
                SmallBlindAmount = this._smallBlindAmount,
                BlindStructure = this._blindStructure
            };
        }

        private void ConfigCheck()
        {
            if (this._smallBlindAmount == default)
            {
                throw new Exception("small_blind_amount is not set!! You need to call 'dealer.set_small_blind_amount' before.");
            }
            if (this._initialStack == default)
            {
                throw new Exception("initial_stack is not set!! You need to call 'dealer.set_initial_stack' before.");
            }
        }

        public virtual object FetchUuid()
        {
            var last = _uuidList.Last();
            _uuidList.Remove(last);
            return last;
        }

        private List<object> GenerateUuidList()
        {
            return (from _ in Enumerable.Range(0, 100)
                    select this.GenerateUuid()).ToList();
        }

        private object GenerateUuid()
        {
            var random = new Random();
            var uuid_size = 22;
            var chars = (from code in Enumerable.Range(97, 123 - 97)
                         select (char)code).ToList();
            return new string((from _ in Enumerable.Range(0, uuid_size)
                               select chars[random.Next(chars.Count)]).ToArray());
        }
    }
}
