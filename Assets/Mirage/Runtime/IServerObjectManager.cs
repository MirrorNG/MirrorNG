using System;
using UnityEngine;

namespace Mirage
{
    public interface IServerObjectManager
    {

        /// <summary>
        /// Raised when the client spawns an object
        /// </summary>
        SpawnEvent Spawned { get; }

        /// <summary>
        /// Raised when the client unspawns an object
        /// </summary>
        SpawnEvent UnSpawned { get; }

        bool AddPlayerForConnection(INetworkPlayer conn, GameObject player);

        bool AddPlayerForConnection(INetworkPlayer conn, GameObject player, Guid assetId);

        bool ReplacePlayerForConnection(INetworkPlayer conn, NetworkClient client, GameObject player, bool keepAuthority = false);

        bool ReplacePlayerForConnection(INetworkPlayer conn, NetworkClient client, GameObject player, Guid assetId, bool keepAuthority = false);

        void Spawn(GameObject obj, GameObject ownerPlayer);

        void Spawn(GameObject obj, INetworkPlayer ownerConnection = null);

        void Spawn(GameObject obj, Guid assetId, INetworkPlayer ownerConnection = null);

        void Destroy(GameObject obj);

        void UnSpawn(GameObject obj);

        void SpawnObjects();
    }
}
