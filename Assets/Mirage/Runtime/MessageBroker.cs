using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Mirage.Logging;
using Mirage.Serialization;
using UnityEngine;

namespace Mirage
{
    /// <summary>
    /// Sends and handles messages
    /// </summary>
    public class MessageBroker : IMessageSender, IMessageReceiver, INotifySender, INotifyReceiver
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(MessageBroker));

        // Handles network messages on client and server
        internal delegate void NetworkMessageDelegate(INetworkPlayer player, NetworkReader reader, int channelId);

        // message handlers for this connection
        internal readonly Dictionary<int, NetworkMessageDelegate> messageHandlers = new Dictionary<int, NetworkMessageDelegate>();

        /// <summary>
        /// Transport level connection
        /// </summary>
        /// <remarks>
        /// <para>On a server, this Id is unique for every connection on the server. On a client this Id is local to the client, it is not the same as the Id on the server for this connection.</para>
        /// <para>Transport layers connections begin at one. So on a client with a single connection to a server, the connectionId of that connection will be one. In NetworkServer, the connectionId of the local connection is zero.</para>
        /// <para>Clients do not know their connectionId on the server, and do not know the connectionId of other clients on the server.</para>
        /// </remarks>
        public IConnection Connection { get; }

        public MessageBroker(IConnection connection)
        {
            Connection = connection;
            lastNotifySentTime = Time.unscaledTime;


            // a black message to ensure a notify timeout
            RegisterHandler<NotifyAck>(msg => { });
        }

        #region Receive

        /// <summary>
        /// Register a handler for a particular message type.
        /// <para>There are several system message types which you can add handlers for. You can also add your own message types.</para>
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="handler">Function handler which will be invoked for when this message type is received.</param>
        /// <param name="requireAuthentication">True if the message requires an authenticated connection</param>
        public void RegisterHandler<T>(Action<INetworkPlayer, T> handler)
        {
            int msgType = MessagePacker.GetId<T>();
            if (logger.filterLogType == LogType.Log && messageHandlers.ContainsKey(msgType))
            {
                logger.Log("NetworkServer.RegisterHandler replacing " + msgType);
            }
            messageHandlers[msgType] = MessageHandler(handler);
        }

        /// <summary>
        /// Register a handler for a particular message type.
        /// <para>There are several system message types which you can add handlers for. You can also add your own message types.</para>
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="handler">Function handler which will be invoked for when this message type is received.</param>
        /// <param name="requireAuthentication">True if the message requires an authenticated connection</param>
        public void RegisterHandler<T>(Action<T> handler)
        {
            RegisterHandler<T>((_, value) => { handler(value); });
        }

        /// <summary>
        /// Unregisters a handler for a particular message type.
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        public void UnregisterHandler<T>()
        {
            int msgType = MessagePacker.GetId<T>();
            messageHandlers.Remove(msgType);
        }

        /// <summary>
        /// Clear all registered callback handlers.
        /// </summary>
        public void ClearHandlers()
        {
            messageHandlers.Clear();
        }


        private static NetworkMessageDelegate MessageHandler<T>(Action<INetworkPlayer, T> handler)
        {
            void AdapterFunction(INetworkPlayer player, NetworkReader reader, int channelId)
            {
                // protect against DOS attacks if attackers try to send invalid
                // data packets to crash the server/client. there are a thousand
                // ways to cause an exception in data handling:
                // - invalid headers
                // - invalid message ids
                // - invalid data causing exceptions
                // - negative ReadBytesAndSize prefixes
                // - invalid utf8 strings
                // - etc.
                //
                // let's catch them all and then disconnect that connection to avoid
                // further attacks.
                var message = default(T);
                try
                {
                    message = reader.Read<T>();
                }
                finally
                {
                    NetworkDiagnostics.OnReceive(message, channelId, reader.Length);
                }

                handler(player, message);
            }
            return AdapterFunction;
        }


        internal void InvokeHandler(INetworkPlayer player, int msgType, NetworkReader reader, int channelId)
        {
            if (messageHandlers.TryGetValue(msgType, out NetworkMessageDelegate msgDelegate))
            {
                msgDelegate(player, reader, channelId);
            }
            else
            {
                try
                {
                    Type type = MessagePacker.GetMessageType(msgType);
                    throw new InvalidDataException($"Unexpected message {type} received in {this}. Did you register a handler for it?");
                }
                catch (KeyNotFoundException)
                {
                    throw new InvalidDataException($"Unexpected message ID {msgType} received in {this}. May be due to no existing RegisterHandler for this message.");
                }
            }
        }

        // note: original HLAPI HandleBytes function handled >1 message in a while loop, but this wasn't necessary
        //       anymore because NetworkServer/NetworkClient Update both use while loops to handle >1 data events per
        //       frame already.
        //       -> in other words, we always receive 1 message per Receive call, never two.
        //       -> can be tested easily with a 1000ms send delay and then logging amount received in while loops here
        //          and in NetworkServer/Client Update. HandleBytes already takes exactly one.
        /// <summary>
        /// This function allows custom network connection classes to process data from the network before it is passed to the application.
        /// </summary>
        /// <param name="buffer">The data received.</param>
        internal void TransportReceive(INetworkPlayer player, ArraySegment<byte> buffer, int channelId)
        {
            // unpack message
            using (PooledNetworkReader networkReader = NetworkReaderPool.GetReader(buffer))
            {
                try
                {
                    int msgType = MessagePacker.UnpackId(networkReader);

                    if (msgType == MessagePacker.GetId<NotifyPacket>())
                    {
                        // this is a notify message, send to the notify receive
                        NotifyPacket notifyPacket = networkReader.ReadNotifyPacket();
                        ReceiveNotify(player, notifyPacket, networkReader, channelId);
                    }
                    else
                    {
                        // try to invoke the handler for that message
                        InvokeHandler(player, msgType, networkReader, channelId);
                    }
                }
                catch (InvalidDataException ex)
                {
                    logger.Log(ex.ToString());
                }
                catch (Exception ex)
                {
                    logger.LogError("Closed connection: " + this + ". Invalid message " + ex);
                    Connection?.Disconnect();
                }
            }
        }

        public async UniTask ProcessMessagesAsync(INetworkPlayer player)
        {
            var buffer = new MemoryStream();

            logger.Assert(player.Connection != null, "");
            try
            {
                while (true)
                {
                    int channel = await player.Connection.ReceiveAsync(buffer);

                    buffer.TryGetBuffer(out ArraySegment<byte> data);
                    TransportReceive(player, data, channel);
                }
            }
            catch (EndOfStreamException)
            {
                // connection closed,  normal
            }
        }

        #endregion

        #region Send

        /// <summary>
        /// This sends a network message to the connection.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="msg">The message to send.</param>
        /// <param name="channelId">The transport layer channel to send on.</param>
        /// <returns></returns>
        public virtual void Send<T>(INetworkPlayer player, T message, int channelId = Channel.Reliable)
        {
            if (player.Connection == null)
            {
                // todo invoke locally when sending to player without connecton
                logger.LogError("can't send to player without connection");
                return;
            }

            using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
            {
                // pack message and send allocation free
                MessagePacker.Pack(message, writer);
                NetworkDiagnostics.OnSend(message, channelId, writer.Length, 1);
                Send(player, writer.ToArraySegment(), channelId);
            }
        }

        // internal because no one except Mirage should send bytes directly to
        // the client. they would be detected as a message. send messages instead.
        public void Send(INetworkPlayer player, ArraySegment<byte> segment, int channelId = Channel.Reliable)
        {
            if (player.Connection == null)
            {
                // todo invoke locally when sending to player without connecton
                logger.LogError("can't send to player without connection");
                return;
            }
            player.Connection.Send(segment, channelId);
        }

        #endregion

        #region Notify

        internal struct PacketEnvelope
        {
            internal ushort Sequence;
            internal object Token;
        }
        const int ACK_MASK_BITS = sizeof(ulong) * 8;
        const int WINDOW_SIZE = 512;
        // packages will be acked no longer than this time
        public float NOTIFY_ACK_TIMEOUT = 0.3f;

        private Sequencer sequencer = new Sequencer(16);
        readonly Queue<PacketEnvelope> sendWindow = new Queue<PacketEnvelope>(WINDOW_SIZE);
        private float lastNotifySentTime;

        private ushort receiveSequence;
        private ulong receiveMask;



        /// <summary>
        /// Sends a message, but notify when it is delivered or lost
        /// </summary>
        /// <typeparam name="T">type of message to send</typeparam>
        /// <param name="message">message to send</param>
        /// <param name="token">a arbitrary object that the sender will receive with their notification</param>
        public void SendNotify<T>(INetworkPlayer player, T message, object token, int channelId = Channel.Unreliable)
        {
            if (sendWindow.Count == WINDOW_SIZE)
            {
                NotifyLost?.Invoke(player, token);
                return;
            }

            using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
            {
                var notifyPacket = new NotifyPacket
                {
                    Sequence = (ushort)sequencer.Next(),
                    ReceiveSequence = receiveSequence,
                    AckMask = receiveMask
                };

                sendWindow.Enqueue(new PacketEnvelope
                {
                    Sequence = notifyPacket.Sequence,
                    Token = token
                });

                MessagePacker.Pack(notifyPacket, writer);
                MessagePacker.Pack(message, writer);
                NetworkDiagnostics.OnSend(message, channelId, writer.Length, 1);
                Send(player, writer.ToArraySegment(), channelId);
                lastNotifySentTime = Time.unscaledTime;
            }

        }

        internal void ReceiveNotify(INetworkPlayer player, NotifyPacket notifyPacket, NetworkReader networkReader, int channelId)
        {
            int sequenceDistance = (int)sequencer.Distance(notifyPacket.Sequence, receiveSequence);

            // sequence is so far out of bounds we can't save, just kick them
            if (Math.Abs(sequenceDistance) > WINDOW_SIZE)
            {
                player?.Connection?.Disconnect();
                return;
            }

            // this message is old,  we already received
            // a newer or duplicate packet.  Discard it
            if (sequenceDistance <= 0)
                return;

            receiveSequence = notifyPacket.Sequence;

            if (sequenceDistance >= ACK_MASK_BITS)
                receiveMask = 1;
            else
                receiveMask = (receiveMask << sequenceDistance) | 1;

            AckPackets(player, notifyPacket.ReceiveSequence, notifyPacket.AckMask);

            int msgType = MessagePacker.UnpackId(networkReader);
            InvokeHandler(player, msgType, networkReader, channelId);

            if (Time.unscaledTime - lastNotifySentTime > NOTIFY_ACK_TIMEOUT)
            {
                SendNotify(player, new NotifyAck(), null, channelId);
            }
        }

        // the other end just sent us a message
        // and it told us the latest message it got
        // and the ack mask
        private void AckPackets(INetworkPlayer player, ushort receiveSequence, ulong ackMask)
        {
            while (sendWindow.Count > 0)
            {
                PacketEnvelope envelope = sendWindow.Peek();

                int distance = (int)sequencer.Distance(envelope.Sequence, receiveSequence);

                if (distance > 0)
                    break;

                sendWindow.Dequeue();

                // if any of these cases trigger, packet is most likely lost
                if ((distance <= -ACK_MASK_BITS) || ((ackMask & (1UL << -distance)) == 0UL))
                {
                    NotifyLost?.Invoke(player, envelope.Token);
                }
                else
                {
                    NotifyDelivered?.Invoke(player, envelope.Token);
                }
            }
        }

        /// <summary>
        /// Raised when a message is delivered
        /// </summary>
        public event Action<INetworkPlayer, object> NotifyDelivered;

        /// <summary>
        /// Raised when a message is lost
        /// </summary>
        public event Action<INetworkPlayer, object> NotifyLost;
        #endregion
    }
}
