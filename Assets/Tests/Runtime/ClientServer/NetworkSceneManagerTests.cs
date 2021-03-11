using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using InvalidEnumArgumentException = System.ComponentModel.InvalidEnumArgumentException;

namespace Mirage.Tests.ClientServer
{

    [TestFixture]
    public class NetworkSceneManagerNonHostTests : ClientServerSetup<MockComponent>
    {
        AssetBundle bundle;

        public override void ExtraSetup()
        {
            bundle = AssetBundle.LoadFromFile("Assets/Tests/Runtime/TestScene/testscene");
        }

        public override void ExtraTearDown()
        {
            bundle.Unload(true);
        }

        [Test]
        public void ClientSceneMessageExceptionTest()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                clientSceneManager.ClientSceneMessage(null, new SceneMessage());
            });
        }

        [Test]
        public void FinishLoadSceneTest()
        {
            UnityAction<string, SceneOperation> func2 = Substitute.For<UnityAction<string, SceneOperation>>();
            clientSceneManager.ClientSceneChanged.AddListener(func2);
            clientSceneManager.FinishLoadScene("Assets/Mirror/Tests/Runtime/testScene.unity", SceneOperation.Normal);

            func2.Received(1).Invoke(Arg.Any<string>(), Arg.Any<SceneOperation>());
        }

        [UnityTest]
        public IEnumerator ClientOfflineSceneException() => UniTask.ToCoroutine(async () =>
        {
            client.Disconnect();

            await AsyncUtil.WaitUntilWithTimeout(() => !client.Active);

            Assert.Throws<InvalidOperationException>(() =>
            {
                clientSceneManager.ClientSceneMessage(null, new SceneMessage());
            });
        });

        [UnityTest]
        public IEnumerator ClientSceneMessageInvokeTest() => UniTask.ToCoroutine(async () =>
        {
            int startInvoked = 0;
            int endInvoked = 0;

            clientSceneManager.ClientChangeScene.AddListener((_, __) => startInvoked++);
            clientSceneManager.ClientSceneChanged.AddListener((_, __) => endInvoked++);
            clientSceneManager.ClientSceneMessage(null, new SceneMessage { scenePath = "Assets/Mirror/Tests/Runtime/testScene.unity" });

            await AsyncUtil.WaitUntilWithTimeout(() => startInvoked == 1);

            // wait 1/2 a second to see if end invokes itself
            await UniTask.Delay(500);
            Assert.That(startInvoked == 1, "Start should only be called once");
            Assert.That(endInvoked == 0, "Should wait for ready before end is called");

            clientSceneManager.ClientSceneReadyMessage(connectionToServer, new SceneReadyMessage());

            await AsyncUtil.WaitUntilWithTimeout(() => endInvoked == 1);

            Assert.That(clientSceneManager.ActiveScenePath, Is.EqualTo("Assets/Mirror/Tests/Runtime/testScene.unity"));

            Assert.That(startInvoked == 1, "Start should only be called once");
            Assert.That(endInvoked == 1, "End should only be called once");
        });

        [Test]
        public void ClientSceneMessageThrowsIfInvalidSceneOperation()
        {
            int startInvoked = 0;
            int endInvoked = 0;

            clientSceneManager.ClientChangeScene.AddListener((_, __) => startInvoked++);
            clientSceneManager.ClientSceneChanged.AddListener((_, __) => endInvoked++);

            var invalidOperation = (SceneOperation)10;
            InvalidEnumArgumentException exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            {
                clientSceneManager.ClientSceneMessage(null, new SceneMessage
                {
                    scenePath = "Assets/Mirror/Tests/Runtime/testScene.unity",
                    sceneOperation = invalidOperation
                });
            });

            string message = new InvalidEnumArgumentException("sceneOperation", 10, typeof(SceneOperation)).Message;
            Assert.That(exception, Has.Message.EqualTo(message));
        }

        [Test]
        public void NetworkSceneNameStringValueTest()
        {
            Assert.That(clientSceneManager.ActiveScenePath.Equals(SceneManager.GetActiveScene().path));
        }

        [Test]
        public void ServerChangeSceneTest()
        {
            UnityAction<string, SceneOperation> func1 = Substitute.For<UnityAction<string, SceneOperation>>();
            serverSceneManager.ServerChangeScene.AddListener(func1);
            serverSceneManager.OnServerChangeScene("test", SceneOperation.Normal);
            func1.Received(1).Invoke(Arg.Any<string>(), Arg.Any<SceneOperation>());
        }

        [Test]
        public void ServerSceneChangedTest()
        {
            UnityAction<string, SceneOperation> func1 = Substitute.For<UnityAction<string, SceneOperation>>();
            serverSceneManager.ServerSceneChanged.AddListener(func1);
            serverSceneManager.OnServerSceneChanged("test", SceneOperation.Normal);
            func1.Received(1).Invoke(Arg.Any<string>(), Arg.Any<SceneOperation>());
        }

        [Test]
        public void OnClientSceneChangedAdditiveListTest()
        {
            UnityAction<string, SceneOperation> func1 = Substitute.For<UnityAction<string, SceneOperation>>();
            clientSceneManager.ClientSceneChanged.AddListener(func1);
            clientSceneManager.pendingAdditiveSceneList.Add("Assets/Mirror/Tests/Runtime/testScene.unity");

            clientSceneManager.OnClientSceneChanged(null, SceneOperation.Normal);

            func1.Received(1).Invoke(Arg.Any<string>(), Arg.Any<SceneOperation>());
            Assert.That(clientSceneManager.pendingAdditiveSceneList.Count == 0);
        }

        [Test]
        public void ClientSceneMessagePendingAdditiveSceneListTest()
        {
            //Check for the additive scene in the pending list at the time of ClientSceneChanged before its removed as part of it being loaded.
            clientSceneManager.ClientSceneChanged.AddListener(CheckForAdditiveScene);
            clientSceneManager.ClientSceneMessage(client.Player, new SceneMessage { scenePath = "Assets/Mirror/Tests/Runtime/testScene.unity", additiveScenes = new[] { "Assets/Mirror/Tests/Runtime/testScene.unity" } });

            Assert.That(additiveSceneWasFound);
        }

        bool additiveSceneWasFound;
        void CheckForAdditiveScene(string scenePath, SceneOperation sceneOperation)
        {
            if (clientSceneManager.pendingAdditiveSceneList.Contains("Assets/Mirror/Tests/Runtime/testScene.unity"))
            {
                additiveSceneWasFound = true;
            }
        }
    }
}
