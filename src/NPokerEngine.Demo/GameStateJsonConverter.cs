using NPokerEngine.Types;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Xml.Linq;

namespace NPokerEngine.Demo
{
    public class GameStateJsonConverter : JsonConverter<GameState>
    {
        public override GameState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var gameStateObject = JsonSerializer.Deserialize<JsonObject>(ref reader, options);

            var gameState = new GameState();
            gameState.Street = Enum.Parse<StreetType>(gameStateObject[nameof(GameState.Street)].GetValue<string>());
            gameState.SmallBlindAmount = gameStateObject[nameof(GameState.SmallBlindAmount)].GetValue<float>();
            gameState.RoundCount = gameStateObject[nameof(GameState.RoundCount)].GetValue<int>();
            gameState.NextPlayerIx = gameStateObject[nameof(GameState.NextPlayerIx)].GetValue<int>();
            gameState.Table = new Table();

            var tableObject = gameStateObject[nameof(GameState.Table)].AsObject();
            int? sb = null, bb = null;
            if (tableObject[nameof(Table.SmallBlindPosition)] != null)
            {
                sb = tableObject[nameof(Table.SmallBlindPosition)].GetValue<int>();
            }
            if (tableObject[nameof(Table.BigBlindPosition)] != null)
            {
                bb = tableObject[nameof(Table.BigBlindPosition)].GetValue<int>();
            }
            gameState.Table.SetBlindPositions(sb, bb);
            typeof(Table).GetField("_dealerButton", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(gameState.Table, tableObject[nameof(Table.DealerButton)].GetValue<int>());
            foreach (var card in JsonArrayToCards(tableObject[nameof(Table.CommunityCards)].AsArray()))
            {
                gameState.Table.AddCommunityCard(card);
            }

            foreach (JsonObject playerObject in tableObject[nameof(Table.Seats)].AsArray())
            {
                var player = new Player(
                    uuid: playerObject[nameof(Player.Uuid)].GetValue<string>(),
                    initialStack: playerObject[nameof(Player.Stack)].GetValue<int>(),
                    name: playerObject[nameof(Player.Name)].GetValue<string>());

                var payInfoObject = playerObject[nameof(Player.PayInfo)].AsObject();
                player.PayInfo.UpdateByPay(payInfoObject[nameof(PayInfo.Amount)].GetValue<float>());

                var payInfoStatus = Enum.Parse<PayInfoStatus>(payInfoObject[nameof(PayInfo.Status)].GetValue<string>());
                if (payInfoStatus == PayInfoStatus.FOLDED)
                {
                    player.PayInfo.UpdateToFold();
                }
                if (payInfoStatus == PayInfoStatus.ALLIN)
                {
                    player.PayInfo.UpdateToAllin();
                }
                
                var holeCards = JsonArrayToCards(playerObject[nameof(Player.HoleCards)].AsArray()).ToArray();
                if (holeCards.Any())
                {
                    player.AddHoleCards(holeCards);
                }

                var streetActionHistoriesObject = playerObject[nameof(Player.RoundActionHistories)].AsObject();
                foreach (KeyValuePair<string, JsonNode> node in streetActionHistoriesObject)
                {
                    var streetType = Enum.Parse<StreetType>(node.Key);
                    foreach (JsonObject historyEntryObject in node.Value.AsArray())
                    {
                        player.ActionHistories.Add(new ActionHistoryEntry
                        {
                            Uuid = player.Uuid,
                            ActionType = Enum.Parse<ActionType>(historyEntryObject[nameof(ActionHistoryEntry.ActionType)].GetValue<string>()),
                            Amount = historyEntryObject[nameof(ActionHistoryEntry.Amount)].GetValue<float>(),
                            AddAmount = historyEntryObject[nameof(ActionHistoryEntry.AddAmount)].GetValue<float>(),
                            Paid = historyEntryObject[nameof(ActionHistoryEntry.Paid)].GetValue<float>()
                        });
                    }
                    player.SaveStreetActionHistories(streetType);
                }

                foreach (JsonObject historyEntryObject in playerObject[nameof(Player.ActionHistories)].AsArray())
                {
                    player.ActionHistories.Add(new ActionHistoryEntry
                    {
                        Uuid = player.Uuid,
                        ActionType = Enum.Parse<ActionType>(historyEntryObject[nameof(ActionHistoryEntry.ActionType)].GetValue<string>()),
                        Amount = historyEntryObject[nameof(ActionHistoryEntry.Amount)].GetValue<float>(),
                        AddAmount = historyEntryObject[nameof(ActionHistoryEntry.AddAmount)].GetValue<float>(),
                        Paid = historyEntryObject[nameof(ActionHistoryEntry.Paid)].GetValue<float>()
                    });
                }

                gameState.Table.Seats.Sitdown(player);
            }

            return gameState;
        }

