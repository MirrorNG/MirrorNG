using System;
using System.IO;
using Mirage.Serialization;
using NSubstitute;
using NUnit.Framework;

namespace Mirage.Tests
{
    public class MessageBrokerTest
    {
        private MessageBroker messageBroker;

        [SetUp]
        public void Setup()
        {
            messageBroker = new MessageBroker();
        }

        [Test]
        public void NoHandler()
        {
            int messageId = MessagePacker.GetId<SceneMessage>();
            var reader = new NetworkReader(new byte[] { 1, 2, 3, 4 });
            InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            {
                messageBroker.InvokeHandler(Arg.Any<INetworkPlayer>(), messageId, reader, 0);
            });

            Assert.That(exception.Message, Does.StartWith("Unexpected message Mirage.SceneMessage received"));
        }

        [Test]
        public void UnknownMessage()
        {
            _ = MessagePacker.GetId<SceneMessage>();
            var reader = new NetworkReader(new byte[] { 1, 2, 3, 4 });
            InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            {
                // some random id with no message
                messageBroker.InvokeHandler(Arg.Any<INetworkPlayer>(), 1234, reader, 0);
            });

            Assert.That(exception.Message, Does.StartWith("Unexpected message ID 1234 received"));
        }


    }
    public class MessageBrokerNofityTest
    {
        private MessageBroker messageBroker;

        private INetworkPlayer player;
        private IConnection connection;

        private SceneMessage data;
        private byte[] serializedMessage;

        private NotifyPacket lastSent;

        private Action<INetworkPlayer, object> delivered;

        private Action<INetworkPlayer, object> lost;

        private byte[] lastSerializedPacket;


        [SetUp]
        public void SetUp()
        {
            messageBroker = new MessageBroker();
            data = new SceneMessage();
            connection = Substitute.For<IConnection>();

            void ParsePacket(ArraySegment<byte> data)
            {
                var reader = new NetworkReader(data);
                _ = MessagePacker.UnpackId(reader);
                lastSent = reader.ReadNotifyPacket();

                lastSerializedPacket = new byte[data.Count];
                Array.Copy(data.Array, data.Offset, lastSerializedPacket, 0, data.Count);
            }

            connection.Send(
                Arg.Do<ArraySegment<byte>>(ParsePacket), Channel.Unreliable);

            player = new NetworkPlayer(connection, Substitute.For<IMessageHandler>());

            serializedMessage = MessagePacker.Pack(new ReadyMessage());
            //messageBroker.RegisterHandler<ReadyMessage>(message => { });

            delivered = Substitute.For<Action<INetworkPlayer, object>>();
            lost = Substitute.For<Action<INetworkPlayer, object>>();

            messageBroker.NotifyDelivered += delivered;
            messageBroker.NotifyLost += lost;
        }
        #region Notify


        [Test]
        public void SendsNotifyPacket()
        {
            messageBroker.SendNotify(Arg.Any<INetworkPlayer>(), data, 1);

            Assert.That(lastSent, Is.EqualTo(new NotifyPacket
            {
                Sequence = 1,
                ReceiveSequence = 0,
                AckMask = 0
            }));
        }

        [Test]
        public void SendsNotifyPacketWithSequence()
        {
            messageBroker.SendNotify(player, data, 1);
            Assert.That(lastSent, Is.EqualTo(new NotifyPacket
            {
                Sequence = 1,
                ReceiveSequence = 0,
                AckMask = 0
            }));

            messageBroker.SendNotify(player, data, 1);
            Assert.That(lastSent, Is.EqualTo(new NotifyPacket
            {
                Sequence = 2,
                ReceiveSequence = 0,
                AckMask = 0
            }));
            messageBroker.SendNotify(player, data, 1);
            Assert.That(lastSent, Is.EqualTo(new NotifyPacket
            {
                Sequence = 3,
                ReceiveSequence = 0,
                AckMask = 0
            }));
        }

        [Test]
        public void RaisePacketDelivered()
        {
            messageBroker.SendNotify(player, data, 1);
            messageBroker.SendNotify(player, data, 3);
            messageBroker.SendNotify(player, data, 5);

            delivered.DidNotReceiveWithAnyArgs().Invoke(default, default);

            var reply = new NotifyPacket
            {
                Sequence = 1,
                ReceiveSequence = 3,
                AckMask = 0b111
            };

            messageBroker.ReceiveNotify(player, reply, new NetworkReader(serializedMessage), Channel.Unreliable);

            Received.InOrder(() =>
            {
                delivered.Invoke(player, 1);
                delivered.Invoke(player, 3);
                delivered.Invoke(player, 5);
            });
        }

        [Test]
        public void RaisePacketNotDelivered()
        {
            messageBroker.SendNotify(player, data, 1);
            messageBroker.SendNotify(player, data, 3);
            messageBroker.SendNotify(player, data, 5);

            delivered.DidNotReceiveWithAnyArgs().Invoke(default, default);

            var reply = new NotifyPacket
            {
                Sequence = 1,
                ReceiveSequence = 3,
                AckMask = 0b001
            };

            messageBroker.ReceiveNotify(player, reply, new NetworkReader(serializedMessage), Channel.Unreliable);

            Received.InOrder(() =>
            {
                lost.Invoke(player, 1);
                lost.Invoke(player, 3);
                delivered.Invoke(player, 5);
            });
        }

        [Test]
        public void DropDuplicates()
        {
            messageBroker.SendNotify(player, data, 1);

            var reply = new NotifyPacket
            {
                Sequence = 1,
                ReceiveSequence = 1,
                AckMask = 0b001
            };

            messageBroker.ReceiveNotify(player, reply, new NetworkReader(serializedMessage), Channel.Unreliable);
            messageBroker.ReceiveNotify(player, reply, new NetworkReader(serializedMessage), Channel.Unreliable);

            delivered.Received(1).Invoke(player, 1);
        }


        [Test]
        public void LoseOldPackets()
        {
            for (int i = 1; i < 10; i++)
            {
                var packet = new NotifyPacket
                {
                    Sequence = (ushort)i,
                    ReceiveSequence = 100,
                    AckMask = ~0b0ul
                };
                messageBroker.ReceiveNotify(player, packet, new NetworkReader(serializedMessage), Channel.Unreliable);
            }

            var reply = new NotifyPacket
            {
                Sequence = 100,
                ReceiveSequence = 100,
                AckMask = ~0b0ul
            };
            messageBroker.ReceiveNotify(player, reply, new NetworkReader(serializedMessage), Channel.Unreliable);

            messageBroker.SendNotify(player, data, 1);

            Assert.That(lastSent, Is.EqualTo(new NotifyPacket
            {
                Sequence = 1,
                ReceiveSequence = 100,
                AckMask = 1
            }));
        }

        [Test]
        public void SendAndReceive()
        {
            messageBroker.SendNotify(player, data, 1);

            Action<SceneMessage> mockHandler = Substitute.For<Action<SceneMessage>>();
            messageBroker.RegisterHandler(mockHandler);

            messageBroker.TransportReceive(player, new ArraySegment<byte>(lastSerializedPacket), Channel.Unreliable);
            mockHandler.Received().Invoke(new SceneMessage());
        }

        [Test]
        public void NotAcknowledgedYet()
        {
            messageBroker.SendNotify(player, data, 1);
            messageBroker.SendNotify(player, data, 3);
            messageBroker.SendNotify(player, data, 5);

            var reply = new NotifyPacket
            {
                Sequence = 1,
                ReceiveSequence = 2,
                AckMask = 0b011
            };

            messageBroker.ReceiveNotify(player, reply, new NetworkReader(serializedMessage), Channel.Unreliable);

            delivered.DidNotReceive().Invoke(Arg.Any<INetworkPlayer>(), 5);

            reply = new NotifyPacket
            {
                Sequence = 2,
                ReceiveSequence = 3,
                AckMask = 0b111
            };

            messageBroker.ReceiveNotify(player, reply, new NetworkReader(serializedMessage), Channel.Unreliable);

            delivered.Received().Invoke(player, 5);
        }

        #endregion
    }
}
