using NPokerEngine.Engine;
using NPokerEngine.Messages;
using NPokerEngine.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPokerEngine
{
    public class Game
    {
        private readonly GameConfig _config;

        public Game()
        {
            _config = new GameConfig
            {
                InitialStack = 100,
                MaxRound = 10,
                SmallBlindAmount = 5,
                Ante = 3,
                BlindStructure = null
            };
        }

        public void SetupConfig(int max_round, float initial_stack, float small_blind_amount, float ante= 0)
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

        public GameResultMessage StartPoker(params BasePokerPlayer[] players)
        {
            if (players == null)
                throw new ArgumentNullException();
            if (players.Length < 2)
                throw new Exception($"At least 2 players are needed to start the game, actual: {players.Count()}");

            var dealer = new Dealer(Convert.ToInt32(_config.SmallBlindAmount), Convert.ToInt32(_config.InitialStack), Convert.ToInt32(_config.Ante));
            dealer.SetBlindStructure(_config.BlindStructure);
            foreach (var player in players) 
            {
                dealer.RegisterPlayer(player.Name, player);
            }

            var (result, msg) = dealer.StartGame(_config.MaxRound);
            return result;
        }
    }
}
