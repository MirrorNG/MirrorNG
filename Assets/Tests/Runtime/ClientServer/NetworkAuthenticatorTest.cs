using System;
using NSubstitute;
using NUnit.Framework;

namespace Mirage.Tests.ClientServer
{
    [TestFixture]
    public class NetworkAuthenticatorTest : ClientServerSetup<MockComponent>
    {
        NetworkAuthenticator serverAuthenticator;
        NetworkAuthenticator clientAuthenticator;

        Action<INetworkPlayer> serverMockMethod;
        Action<INetworkPlayer> clientMockMethod;


        class NetworkAuthenticationImpl : NetworkAuthenticator { };

        public override void ExtraSetup()
        {
            serverAuthenticator = serverGo.AddComponent<NetworkAuthenticationImpl>();
            clientAuthenticator = clientGo.AddComponent<NetworkAuthenticationImpl>();
            server.authenticator = serverAuthenticator;
            client.authenticator = clientAuthenticator;

            serverMockMethod = Substitute.For<Action<INetworkPlayer>>();
            serverAuthenticator.OnServerAuthenticated += serverMockMethod;

            clientMockMethod = Substitute.For<Action<INetworkPlayer>>();
            clientAuthenticator.OnClientAuthenticated += clientMockMethod;
        }

        [Test]
        public void OnServerAuthenticateTest()
        {
            serverAuthenticator.OnServerAuthenticate(Substitute.For<INetworkPlayer>());

            serverMockMethod.Received().Invoke(Arg.Any<INetworkPlayer>());
        }

        [Test]
        public void OnServerAuthenticateInternalTest()
        {
            serverAuthenticator.OnServerAuthenticateInternal(Substitute.For<INetworkPlayer>());

            serverMockMethod.Received().Invoke(Arg.Any<INetworkPlayer>());
        }

        [Test]
        public void OnClientAuthenticateTest()
        {
            clientAuthenticator.OnClientAuthenticate(Substitute.For<INetworkPlayer>());

            clientMockMethod.Received().Invoke(Arg.Any<INetworkPlayer>());
        }

        [Test]
        public void OnClientAuthenticateInternalTest()
        {
            clientAuthenticator.OnClientAuthenticateInternal(Substitute.For<INetworkPlayer>());

            clientMockMethod.Received().Invoke(Arg.Any<INetworkPlayer>());
        }

        [Test]
        public void ClientOnValidateTest()
        {
            Assert.That(client.authenticator, Is.EqualTo(clientAuthenticator));
        }

        [Test]
        public void ServerOnValidateTest()
        {
            Assert.That(server.authenticator, Is.EqualTo(serverAuthenticator));
        }

        [Test]
        public void NetworkClientCallsAuthenticator()
        {
            clientMockMethod.Received().Invoke(Arg.Any<INetworkPlayer>());
        }

        [Test]
        public void NetworkServerCallsAuthenticator()
        {
            clientMockMethod.Received().Invoke(Arg.Any<INetworkPlayer>());
        }
    }
}
