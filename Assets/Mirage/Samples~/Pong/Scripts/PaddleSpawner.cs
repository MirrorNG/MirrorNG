using UnityEngine;

namespace Mirage.Examples.Pong
{
    public class PaddleSpawner : PlayerSpawner
    {
        public Transform leftRacketSpawn;
        public Transform rightRacketSpawn;
        public GameObject ballPrefab;

        GameObject ball;

        public override void OnServerAddPlayer(INetworkPlayer conn)
        {
            // add player at correct spawn position
            Transform start = Server.NumberOfPlayers == 0 ? leftRacketSpawn : rightRacketSpawn;
            NetworkIdentity player = Instantiate(PlayerPrefab, start.position, start.rotation);
            ServerObjectManager.AddPlayerForConnection(conn, player.gameObject);

            // spawn ball if two players
            if (Server.NumberOfPlayers == 2)
            {
                ball = Instantiate(ballPrefab);
                ServerObjectManager.Spawn(ball);
            }
        }


        public void OnServerDisconnect(INetworkPlayer conn)
        {
            // destroy ball
            if (ball != null)
                ServerObjectManager.Destroy(ball);
        }
    }
}
