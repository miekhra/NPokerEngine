using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using NPokerEngine.Types;

namespace NPokerEngine.Engine
{
    public class DataEncoder
    {
        public string PAY_INFO_PAY_TILL_END_STR = "participating";
        public string PAY_INFO_ALLIN_STR = "allin";
        public string PAY_INFO_FOLDED_STR = "folded";

        private static DataEncoder _instance;
        public static DataEncoder Instance
        {
            get
            {
                _instance = _instance ?? new DataEncoder();
                return _instance;
            }
        }

        private DataEncoder() { }

        public Dictionary<string, object> EncodePlayer(Player player, bool holecards = false)
        {
            var hash = new Dictionary<string, object> 
            {
                { "name", player.Name },
                { "uuid", player.Uuid },
                { "stack", player.Stack },
                { "state", player.PayInfo.Status switch 
                    {
                        PayInfoStatus.PAY_TILL_END => PAY_INFO_PAY_TILL_END_STR,
                        PayInfoStatus.ALLIN => PAY_INFO_ALLIN_STR,
                        PayInfoStatus.FOLDED => PAY_INFO_FOLDED_STR,
                        _ => string.Empty
                    }
                }
            };

            if (holecards)
            {
                hash["hole_card"] = player.HoleCards.Select(t => t.ToString()).ToList();
            }
                
            return hash;
        }

        public Dictionary<string, object> EncodeSeats(Seats seats)
        {
            return new Dictionary<string, object> {
                    {
                        "seats",
                        (from player in seats.Players
                            select this.EncodePlayer(player)).ToList()}};
        }

        public virtual Dictionary<string, object> EncodePot(IEnumerable<Player> players)
        {
            var pots = GameEvaluator.Instance.CreatePot(players);
            var main = new Dictionary<string, object> 
            {
                {
                    "amount",
                    pots[0].Amount
                }
            };
            Func<PotInfo, Dictionary<string, object>> genHsh = sidepot => new Dictionary<string, object> {
                    {
                        "amount",
                        sidepot.Amount},
                    {
                        "eligibles",
                        (from p in ((IEnumerable<Player>)sidepot.Eligibles)
                            select p.Uuid).ToList()
                    }};

            //var sidepots = pots.Count > 1 ? pots.Skip(1)

            var side = pots.Count > 1 ? 
                        (from sidepot in pots.Skip(1) select genHsh(sidepot)).ToList() :
                        new List<Dictionary<string, object>>();
            return new Dictionary<string, object> {
                    {
                        "main",
                        main},
                    {
                        "side",
                        side}};
        }

        public Dictionary<string, object> EncodeGameInformation(IDictionary config, Seats seats)
        {
            var hsh = new Dictionary<string, object> {
                    {
                        "player_num",
                        seats.Players.Count},
                    {
                        "rule",
                        new Dictionary<object, object> {
                            {
                                "initial_stack",
                                config["initial_stack"]},
                            {
                                "max_round",
                                config["max_round"]},
                            {
                                "small_blind_amount",
                                config["small_blind_amount"]},
                            {
                                "ante",
                                config["ante"]},
                            {
                                "blind_structure",
                                config["blind_structure"]}}}};
            foreach (var kvp in this.EncodeSeats(seats))
            {
                hsh[kvp.Key] = kvp.Value;
			}
            return hsh;
        }

        public virtual Dictionary<string, object> EncodeValidActions(int callAmount, int minBetAmount, int maxBetAmount)
        {
            return new Dictionary<string, object> {
                    {
                        "valid_actions",
                        new List<Dictionary<string, object>> {
                            new Dictionary<string, object> {
                                {
                                    "action",
                                    "fold"},
                                {
                                    "amount",
                                    0}},
                            new Dictionary<string, object> {
                                {
                                    "action",
                                    "call"},
                                {
                                    "amount",
                                    callAmount}},
                            new Dictionary<string, object> {
                                {
                                    "action",
                                    "raise"},
                                {
                                    "amount",
                                    new Dictionary<string, object> {
                                        {
                                            "min",
                                            minBetAmount},
                                        {
                                            "max",
                                            maxBetAmount}}}}
                        }}};
        }

        public Dictionary<string, object> EncodeAction(Player player, object action, object amount)
        {
            return new Dictionary<string, object>
            {
                {
                    "player_uuid",
                    player.Uuid
                },
                {
                    "action",
                    action},
                {
                    "amount",
                    amount
                }
            };
        }

        public Dictionary<string, object> EncodeStreet(byte street)
        {
            return new Dictionary<string, object> {
                    {
                        "street",
                        this.StreetToStr(street)}};
        }

