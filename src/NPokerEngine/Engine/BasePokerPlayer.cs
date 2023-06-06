using NPokerEngine.Messages;
using NPokerEngine.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NPokerEngine.Engine
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
        public abstract void ReceiveGameStartMessage(GameStartMessage gameStartMessage);
        public abstract void ReceiveRoundStartMessage(RoundStartMessage roundStartMessage);
        public abstract void ReceiveStreetStartMessage(StreetStartMessage streetStartMessage);
        public abstract void ReceiveGameUpdateMessage(GameUpdateMessage gameUpdateMessage);
        public abstract void ReceiveRoundResultMessage(RoundResultMessage roundResultMessage);

        // Called from Dealer when ask message received from RoundManager
        public Tuple<ActionType, int> RespondToAsk(IMessage message)
        {
            //var _tup_1 = ParseAskMessage(message);
            //var valid_actions = _tup_1.Item1;
            //var hole_card = _tup_1.Item2;
            //var round_state = _tup_1.Item3;
            if (message is not AskMessage askMessage)
                throw new InvalidCastException($"Invalid ask type {message.GetType().Name}");
            //return DeclareAction(valid_actions, hole_card, round_state);
            var askPlayer = askMessage.State.Table.Seats[askMessage.PlayerUuid];
            return DeclareAction(askMessage.ValidActions, new HoleCards { FirstCard = askPlayer.HoleCards[0], SecondCard = askPlayer.HoleCards[1] }, askMessage.State );
        }

        // Called from Dealer when notification received from RoundManager
        public void ReceiveNotification(IMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.GAME_START_MESSAGE:
                    ReceiveGameStartMessage((GameStartMessage)message);
                    break;
                case MessageType.ROUND_START_MESSAGE:
                    ReceiveRoundStartMessage((RoundStartMessage)message);
                    break;
                case MessageType.STREET_START_MESSAGE:
                    ReceiveStreetStartMessage((StreetStartMessage)message);
                    break;
                case MessageType.ASK_MESSAGE:
                    break;
                case MessageType.GAME_UPDATE_MESSAGE:
                    ReceiveGameUpdateMessage((GameUpdateMessage)message);
                    break;
                case MessageType.ROUND_RESULT_MESSAGE:
                    ReceiveRoundResultMessage((RoundResultMessage)message);
                    break;
                case MessageType.GAME_RESULT_MESSAGE:
                    break;
                default:
                    throw new ArgumentException($"{message.MessageType}");
            }
            //object state;
            //var msg_type = (string)message["message_type"];
            //if (msg_type == "game_start_message")
            //{
            //    var info = ParseGameStartMessage(message);
            //    ReceiveGameStartMessage(info);
            //}
            //else if (msg_type == "round_start_message")
            //{
            //    var _tup_1 = ParseRoundStartMessage(message);
            //    var round_count = _tup_1.Item1;
            //    var hole = _tup_1.Item2;
            //    var seats = _tup_1.Item3;
            //    ReceiveRoundStartMessage(round_count, hole, seats);
            //}
            //else if (msg_type == "street_start_message")
            //{
            //    var _tup_2 = ParseStreetStartMessage(message);
            //    var street = _tup_2.Item1;
            //    state = _tup_2.Item2;
            //    ReceiveStreetStartMessage(street, state);
            //}
            //else if (msg_type == "game_update_message")
            //{
            //    var _tup_3 = ParseGameUpdateMessage(message);
            //    var new_action = _tup_3.Item1;
            //    var round_state = _tup_3.Item2;
            //    ReceiveGameUpdateMessage(new_action, round_state);
            //}
            //else if (msg_type == "round_result_message")
            //{
            //    var _tup_4 = ParseRoundResultMessage(message);
            //    var winners = _tup_4.Item1;
            //    var hand_info = _tup_4.Item2;
            //    state = _tup_4.Item3;
            //    ReceiveRoundResultMessage(winners, hand_info, state);
            //}
        }

        private Tuple<IEnumerable, HoleCards, object> ParseAskMessage(IDictionary message)
        {
            var hole_card = (HoleCards)message["hole_card"];
            var valid_actions = (IEnumerable)message["valid_actions"];
            var round_state = message["round_state"];
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
            var round_state = message["round_state"];
            return Tuple.Create(street, round_state);
        }

        private Tuple<ActionType, object> ParseGameUpdateMessage(IDictionary message)
        {
            var new_action = (ActionType)message["action"];
            var round_state = message["round_state"];
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
