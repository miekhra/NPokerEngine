using NPokerEngine.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPokerEngine.Engine
{
    public class MessageSummarizer
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
            var summaries = (from raw_message in raw_messages.Cast<IDictionary>()
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

        public string Summarize(IDictionary message)
        {
            if (this._verbose == 0)
            {
                return string.Empty;
            }
            var content = (IDictionary)message["message"];
            var message_type = (string)content["message_type"];
            if (MessageBuilder.GAME_START_MESSAGE == message_type)
            {
                return this.SummarizeGameStart(content);
            }
            if (MessageBuilder.ROUND_START_MESSAGE == message_type)
            {
                return this.SummarizeRoundStart(content);
            }
            if (MessageBuilder.STREET_START_MESSAGE == message_type)
            {
                return this.SummarizeStreetStart(content);
            }
            if (MessageBuilder.GAME_UPDATE_MESSAGE == message_type)
            {
                return this.SummarizePlayerAction(content);
            }
            if (MessageBuilder.ROUND_RESULT_MESSAGE == message_type)
            {
                return this.SummarizeRoundResult(content);
            }
            if (MessageBuilder.GAME_RESULT_MESSAGE == message_type)
            {
                return this.SummarizeGameResult(content);
            }
            return string.Empty;
        }

        public string SummarizeGameStart(IDictionary message)
        {
            var seats = ((IList)((IDictionary)message["game_information"])["seats"]);
            var names = (from player in seats.Cast<IDictionary>()
                         select player["name"].ToString()).ToList();
            var rule = ((IDictionary)message["game_information"])["rule"] as IDictionary;
            return $"Started the game with player {string.Format(", ", names)} for {rule["max_round"]} round. (start stack={rule["initial_stack"]}, small blind={rule["small_blind_amount"]})";
        }

        public string SummarizeRoundStart(IDictionary message)
        {
            return $"Started the round {message["round_count"]}";
        }

        public string SummarizeStreetStart(IDictionary message)
        {
            return $"Street \"%s\" started. (community card = {((IDictionary)message["round_state"])["community_card"]})"; ;
        }

        public string SummarizePlayerAction(IDictionary message)
        {
            var players = ((IDictionary)((IDictionary)message)["round_state"])["seats"] as IList;
            var action = (IDictionary)message["action"];
            var player_name = (from player in players.Cast<IDictionary>()
                               where player["uuid"] == action["player_uuid"]
                               select player["name"]).ToList()[0];
            return $"\"{player_name}\" declared \"{action["action"]}:{action["amount"]}\"";
        }

        public string SummarizeRoundResult(IDictionary message)
        {
            var seats = ((IList)((IDictionary)message["game_information"])["seats"]);
            var winners = (from player in ((IDictionary)message["winners"]).Cast<object>()
                           select (((IDictionary)player)["name"]).ToString()).ToList();
            var stack = new Dictionary<string, int>();
            foreach (var item in seats)
            {
                stack.Add((((IDictionary)item)["name"]).ToString(), (int)((IDictionary)item)["stack"]);
            }
            return $"\"{string.Format(", ", winners)}\" won the round %d (stack = {DictionaryUtils.PrintForMessageSummarizer(stack)})";
        }

        public string SummarizeGameResult(IDictionary message)
        {
            var seats = ((IList)((IDictionary)message["game_information"])["seats"]);
            var stack = new Dictionary<string, int>();
            foreach (var item in seats)
            {
                stack.Add((((IDictionary)item)["name"]).ToString(), (int)((IDictionary)item)["stack"]);
            }
            return $"Game finished. (stack = {DictionaryUtils.PrintForMessageSummarizer(stack)})";
        }

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
