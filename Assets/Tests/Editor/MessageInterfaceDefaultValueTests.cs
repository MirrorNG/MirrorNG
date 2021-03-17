using System;
using NSubstitute;
using NUnit.Framework;

namespace Mirage
{
    public class MessageInterfaceDefaultValueTests
    {
        [Test]
        public void SenderUsesDefaultChannelForMessage()
        {
            IMessageSender sender = Substitute.For<IMessageSender>();
            var msg = new NetworkPingMessage();
            sender.Send(default, msg);
            sender.Received(1).Send(Arg.Any<INetworkPlayer>(), msg, Channel.Reliable);
        }

        [Test]
        public void SenderUsesDefaultChannelForSegement()
        {
            IMessageSender sender = Substitute.For<IMessageSender>();
            var segment = new ArraySegment<byte>();
            sender.Send(default, segment);
            sender.Received(1).Send(Arg.Any<INetworkPlayer>(), segment, Channel.Reliable);
        }

        [Test]
        public void NotifySenderUsesDefaultChannelForSegement()
        {
            INotifySender sender = Substitute.For<INotifySender>();
            var msg = new NetworkPingMessage();
            object token = new object();
            sender.SendNotify(default, msg, token);
            sender.Received(1).SendNotify(Arg.Any<INetworkPlayer>(), msg, token, Channel.Unreliable);
        }
    }
}
