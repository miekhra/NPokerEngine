using NPokerEngine.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NPokerEngine
{
    public abstract class BasePokerPlayer
    {
        private string _uuid;
        public string Uuid 
        { 
            get => _uuid; 
            set 
            {
                if (!string.IsNullOrEmpty(_uuid))
                    throw new ArgumentException($"Uuid already set {_uuid}");
                _uuid = value;
            } 
        }
        public abstract Tuple<ActionType, int> DeclareAction(IEnumerable validActions, HoleCards holeCards, object roundState);
        public abstract void ReceiveGameStartMessage(IDictionary gameInfo);
        public abstract void ReceiveRoundStartMessage(int roundCount, HoleCards holeCards, Seats seats);
        public abstract void ReceiveStreetStartMessage(StreetType street, object roundState);
        public abstract void ReceiveGameUpdateMessage(ActionType actionType, object roundState);
        public abstract void ReceiveRoundResultMessage(IEnumerable<Player> winners, object handInfo, object roundState);

        // Called from Dealer when ask message received from RoundManager
        public Tuple<ActionType, int> RespondToAsk(IDictionary message)
        {
            var _tup_1 = this.ParseAskMessage(message);
            var valid_actions = _tup_1.Item1;
            var hole_card = _tup_1.Item2;
            var round_state = _tup_1.Item3;
            return this.DeclareAction(valid_actions, hole_card, round_state);
        }

        // Called from Dealer when notification received from RoundManager
        public void ReceiveNotification(IDictionary message)
        {
            object state;
            var msg_type = (string)message["message_type"];
            if (msg_type == "game_start_message")
            {
                var info = this.ParseGameStartMessage(message);
                this.ReceiveGameStartMessage(info);
            }
            else if (msg_type == "round_start_message")
            {
                var _tup_1 = this.ParseRoundStartMessage(message);
                var round_count = _tup_1.Item1;
                var hole = _tup_1.Item2;
                var seats = _tup_1.Item3;
                this.ReceiveRoundStartMessage(round_count, hole, seats);
            }
            else if (msg_type == "street_start_message")
            {
                var _tup_2 = this.ParseStreetStartMessage(message);
                var street = _tup_2.Item1;
                state = _tup_2.Item2;
                this.ReceiveStreetStartMessage(street, state);
            }
            else if (msg_type == "game_update_message")
            {
                var _tup_3 = this.ParseGameUpdateMessage(message);
                var new_action = _tup_3.Item1;
                var round_state = _tup_3.Item2;
                this.ReceiveGameUpdateMessage(new_action, round_state);
            }
            else if (msg_type == "round_result_message")
            {
                var _tup_4 = this.ParseRoundResultMessage(message);
                var winners = _tup_4.Item1;
                var hand_info = _tup_4.Item2;
                state = _tup_4.Item3;
                this.ReceiveRoundResultMessage(winners, hand_info, state);
            }
        }

        private Tuple<IEnumerable, HoleCards, object> ParseAskMessage(IDictionary message)
        {
            var hole_card = (HoleCards)message["hole_card"];
            var valid_actions = (IEnumerable)message["valid_actions"];
            var round_state = (object)message["round_state"];
            return Tuple.Create(valid_actions, hole_card, round_state);
        }

        private IDictionary ParseGameStartMessage(IDictionary message)
        {
            var game_info = (IDictionary)message["game_information"];
            return game_info;
        }

        private Tuple<int, HoleCards, Seats> ParseRoundStartMessage(IDictionary message)
        {
            var round_count = (int)message["round_count"];
            var seats = (Seats)message["seats"];
            var hole_card = (HoleCards)message["hole_card"];
            return Tuple.Create(round_count, hole_card, seats);
        }

        private Tuple<StreetType, object> ParseStreetStartMessage(IDictionary message)
        {
            var street = (StreetType)message["street"];
            var round_state = (object)message["round_state"];
            return Tuple.Create(street, round_state);
        }

        private Tuple<ActionType, object> ParseGameUpdateMessage(IDictionary message)
        {
            var new_action = (ActionType)message["action"];
            var round_state = (object)message["round_state"];
            return Tuple.Create(new_action, round_state);
        }

        private Tuple<IEnumerable<Player>, object, object> ParseRoundResultMessage(IDictionary message)
        {
            var winners = (IEnumerable<Player>)message["winners"];
            var hand_info = message["hand_info"];
            var round_state = message["round_state"];
            return Tuple.Create(winners, hand_info, round_state);
        }

    }
}
