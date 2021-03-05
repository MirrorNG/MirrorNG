using System.Collections;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirage.Tests.Host
{
    [TestFixture]
    public class NetworkClientTest : HostSetup<MockComponent>
    {
        [Test]
        public void IsConnectedTest()
        {
            Assert.That(client.IsConnected);
        }

        [Test]
        public void ConnectionTest()
        {
            Assert.That(client.Connection != null);
        }

        [Test]
        public void GetNewConnectionTest()
        {
            Assert.That(client.GetNewConnection(Substitute.For<IConnection>()), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ClientDisconnectTest() => UniTask.ToCoroutine(async () =>
        {
            client.Disconnect();

            await AsyncUtil.WaitUntilWithTimeout(() => client.connectState == ConnectState.Disconnected);
            await AsyncUtil.WaitUntilWithTimeout(() => !client.Active);
        });

        [Test]
        public void ConnectionClearHandlersTest()
        {
            var clientConn = client.Connection as NetworkConnection;

            Assert.That(clientConn.messageHandlers.Count > 0);

            clientConn.ClearHandlers();

            Assert.That(clientConn.messageHandlers.Count == 0);
        }

        [Test]
        public void IsLocalClientHostTest()
        {
            Assert.That(client.IsLocalClient, Is.True);
        }

        [UnityTest]
        public IEnumerator IsLocalClientShutdownTest() => UniTask.ToCoroutine(async () =>
        {
            client.Disconnect();

            await AsyncUtil.WaitUntilWithTimeout(() => !client.IsLocalClient);
        });

        [Test]
        public void ConnectedNotNullTest()
        {
            Assert.That(client.Connected, Is.Not.Null);
        }

        [Test]
        public void AuthenticatedNotNullTest()
        {
            Assert.That(client.Authenticated, Is.Not.Null);
        }

        [Test]
        public void DisconnectedNotNullTest()
        {
            Assert.That(client.Disconnected, Is.Not.Null);
        }

        [Test]
        public void TimeNotNullTest()
        {
            Assert.That(client.Time, Is.Not.Null);
        }
    }
}
