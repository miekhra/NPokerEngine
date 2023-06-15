using NPokerEngine.Messages;
using NPokerEngine.Types;
using System;
using System.Collections;
using System.Linq;

namespace NPokerEngine.Engine
{
    internal class MessageSummarizer
    {
        private int _verbose;

        public int Verbose { get => _verbose; set => _verbose = value; }

        public MessageSummarizer(int verbose = 0)
        {
            this._verbose = verbose;
        }

        public void PrintMessage(object message)
        {
            Console.WriteLine(message);
        }

        public void SummarizeMessages(IList raw_messages)
        {
            if (this._verbose == 0)
            {
                return;
            }
            var summaries = (from raw_message in raw_messages.Cast<IMessage>()
                             select this.Summarize(raw_message)).ToList();
            summaries = (from summary in summaries
                         where summary != null
                         select summary).ToList();
            //summaries = OrderedDict.fromkeys(summaries).ToList();
            foreach (var summary in summaries)
            {
                this.PrintMessage(summary);
            }
        }

        public string Summarize(IMessage message)
        {
            if (this._verbose == 0)
            {
                return string.Empty;
            }
            switch (message.MessageType)
            {
                case MessageType.GAME_START_MESSAGE:
                    return this.SummarizeGameStart((GameStartMessage)message);
                case MessageType.ROUND_START_MESSAGE:
                    return this.SummarizeRoundStart((RoundStartMessage)message);
                case MessageType.STREET_START_MESSAGE:
                    return this.SummarizeStreetStart((StreetStartMessage)message);
                case MessageType.GAME_UPDATE_MESSAGE:
                    return this.SummarizePlayerAction((GameUpdateMessage)message);
                case MessageType.ROUND_RESULT_MESSAGE:
                    return this.SummarizeRoundResult((RoundResultMessage)message);
                case MessageType.GAME_RESULT_MESSAGE:
                    return this.SummarizeGameResult((GameResultMessage)message);
                default:
                    return string.Empty;
            };
        }

        public string SummarizeGameStart(GameStartMessage message)
        {
            return $"Started the game with player {string.Join(", ", message.Seats.Players.Select(t => t.Name))} for {message.Config.MaxRound} round. (start stack={message.Config.InitialStack}, small blind={message.Config.SmallBlindAmount})";
        }

        public string SummarizeRoundStart(RoundStartMessage message)
        {
            return $"Started the round {message.RoundCount}";
        }

        public string SummarizeStreetStart(StreetStartMessage message)
        {
            return $"Street {message.Street} started. (community card = {string.Join("", message.GameState.Table.CommunityCards.Select(t => t.ToString()))})";
        }

        public string SummarizePlayerAction(GameUpdateMessage message)
        {
            return $"Player {message.Seats.Players.Single(t => t.Uuid == message.PlayerUuid).Name} declared action {message.Action}: {message.Amount}";
        }

        public string SummarizeRoundResult(RoundResultMessage message)
        {
            return $"{string.Join(", ", message.Winners.Select(t => t.Name))} won the round {message.RoundCount} (stack = {PrintForMessageSummarizer(message.State.Table.Seats.Players.ToDictionary(k => k.Name, v => v.Stack))})";
        }

        public string SummarizeGameResult(GameResultMessage message)
        {
            return $"Game finished. (stack = {PrintForMessageSummarizer(message.Seats.Players.ToDictionary(k => k.Name, v => v.Stack))})";
        }

        private static string PrintForMessageSummarizer(IDictionary source)
            => $"{{{string.Join(", ", source.Keys.Cast<object>().ToList().Select(key => $"'{key}': {source[key]}"))}}}";

        public string SummairzeBlindLevelUpdate(
            object round_count,
            object old_ante,
            object new_ante,
            object old_sb_amount,
            object new_sb_amount)
        {
            var @base = "Blind level update at round-%d : Ante %s -> %s, SmallBlind %s -> %s";
            return String.Format(@base, round_count, old_ante, new_ante, old_sb_amount, new_sb_amount);
        }
    }
}
