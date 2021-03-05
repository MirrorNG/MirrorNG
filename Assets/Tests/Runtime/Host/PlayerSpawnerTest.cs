using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Host
{
    public class PlayerSpawnerTest : HostSetup<MockComponent>
    {
        AssetBundle bundle;
        GameObject player;
        PlayerSpawner spawner;

        public override void ExtraSetup()
        {
            bundle = AssetBundle.LoadFromFile("Assets/Tests/Runtime/TestScene/testscene");

            spawner = networkManagerGo.AddComponent<PlayerSpawner>();

            spawner.Client = client;
            spawner.Server = server;
            spawner.SceneManager = sceneManager;
            spawner.ClientObjectManager = clientObjectManager;
            spawner.ServerObjectManager = serverObjectManager;

            player = new GameObject();
            NetworkIdentity identity = player.AddComponent<NetworkIdentity>();
            spawner.PlayerPrefab = identity;

            spawner.AutoSpawn = false;

            spawner.Start();
        }

        public override void ExtraTearDown()
        {
            bundle.Unload(true);
            Object.Destroy(player);
        }

        [UnityTest]
        public IEnumerator DontAutoSpawnTest() => UniTask.ToCoroutine(async () =>
        {
            bool invokeAddPlayerMessage = false;
            server.LocalConnection.RegisterHandler<AddPlayerMessage>(msg => invokeAddPlayerMessage = true);

            sceneManager.ChangeServerScene("Assets/Mirror/Tests/Runtime/testScene.unity");
            // wait for messages to be processed
            await UniTask.Yield();

            Assert.That(invokeAddPlayerMessage, Is.False);

        });

        [Test]
        public void ManualSpawnTest()
        {
            bool invokeAddPlayerMessage = false;
            server.LocalConnection.RegisterHandler<AddPlayerMessage>(msg => invokeAddPlayerMessage = true);

            spawner.RequestServerSpawnPlayer();

            Assert.That(invokeAddPlayerMessage, Is.False);
        }
    }
}
