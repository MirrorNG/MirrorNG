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

        bool AddPlayerForConnection(INetworkConnection conn, GameObject player);

        bool AddPlayerForConnection(INetworkConnection conn, GameObject player, Guid assetId);

        bool ReplacePlayerForConnection(INetworkConnection conn, NetworkClient client, GameObject player, bool keepAuthority = false);

        bool ReplacePlayerForConnection(INetworkConnection conn, NetworkClient client, GameObject player, Guid assetId, bool keepAuthority = false);

        void Spawn(GameObject obj, GameObject ownerPlayer);

        void Spawn(GameObject obj, INetworkConnection ownerConnection = null);

        void Spawn(GameObject obj, Guid assetId, INetworkConnection ownerConnection = null);

        void Destroy(GameObject obj);

        void UnSpawn(GameObject obj);

        void SpawnObjects();
    }
}
