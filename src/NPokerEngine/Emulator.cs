using NPokerEngine.Engine;
using NPokerEngine.Messages;
using NPokerEngine.Players;
using NPokerEngine.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace NPokerEngine
{
    public class Emulator
    {
        private readonly List<PokerPlayer> _playersMap = new();
        private readonly GameConfig _config = new GameConfig
            {
                InitialStack = 100,
                MaxRound = 10,
                SmallBlindAmount = 5,
                Ante = 3,
                BlindStructure = null
            };

        public void SetupConfig(int max_round, float initial_stack, float small_blind_amount, float ante = 0)
        {
            _config.MaxRound = max_round;
            _config.InitialStack = initial_stack;
            _config.SmallBlindAmount = small_blind_amount;
            _config.Ante = ante;
        }

        public void SetBlindStructure(Dictionary<object, object> blind_structure)
        {
            _config.BlindStructure = blind_structure;
        }

        public PokerPlayer this[Guid guid] 
            => _playersMap.SingleOrDefault(p => p.Guid == guid);

        public void RegisterPlayer(PokerPlayer player)
        {
            if (_playersMap.Any(p => p.Guid == player.Guid))
                throw new ConstraintException();
            if (player.Guid == Guid.Empty)
                throw new ArgumentNullException(nameof(player.Guid));
            _playersMap.Add(player);
        }

        public List<(ActionType possibleAction, AmountInterval amountInterval)> GeneratePossibleActions(GameState gameState)
        {
            return ActionChecker
                .Instance
                .LegalActions(gameState.Table.Seats.Players, gameState.NextPlayerIx, gameState.SmallBlindAmount)
                .Select(t => (t.Key, t.Value))
                .ToList();
        }

        public (GameState gameState, List<IMessage> messages) ApplyAction(GameState gameState, ActionType actionType, float betAmount = 0)
        {
            var messages = new List<IMessage>();
            if (gameState.Street == StreetType.FINISHED)
            {
                (gameState, messages) = StartNextRound(gameState);
            }
                
            var (updatedState, msgs) =  RoundManager.Instance.ApplyAction(gameState, actionType, betAmount);
            gameState = updatedState;
            messages.AddRange(msgs);

            if (IsLastRound(gameState))
            {
                messages.Add(GenerateGameResultMessage(gameState));
            }

            return (gameState, messages);
        }

        public (GameState gameState, List<IMessage> messages) RunUntilRoundFinish(GameState gameState)
        {
            var mailbox = new List<IMessage>();
            while (gameState.Street != StreetType.FINISHED) 
            { 
                var nextPlayerIx = gameState.NextPlayerIx;
                var nextPlayerUuid = gameState.Table.Seats[nextPlayerIx].Uuid;
                var nextPlayerAlgo = _playersMap.Single(p => p.Uuid == nextPlayerUuid);
                var msg = MessageBuilder.Instance.BuildAskMessage(nextPlayerIx, gameState);
                var gameAction = nextPlayerAlgo.DeclareAction(msg.ValidActions, HoleCards.FromSequence(gameState.Table.Seats[nextPlayerIx].HoleCards), gameState);
                var (updatedState, messages) = RoundManager.Instance.ApplyAction(gameState, gameAction.Item1, gameAction.Item2);
                gameState = updatedState;
                mailbox.AddRange(messages);
            }

            if (IsLastRound(gameState))
                mailbox.Add(GenerateGameResultMessage(gameState));

            return (gameState, mailbox);
        }

        public (GameState gameState, List<IMessage> messages) RunUntilGameFinish(GameState gameState)
        {
            var mailbox = new List<IMessage>();

            if (gameState.Street != StreetType.FINISHED)
            {
                var (roundGameState, roundMessages) = RunUntilRoundFinish(gameState);
                gameState = roundGameState;
                mailbox.AddRange(roundMessages);
            }

            while (true)
            {
                var (startRoundGameState, startRoundMessages) = StartNewRound(gameState);
                mailbox.AddRange(startRoundMessages);
                gameState = startRoundGameState;
                if (startRoundMessages.Last() is GameResultMessage) break;

                var (roundGameState, roundMessages) = RunUntilRoundFinish(gameState);
                mailbox.AddRange(roundMessages);
                gameState = roundGameState;
                if (roundMessages.Last() is GameResultMessage) break;
            }

            return (gameState, mailbox);
        }

        private GameResultMessage GenerateGameResultMessage(GameState gameState)
        {
            return new GameResultMessage 
            { 
                Seats = gameState.Table.Seats,
                Config = _config
            };
        }

        internal bool IsLastRound(GameState gameState)
        {
            var isRoundFinished = gameState.Street == StreetType.FINISHED;
            var isFinalRound = gameState.RoundCount >= _config.MaxRound;
            var isWinnerDecided = gameState.Table.Seats.Players.Where(p => p.Stack != 0).Count() == 1;
            return isRoundFinished && (isFinalRound || isWinnerDecided);
        }

        private (GameState gameState, List<IMessage> messages) StartNextRound(GameState gameState) 
        { 
            var messages = new List<IMessage>();
            var gameFinished = gameState.RoundCount >= _config.MaxRound;

            (gameState, messages) = StartNewRound(gameState);

            if (messages.Last() is GameResultMessage || gameFinished)
                throw new Exception("Failed to apply action. Because game is already finished.");

            return (gameState, messages);
        }

        public (GameState gameState, List<IMessage> messages) StartNewRound(GameState gameState)
        {
            var roundCount = gameState.RoundCount + 1;
            var ante = _config.Ante;
            var sbAmount = _config.SmallBlindAmount;

            var copy = (GameState)gameState.Clone();
            copy.Table.ShiftDealerButton();

            (ante, sbAmount) = UpdateBlindLevel(ante, sbAmount, roundCount);
            ExcludeShortOfMoneyPlayers(copy.Table, ante, sbAmount);
            var isGameFinished = copy.Table.Seats.Players.Where(p => p.IsActive()).Count() == 1;
            if (isGameFinished)
            {
                return (copy, new List<IMessage> { GenerateGameResultMessage(copy) });
            }

            return RoundManager.Instance.StartNewRound(roundCount, sbAmount, ante, copy.Table);
        }

        private (float ante, float sbAmount) UpdateBlindLevel(float ante, float sbAmount, int roundCount)
        {
            if (_config.BlindStructure == null)
            {
                return (ante, sbAmount);
            }
            
            if (_config.BlindStructure.ContainsKey(roundCount))
            {
                var updateInfo = (IDictionary)_config.BlindStructure[roundCount];

                return (Convert.ToSingle(updateInfo["ante"]), Convert.ToSingle(updateInfo["small_blind"]));
            }

            var keys = _config.BlindStructure.Keys.Cast<int>().Where(k => k < roundCount).OrderBy(k => k);
            if (!keys.Any()) 
            {
                return (ante, sbAmount);
            }

            var updateInfo2 = (IDictionary)_config.BlindStructure[keys.Last()];

            return (Convert.ToSingle(updateInfo2["ante"]), Convert.ToSingle(updateInfo2["small_blind"]));
        }

        private void ExcludeShortOfMoneyPlayers(Table table, float ante, float sbAmount)
        {
            var (sb, bb) = SteelMoneyFromPoorPlayer(table, ante, sbAmount);
            DisableNoMoneyPlayers(table.Seats.Players);
            table.SetBlindPositions(sb, bb);
            if (table.Seats.Players[table.DealerButton].Stack <= 0)
            {
                table.ShiftDealerButton();
            }
        }

        private (int sb, int bb) SteelMoneyFromPoorPlayer(Table table, float ante, float sbAmount)
        {
            var players = table.Seats.Players;
            foreach (var p in players.Where(p => p.Stack <= ante))
            {
                p.Stack = 0;
            }

            if (players[table.DealerButton].Stack <= 0)
            {
                table.ShiftDealerButton();
            }
            
            var searchTargets = players.Concat(players).Concat(players).ToList();
            searchTargets = searchTargets.Skip(table.DealerButton + 1).Take(players.Count).ToList();
            //exclude player who cannot pay small blind
            var sbPlayer = FindFirstElligiblePlayer(searchTargets, sbAmount + ante);
            var sbPlayerRelativePos = searchTargets.IndexOf(sbPlayer);
            foreach (var player in new ArraySegment<Player>(searchTargets.ToArray(), 0, sbPlayerRelativePos))
            {
                player.Stack = 0;
            }
            // exclude player who cannot pay big blind
            searchTargets = searchTargets.Skip(sbPlayerRelativePos + 1).Take(players.Count - 1).ToList();
            var bbPlayer = this.FindFirstElligiblePlayer(searchTargets, sbAmount * 2 + ante, sbPlayer);
            if (sbPlayer == bbPlayer)
            {
                // no one can pay big blind. So steal money from all players except small blind
                foreach (var player in (from p in players
                                        where p != bbPlayer
                                        select p).ToList())
                {
                    player.Stack = 0;
                }
            }
            else
            {
                var bb_relative_pos = searchTargets.IndexOf(bbPlayer);
                for (int ix = 0; ix < bb_relative_pos; ix++)
                {
                    searchTargets[ix].Stack = 0;
                }
            }
            return (players.IndexOf(sbPlayer), players.IndexOf(bbPlayer));
        }

        private void DisableNoMoneyPlayers(IEnumerable<Player> players)
        {
            foreach (var player in players.Where(p => p.Stack <= 0)) 
            {
                player.PayInfo.UpdateToFold();
            }
        }

        private Player FindFirstElligiblePlayer(IEnumerable<Player> players, float need_amount, Player @default = null)
        {
            return (from player in players
                    where player.Stack >= need_amount
                    select player).FirstOrDefault() ?? @default;
        }

        public GameState GenerateInitialState()
        {
            var table = new Table();
            this._playersMap.ForEach(p => table.Seats.Sitdown(new Player(p.Uuid, (int)_config.InitialStack, p.Name)));
            table._dealerButton = _playersMap.Count - 1;

            return new GameState
            {
                Table = table,
                SmallBlindAmount = _config.SmallBlindAmount,
                Street = StreetType.PREFLOP,
                RoundCount = 0,
                NextPlayerIx = -1
            };
        }
    }
}
