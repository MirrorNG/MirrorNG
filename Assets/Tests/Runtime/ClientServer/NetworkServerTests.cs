using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Mirage.Tests.ClientServer
{
    public class NetworkServerTests : ClientServerSetup<MockComponent>
    {
        WovenTestMessage message;

        public override void ExtraSetup()
        {
            message = new WovenTestMessage
            {
                IntValue = 1,
                DoubleValue = 1.0,
                StringValue = "hello"
            };

        }

        [Test]
        public void InitializeTest()
        {
            Assert.That(server.Players, Has.Count.EqualTo(1));
            Assert.That(server.Active);
            Assert.That(server.LocalClientActive, Is.False);
        }

        [UnityTest]
        public IEnumerator ReadyMessageSetsClientReadyTest() => UniTask.ToCoroutine(async () =>
        {
            client.MessageHandler.Send(connectionToServer, new ReadyMessage());

            await AsyncUtil.WaitUntilWithTimeout(() => connectionToClient.IsReady);

            // ready?
            Assert.That(connectionToClient.IsReady, Is.True);
        });

        [UnityTest]
        public IEnumerator SendToAll() => UniTask.ToCoroutine(async () =>
        {
            bool invoked = false;

            client.MessageHandler.RegisterHandler<WovenTestMessage>(msg => invoked = true);

            server.SendToAll(message);

            client.MessageHandler.ProcessMessagesAsync(connectionToServer).Forget();

            await AsyncUtil.WaitUntilWithTimeout(() => invoked);
        });

        [UnityTest]
        public IEnumerator SendToClientOfPlayer() => UniTask.ToCoroutine(async () =>
        {
            bool invoked = false;

            client.MessageHandler.RegisterHandler<WovenTestMessage>(msg => invoked = true);

            serverIdentity.ConnectionToClient.Send(message);

            client.MessageHandler.ProcessMessagesAsync(connectionToServer).Forget();

            await AsyncUtil.WaitUntilWithTimeout(() => invoked);
        });

        [UnityTest]
        public IEnumerator RegisterMessage1() => UniTask.ToCoroutine(async () =>
        {
            bool invoked = false;

            server.MessageHandler.RegisterHandler<WovenTestMessage>(msg => invoked = true);
            client.MessageHandler.Send(connectionToServer, message);

            await AsyncUtil.WaitUntilWithTimeout(() => invoked);

        });

        [UnityTest]
        public IEnumerator RegisterMessage2() => UniTask.ToCoroutine(async () =>
        {
            bool invoked = false;

            server.MessageHandler.RegisterHandler<WovenTestMessage>((conn, msg) => invoked = true);

            client.MessageHandler.Send(connectionToServer, message);

            await AsyncUtil.WaitUntilWithTimeout(() => invoked);
        });

        [UnityTest]
        public IEnumerator UnRegisterMessage1() => UniTask.ToCoroutine(async () =>
        {
            Action<WovenTestMessage> func = Substitute.For<Action<WovenTestMessage>>();

            server.MessageHandler.RegisterHandler(func);
            server.MessageHandler.UnregisterHandler<WovenTestMessage>();

            client.MessageHandler.Send(connectionToServer, message);

            await UniTask.Delay(1);

            func.Received(0).Invoke(
                Arg.Any<WovenTestMessage>());
        });

        [Test]
        public void NumPlayersTest()
        {
            Assert.That(server.NumberOfPlayers, Is.EqualTo(1));
        }

        [Test]
        public void GetNewConnectionTest()
        {
            Assert.That(server.GetNewPlayer(Substitute.For<IConnection>()), Is.Not.Null);
        }

        [Test]
        public void VariableTest()
        {
            Assert.That(server.MaxConnections, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator DisconnectStateTest() => UniTask.ToCoroutine(async () =>
        {
            server.Disconnect();

            await AsyncUtil.WaitUntilWithTimeout(() => !server.Active);
        });

        [UnityTest]
        public IEnumerator StoppedInvokeTest() => UniTask.ToCoroutine(async () =>

        {
            UnityAction func1 = Substitute.For<UnityAction>();
            server.Stopped.AddListener(func1);

            server.Disconnect();

            await AsyncUtil.WaitUntilWithTimeout(() => !server.Active);

            func1.Received(1).Invoke();
        });

        public IEnumerator ApplicationQuitTest() => UniTask.ToCoroutine(async () =>
        {
            UnityAction func1 = Substitute.For<UnityAction>();
            server.Stopped.AddListener(func1);

            await UniTask.Delay(1);

            Application.Quit();

            await AsyncUtil.WaitUntilWithTimeout(() => !server.Active);

            func1.Received(1).Invoke();
        });
    }
}