        public Dictionary<string, object> EncodeActionHistories(Table table)
        {
            //var street_name = new List<string> {
            //                 "preflop",
            //                 "flop",
            //                 "turn",
            //                 "river"
            //             };

            var allStreetHistories = new List<List<List<ActionHistoryEntry>>>();
            foreach (var ix in Enumerable.Range(0, 4))
            {
                allStreetHistories.Add(new List<List<ActionHistoryEntry>>());
                var streetType = (StreetType)Enum.ToObject(typeof(StreetType), (byte)ix);
                foreach (var player in table.Seats.Players)
                {
                    if (!player.RoundActionHistories.ContainsKey(streetType)) continue;
                    allStreetHistories[ix].Add(player.RoundActionHistories[streetType]);
                }
                //var t = table.Seats.Players.Select(p => p.RoundActionHistories.ContainsKey(streetType) ? p.RoundActionHistories[streetType] : new List<Dictionary<string, object>>()).Cast<Dictionary<string, object>>().ToList();
            }

            object start_positions;
            var all_street_histories = allStreetHistories;
            //foreach (var allStreetHistory in all_street_histories)
            //{
            //    foreach (var sh in allStreetHistory)
            //    {

            //    }
            //}
            //var all_street_histories = (from street in Enumerable.Range(0, 4)
            //                            select (from player in table.Seats.Players
            //                                    select player.RoundActionHistories.ContainsKey((StreetType)Enum.ToObject(typeof(StreetType), (byte)street)) ? player.RoundActionHistories[(StreetType)Enum.ToObject(typeof(StreetType), (byte)street)] : new List<Dictionary<string, object>>()).ToList()).ToList();
            //var past_street_histories = (from histories in all_street_histories
            //                             where ((from e in histories
            //                                     select (e != null)).Any())
            //                             select histories).ToList();
            var past_street_histories = all_street_histories.Where(t => t.Count > 0).ToList();
            var current_street_histories = (from player in table.Seats.Players
                                            select player.ActionHistories).ToList();
            //var street_histories = past_street_histories + new List<object> {
            //        current_street_histories
            //    };

            past_street_histories.Add(current_street_histories);
            var street_histories = past_street_histories;

            //foreach (var sh in street_histories)
            //{
            //    foreach (var hst in sh)
            //    {
            //        var ordered = OrderHistories(table.SmallBlindPosition.Value, sh);
            //    }
                
            //}

            

            //if (table.Seats.Players.Count == 2)
            //{
            //    // in Heads-Up after PreFlop, the BigBlind is the first to make an action
            //    //start_positions = new List<object> {
            //    //        table.SmallBlindPosition
            //    //    } + new List<object> {
            //    //        table.BigBlindPosition
            //    //    } * 3;
            //    start_positions = new List<int?>
            //             {
            //                 table.SmallBlindPosition,
            //                 table.BigBlindPosition,
            //                 table.BigBlindPosition,
            //                 table.BigBlindPosition
            //             };

            //}
            //else
            //{
            //    start_positions = new List<int?>
            //    {
            //        table.SmallBlindPosition,
            //        table.SmallBlindPosition,
            //        table.SmallBlindPosition,
            //        table.SmallBlindPosition
            //    };
            //}
            //street_histories = (from _tup_1 in zip(street_histories, start_positions).Chop((histories, pos) => (histories, pos))
            //                    let histories = _tup_1.Item1
            //                    let pos = _tup_1.Item2
            //                    select this.OrderHistories(pos, histories)).ToList();
            var street_name = new List<string> {
                             "preflop",
                             "flop",
                             "turn",
                             "river"
                         };

            var ah = new Dictionary<string, List<ActionHistoryEntry>>();
            for (int ix = 0; ix < street_name.Count; ix++)
            {
                if (street_histories.Count < ix+1) continue;
                ah[street_name[ix]] = OrderHistories(table.SmallBlindPosition.Value, street_histories[ix]); //street_histories[ix].SelectMany(t => t).ToList();
            }


            //var action_histories = street_histories.Zip(street_name, (t1, t2) => (t1, t2)).ToDictionary(k => k.t2.ToString(), v => v.t2);//  zip(street_name, street_histories).ToDictionary(_tup_2 => _tup_2.Item1, _tup_2 => _tup_2.Item2);
            return new Dictionary<string, object> {
                             {
                                 "action_histories",
                                 ah/*action_histories*/}};
        }

        public Dictionary<string, object> EncodeWinners(IEnumerable<Player> winners)
        {
            return new Dictionary<string, object> 
            {
                {
                    "winners",
                    this.EncodePlayers(winners)
                }
            };
        }

