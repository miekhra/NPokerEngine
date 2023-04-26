using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Engine
{
    public class MessageHandler
    {
        private Dictionary<object, object> algo_owner_map;

        public MessageHandler()
        {
            this.algo_owner_map = new Dictionary<object, object>
            {
            };
        }

        public void RegisterAlgorithm(object uuid, object algorithm)
        {
            this.algo_owner_map[uuid] = algorithm;
        }

        public Tuple<ActionType, int> ProcessMessage(object address, IDictionary msg)
        {
            var receivers = this.FetchReceivers(address);
            foreach (BasePokerPlayer receiver in receivers)
            {
                if ((string)msg["type"] == "ask")
                {
                    return receiver.RespondToAsk((IDictionary)msg["message"]);
                }
                else if ((string)msg["type"] == "notification")
                {
                    receiver.ReceiveNotification((IDictionary)msg["message"]);
                    return new Tuple<ActionType, int>(default, default);
                }
                else
                {
                    throw new ArgumentException(String.Format("Received unexpected message which type is [%s]", msg["type"]));
                }
            }

            throw new NotImplementedException();
        }

        private ICollection FetchReceivers(object address)
        {
            if ((int)address == -1)
            {
                return this.algo_owner_map.Values;
            }
            else
            {
                if (!this.algo_owner_map.ContainsKey(address))
                {
                    throw new ArgumentException(String.Format("Received message its address [%s] is unknown", address));
                }
                return new List<object> {
                        this.algo_owner_map[address]
                    };
            }
        }
    }
}