        public override void Write(Utf8JsonWriter writer, GameState value, JsonSerializerOptions options)
        {
            var gameStateObject = new JsonObject();
            gameStateObject[nameof(GameState.RoundCount)] = value.RoundCount;
            gameStateObject[nameof(GameState.SmallBlindAmount)] = value.SmallBlindAmount;
            gameStateObject[nameof(GameState.Street)] = value.Street.ToString();
            gameStateObject[nameof(GameState.NextPlayerIx)] = value.NextPlayerIx;

            var tableObject = new JsonObject();
            tableObject[nameof(Table.BigBlindPosition)] = value.Table.BigBlindPosition;
            tableObject[nameof(Table.SmallBlindPosition)] = value.Table.SmallBlindPosition;
            tableObject[nameof(Table.DealerButton)] = value.Table.DealerButton;
            tableObject[nameof(Table.CommunityCards)] = CardsToJsonArray(value.Table.CommunityCards);

            var seatsArray = new JsonArray();
            tableObject[nameof(Table.Seats)] = seatsArray;
            if (value.Table.Seats?.Players != null)
            {
                foreach (var player in value.Table.Seats.Players)
                {
                    var playerObject = new JsonObject();
                    playerObject[nameof(Player.Name)] = player.Name;
                    playerObject[nameof(Player.Uuid)] = player.Uuid;
                    playerObject[nameof(Player.Stack)] = player.Stack;
                    playerObject[nameof(Player.HoleCards)] = CardsToJsonArray(player.HoleCards);
                    playerObject[nameof(Player.PayInfo)] = new JsonObject(new Dictionary<string, JsonNode>() 
                    { 
                        { nameof(PayInfo.Amount), player.PayInfo.Amount },
                        { nameof(PayInfo.Status), player.PayInfo.Status.ToString() }
                    });
                    playerObject[nameof(Player.ActionHistories)] = ActionHistoriesToJsonArray(player.ActionHistories);
                    playerObject[nameof(Player.RoundActionHistories)] = new JsonObject(player.RoundActionHistories.ToDictionary(k => k.Key.ToString(), v => (JsonNode)ActionHistoriesToJsonArray(v.Value)));
                    seatsArray.Add(playerObject);
                }
            }
            gameStateObject[nameof(GameState.Table)] = tableObject;

            gameStateObject.WriteTo(writer, options);
        }

        private static JsonArray CardsToJsonArray(IEnumerable<Card> cards)
        {
            var cardsArray = new JsonArray();
            if (cards != null)
            {
                foreach (var card in cards)
                {
                    cardsArray.Add(card.ToString());
                }
            }

            return cardsArray;
        }

        private static IEnumerable<Card> JsonArrayToCards(JsonArray array)
            => array.Select(x => Card.FromString(x.GetValue<string>()));

        private static JsonArray ActionHistoriesToJsonArray(IEnumerable<ActionHistoryEntry> actionHistoryEntries)
        {
            var actionHistoryEntriesArray = new JsonArray();
            if (actionHistoryEntries != null)
            {
                foreach (var actionHistoryEntry in actionHistoryEntries)
                {
                    actionHistoryEntriesArray.Add(new JsonObject(new Dictionary<string, JsonNode>()
                    {
                        { nameof(ActionHistoryEntry.ActionType), actionHistoryEntry.ActionType.ToString() },
                        { nameof(ActionHistoryEntry.Amount), actionHistoryEntry.Amount },
                        { nameof(ActionHistoryEntry.AddAmount), actionHistoryEntry.AddAmount },
                        { nameof(ActionHistoryEntry.Paid), actionHistoryEntry.Paid }
                    }));
                }
            }

            return actionHistoryEntriesArray;
        }
    }
}
