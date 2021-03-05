using NUnit.Framework;

namespace Mirage
{
    public class CustomAttrributeDefaultValueTests
    {
        [Test]
        public void SyncVarHookDefaultsToEmpty()
        {
            var attrib = new SyncVarAttribute();

            Assert.That(string.IsNullOrEmpty(attrib.hook));
        }

        [Test]
        public void ServerRpcDefaultsToReliable()
        {
            var attrib = new ServerRpcAttribute();

            Assert.That(attrib.channel == Channel.Reliable);
        }

        [Test]
        public void ClientRPCDefaultsToReliable()
        {
            var attrib = new ClientRpcAttribute();

            Assert.That(attrib.channel == Channel.Reliable);
        }

        [Test]
        public void ServerDefaultsToError()
        {
            var attrib = new ServerAttribute();

            Assert.IsTrue(attrib.error);
        }

        [Test]
        public void ClientDefaultsToError()
        {
            var attrib = new ClientAttribute();

            Assert.IsTrue(attrib.error);
        }

        [Test]
        public void HasAuthorityDefaultsToError()
        {
            var attrib = new HasAuthorityAttribute();

            Assert.IsTrue(attrib.error);
        }

        [Test]
        public void LocalPlayerDefaultsToError()
        {
            var attrib = new LocalPlayerAttribute();

            Assert.IsTrue(attrib.error);
        }
    }
}
