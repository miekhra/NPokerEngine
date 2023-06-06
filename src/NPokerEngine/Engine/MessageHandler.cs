using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using NPokerEngine.Messages;
using NPokerEngine.Types;

namespace NPokerEngine.Engine
{
    public class MessageHandler
    {
        internal Dictionary<object, object> algo_owner_map;

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



        public Tuple<ActionType, int> ProcessMessage(object address, IMessage msg)
        {
            var receivers = this.FetchReceivers(address);
            var messageType = MessageBuilder.GetMessageType(msg);
            foreach (BasePokerPlayer receiver in receivers)
            {
                if (messageType == MessageBuilder.ASK)
                {
                    return receiver.RespondToAsk(msg);
                }
                else if (messageType == MessageBuilder.NOTIFICATION)
                {
                    receiver.ReceiveNotification(msg);
                    return new Tuple<ActionType, int>(default, default);
                }
                else
                {
                    throw new ArgumentException(String.Format("Received unexpected message which type is [%s]", msg.MessageType));
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