        public Dictionary<string, object> EncodeRoundState(Dictionary<string, object> state)
        {
            var hsh = new Dictionary<string, object> {
                    {
                        "street",
                        this.StreetToStr(Convert.ToByte(state["street"]))},
                    {
                        "pot",
                        this.EncodePot(((Table)state["table"]).Seats.Players)},
                    {
                        "community_card",
                        (from card in ((Table)state["table"]).CommunityCards
                            select card.ToString()).ToList()},
                    {
                        "dealer_btn",
                        ((Table)state["table"]).DealerButton},
                    {
                        "next_player",
                        state["next_player"]},
                    {
                        "small_blind_pos",
                        ((Table)state["table"]).SmallBlindPosition},
                    {
                        "big_blind_pos",
                        ((Table)state["table"]).BigBlindPosition},
                    {
                        "round_count",
                        state["round_count"]},
                    {
                        "small_blind_amount",
                        state["small_blind_amount"]}};
            foreach (var kvp in this.EncodeSeats(((Table)state["table"]).Seats))
            {
                hsh[kvp.Key] = kvp.Value;
			}
            //hsh.update(this.encode_seats(state["table"].seats));
            foreach (var kvp in this.EncodeActionHistories((Table)state["table"]))
            {
				hsh[kvp.Key] = kvp.Value;
			}
            //hsh.update(this.EncodeActionHistories((Table)state["table"]));
            return hsh;
        }

        private string PayInfoToStr(PayInfoStatus status)
        {
            if (status == PayInfoStatus.PAY_TILL_END)
            {
                return PAY_INFO_PAY_TILL_END_STR;
            }
            if (status == PayInfoStatus.ALLIN)
            {
                return PAY_INFO_ALLIN_STR;
            }
            if (status == PayInfoStatus.FOLDED)
            {
                return PAY_INFO_FOLDED_STR;
            }
            return string.Empty;
        }

        private string StreetToStr(byte street)
        {
            if (street == (byte)StreetType.PREFLOP)
            {
                return "preflop";
            }
            if (street == (byte)StreetType.FLOP)
            {
                return "flop";
            }
            if (street == (byte)StreetType.TURN)
            {
                return "turn";
            }
            if (street == (byte)StreetType.RIVER)
            {
                return "river";
            }
            if (street == (byte)StreetType.SHOWDOWN)
            {
                return "showdown";
            }
            return string.Empty;
        }

        private List<Dictionary<string, object>> EncodePlayers(IEnumerable<Player> players)
        {
            return (from player in players
                    select this.EncodePlayer(player)).ToList();
        }


        public List<ActionHistoryEntry> OrderHistories(int startPos, List<List<ActionHistoryEntry>> playerHistories)
        {
            List<List<ActionHistoryEntry>> orderedPlayerHistories = new List<List<ActionHistoryEntry>>();
            for (int ix = 0; ix < playerHistories.Count; ix++)
            {
                var current = new List<ActionHistoryEntry>();
                foreach (var item in (playerHistories[(startPos + ix) % playerHistories.Count]))
                {
                    current.Add(item);
                }
                orderedPlayerHistories.Add(current);
            }

            var allPlayerHistories = orderedPlayerHistories.AsEnumerable().ToList();

            var maxLen = allPlayerHistories.Select(x => x.Count).Max();
            foreach (var current in allPlayerHistories)
            {
                while (current.Count < maxLen) current.Add(null);
            }

            var orderedHistories = new List<ActionHistoryEntry>();
            for (int ix = 0; ix < maxLen; ix++)
            {
                foreach (var item in allPlayerHistories)
                {
                    if (item[ix] == null) continue;
                    orderedHistories.Add(item[ix]);
                }
            }
            return orderedHistories;
        }

   //     public List<Dictionary<int, object>> OrderHistories(int startPos, IList<Dictionary<int,object>> playerHistories)
   //     {
   //         var orderedPlayerHistories = (from i in Enumerable.Range(0, playerHistories.Count)
   //                                         select playerHistories[(startPos + i) % playerHistories.Count]).ToList();
   //         var allPlayerHistories = (from histories in orderedPlayerHistories
   //                                   select histories).ToList();
   //         var maxLen = (from h in orderedPlayerHistories
   //                           select h.Count).Max();
   //         var unifiedHistories = (from l in orderedPlayerHistories
   //                                  select this.UnifyLength(maxLen, l)).ToList();
   //         //unifiedHistories.Zip(new Dictionary<int, object>());
   //         //var orderedHistories = reduce((acc, zp) => acc + zp.ToList(), zip(unified_histories), new List<object>());
   //         //unifiedHistories.Zip()
   //         var orderedHistories = unifiedHistories;

			//return (from history in orderedHistories
   //                 where !(history == null)
   //                 select history).ToList();
   //     }

        private Dictionary<int, object> UnifyLength(int maxLen, Dictionary<int, object> lst)
        {
            foreach (var _ in Enumerable.Range(0, maxLen - lst.Count))
            {
                lst.Add(int.MinValue, null);
            }
            return lst;
        }
    }
}
