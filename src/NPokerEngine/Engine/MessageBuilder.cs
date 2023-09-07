using NPokerEngine.Messages;
using NPokerEngine.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace NPokerEngine.Engine
{
    internal class MessageBuilder : Singleton<MessageBuilder>
    {
        public const string ASK = "ask";
        public const string NOTIFICATION = "notification";

        private static Dictionary<MessageType, string> _actionTypesMap =
            typeof(MessageType)
                .GetEnumValues()
                .OfType<MessageType>()
                .ToDictionary
                (
                    k => k,
                    v => ((CategoryAttribute)(typeof(MessageType).GetMember(v.ToString()).First().GetCustomAttributes(typeof(CategoryAttribute), false)[0])).Category
                );

        public GameStartMessage BuildGameStartMessage(GameConfig config, Seats seats)
        {
            return new GameStartMessage
            {
                Config = config,
                Seats = seats
            };
        }

        public static string GetMessageType(IMessage message)
            => _actionTypesMap[message.MessageType];

        public virtual RoundStartMessage BuildRoundStartMessage(int roundCount, int playerPos, Seats seats)
        {
            return new RoundStartMessage
            {
                RoundCount = roundCount,
                PlayerUuid = seats.Players[playerPos].Uuid,
                HoleCards = seats.Players[playerPos].HoleCards.ToList(),
                Players = seats.Players.ToList()
            };
            //var player = seats.Players[playerPos];
            //var holeCards = DataEncoder.Instance.EncodePlayer(player, holecards: true)["hole_card"];
            //var message = new Dictionary<object, object> {
            //        {
            //            "message_type",
            //            ROUND_START_MESSAGE},
            //        {
            //            "round_count",
            //            roundCount},
            //        {
            //            "hole_card",
            //            holeCards}};
            //foreach (var item in DataEncoder.Instance.EncodeSeats(seats))
            //{
            //    message[item.Key] = item.Value;
            //}
            //return this.BuildNotificationMessage(message);
        }

        public virtual StreetStartMessage BuildStreetStartMessage(GameState state)
        {
            return new StreetStartMessage
            {
                Street = state.Street,
                GameState = state
            };
            //var message = new Dictionary<object, object> {
            //        {
            //            "message_type",
            //            STREET_START_MESSAGE},
            //        {
            //            "round_state",
            //            DataEncoder.Instance.EncodeRoundState(state)}};
            //foreach (var item in DataEncoder.Instance.EncodeStreet(Convert.ToByte(state["street"])))
            //{
            //    message[item.Key] = item.Value;
            //}
            //return this.BuildNotificationMessage(message);
        }

        public virtual AskMessage BuildAskMessage(int playerPos, GameState state)
        {
            var players = state.Table.Seats.Players;
            var player = players[playerPos];
            var validActions = ActionChecker.Instance.LegalActions(players, playerPos, Convert.ToInt32(state.SmallBlindAmount));
            return new AskMessage
            {
                PlayerUuid = player.Uuid,
                ValidActions = validActions.ToDictionary
                (
                    k => k.Key, //(ActionType)Enum.Parse(typeof(ActionType), k["action"].ToString(), ignoreCase: true),
                    v => v.Value.AsTuple()
                    //{
                    //    if (v["amount"] is Dictionary<object, object> amountDictionary)
                    //        return new Tuple<float, float?>(Convert.ToSingle(amountDictionary["min"]), Convert.ToSingle(amountDictionary["max"]));
                    //    return new Tuple<float, float?>(Convert.ToSingle(v["amount"]), null);
                    //}
                ),
                State = state
            };
        }

        public virtual GameUpdateMessage BuildGameUpdateMessage(int playerPos, ActionType action, float amount, GameState state)
        {
            var player = state.Table.Seats.Players[playerPos];
            return new GameUpdateMessage
            {
                Action = action,
                Amount = amount,
                PlayerUuid = player.Uuid,
                Seats = state.Table.Seats
            };
        }

        public virtual RoundResultMessage BuildRoundResultMessage(int round_count, IEnumerable<Player> winners, object hand_info, GameState state, Dictionary<int, float> prizeMap)
        {
            return new RoundResultMessage
            {
                RoundCount = round_count,
                Winners = winners.ToList(),
                State = state,
                PrizeMap = prizeMap
            };
        }

        public GameResultMessage BuildGameResultMessage(GameConfig config, Seats seats)
        {
            return new GameResultMessage
            {
                Config = config,
                Seats = seats
            };
            //var message = new Dictionary<string, object> {
            //        {
            //            "message_type",
            //            GAME_RESULT_MESSAGE},
            //        {
            //            "game_information",
            //            DataEncoder.Instance.EncodeGameInformation(config, seats)}};
            //return this.BuildNotificationMessage(message);
        }

        //private Dictionary<string, object> BuildAskMessage(object message)
        //{
        //    return new Dictionary<string, object> {
        //            {
        //                "type",
        //                "ask"},
        //            {
        //                "message",
        //                message}};
        //}

        //private Dictionary<string, object> BuildNotificationMessage(object message)
        //{
        //    return new Dictionary<string, object> {
        //            {
        //                "type",
        //                "notification"
        //        },
        //            {
        //                "message",
        //                message
        //        }
        //    };
        //}
    }
}
