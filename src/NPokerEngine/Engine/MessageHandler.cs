using NPokerEngine.Messages;
using NPokerEngine.Types;
using System;
using System.Collections.Generic;

namespace NPokerEngine.Engine
{
    internal class MessageHandler
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



        public Tuple<ActionType, int> ProcessMessage(IMessage msg)
        {
            var messageType = MessageBuilder.GetMessageType(msg);
            foreach (var receiver in algo_owner_map)
            {
                if (messageType == MessageBuilder.ASK && msg is AskMessage askMessage)
                {
                    if (object.Equals(askMessage.PlayerUuid, receiver.Key))
                        return ((BasePokerPlayer)receiver.Value).RespondToAsk(msg);
                    else
                        continue;
                }
                else if (msg is IPlayerMessage playerMessage)
                {
                    if (object.Equals(playerMessage.PlayerUuid, receiver.Key))
                        ((BasePokerPlayer)receiver.Value).ReceiveNotification(msg);
                    else
                        continue;
                }
                else if (messageType == MessageBuilder.NOTIFICATION)
                {
                    ((BasePokerPlayer)receiver.Value).ReceiveNotification(msg);

                }
                else
                {
                    throw new ArgumentException(String.Format("Received unexpected message which type is [%s]", msg.MessageType));
                }
            }

            return new Tuple<ActionType, int>(default, default);
        }
    }
}
