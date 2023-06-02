using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NPokerEngine.Types;

namespace NPokerEngine.Engine
{
    public class MessageBuilder : IMessageBuilder
    {
        public const string GAME_START_MESSAGE = "game_start_message";
        public const string ROUND_START_MESSAGE = "round_start_message";
        public const string STREET_START_MESSAGE = "street_start_message";
        public const string ASK_MESSAGE = "ask_message";
        public const string GAME_UPDATE_MESSAGE = "game_update_message";
        public const string ROUND_RESULT_MESSAGE = "round_result_message";
        public const string GAME_RESULT_MESSAGE = "game_result_message";

        private static MessageBuilder _instance;
        public static MessageBuilder Instance
        {
            get
            {
                _instance = _instance ?? new MessageBuilder();
                return _instance;
            }
        }

        private MessageBuilder() { }

        public Dictionary<string, object> BuildGameStartMessage(Dictionary<string, object> config, Seats seats)
        {
            var message = new Dictionary<object, object> {
                    {
                        "message_type",
                        GAME_START_MESSAGE},
                    {
                        "game_information",
                        DataEncoder.Instance.EncodeGameInformation(config, seats)}};
            return this.BuildNotificationMessage(message);
        }

        public Dictionary<string, object> BuildRoundStartMessage(int roundCount, int playerPos, Seats seats)
        {
            var player = seats.Players[playerPos];
            var holeCards = DataEncoder.Instance.EncodePlayer(player, holecards: true)["hole_card"];
            var message = new Dictionary<object, object> {
                    {
                        "message_type",
                        ROUND_START_MESSAGE},
                    {
                        "round_count",
                        roundCount},
                    {
                        "hole_card",
                        holeCards}};
            foreach (var item in DataEncoder.Instance.EncodeSeats(seats))
            {
                message[item.Key] = item.Value;
            }
            return this.BuildNotificationMessage(message);
        }

        public Dictionary<string, object> BuildStreetStartMessage(Dictionary<string, object> state)
        {
            var message = new Dictionary<object, object> {
                    {
                        "message_type",
                        STREET_START_MESSAGE},
                    {
                        "round_state",
                        DataEncoder.Instance.EncodeRoundState(state)}};
            foreach (var item in DataEncoder.Instance.EncodeStreet(Convert.ToByte(state["street"])))
            {
                message[item.Key] = item.Value;
            }
            return this.BuildNotificationMessage(message);
        }

        public Dictionary<string, object> BuildAskMessage(int playerPos, GameState state)
        {
            var players = state.Table.Seats.Players;
            var player = players[playerPos];
            var holeCards = DataEncoder.Instance.EncodePlayer(player, holecards: true)["hole_card"];
            var validActions = ActionChecker.Instance.LegalActions(players, playerPos, Convert.ToInt32(state.SmallBlindAmount));
            var message = new Dictionary<object, object> {
                    {
                        "message_type",
                        ASK_MESSAGE},
                    {
                        "hole_card",
                        holeCards},
                    {
                        "valid_actions",
                        validActions},
                    {
                        "round_state",
                        DataEncoder.Instance.EncodeRoundState(state.ToDictionary())},
                    {
                        "action_histories",
                        DataEncoder.Instance.EncodeActionHistories(state.Table)}};
            return this.BuildAskMessage(message);
        }

        public Dictionary<string, object> BuildGameUpdateMessage(int playerPos, object action, object amount, Dictionary<string, object> state)
        {
            var player = ((Table)state["table"]).Seats.Players[playerPos];
            var message = new Dictionary<object, object> {
                    {
                        "message_type",
                        GAME_UPDATE_MESSAGE},
                    {
                        "action",
                        DataEncoder.Instance.EncodeAction(player, action, amount)},
                    {
                        "round_state",
                        DataEncoder.Instance.EncodeRoundState(state)},
                    {
                        "action_histories",
                        DataEncoder.Instance.EncodeActionHistories((Table)state["table"])}};
            return this.BuildNotificationMessage(message);
        }

        public Dictionary<string, object> BuildRoundResultMessage(object round_count, IEnumerable<Player> winners, object hand_info, Dictionary<string, object> state)
        {
            var message = new Dictionary<string, object> {
                    {
                        "message_type",
                        ROUND_RESULT_MESSAGE},
                    {
                        "round_count",
                        round_count},
                    {
                        "hand_info",
                        hand_info},
                    {
                        "round_state",
                        DataEncoder.Instance.EncodeRoundState(state)}};

            foreach (var item in DataEncoder.Instance.EncodeWinners(winners))
            {
                message[item.Key] = item.Value;
            }

            return this.BuildNotificationMessage(message);
        }

        public Dictionary<string, object> BuildGameResultMessage(IDictionary config, Seats seats)
        {
            var message = new Dictionary<string, object> {
                    {
                        "message_type",
                        GAME_RESULT_MESSAGE},
                    {
                        "game_information",
                        DataEncoder.Instance.EncodeGameInformation(config, seats)}};
            return this.BuildNotificationMessage(message);
        }

        private Dictionary<string, object> BuildAskMessage(object message)
        {
            return new Dictionary<string, object> {
                    {
                        "type",
                        "ask"},
                    {
                        "message",
                        message}};
        }

        private Dictionary<string, object> BuildNotificationMessage(object message)
        {
            return new Dictionary<string, object> {
                    {
                        "type",
                        "notification"
                },
                    {
                        "message",
                        message
                }
            };
        }
    }
}
