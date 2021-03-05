using System;
using UnityEngine.Events;

namespace Mirage
{
    public delegate NetworkIdentity SpawnHandlerDelegate(SpawnMessage msg);

    // Handles requests to unspawn objects on the client
    public delegate void UnSpawnDelegate(NetworkIdentity spawned);

    [Serializable]
    public class SpawnEvent : UnityEvent<NetworkIdentity> { }

    public interface IClientObjectManager
    {
        /// <summary>
        /// Raised when the client spawns an object
        /// </summary>
        SpawnEvent Spawned { get; }

        /// <summary>
        /// Raised when the client unspawns an object
        /// </summary>
        SpawnEvent UnSpawned { get; }

        /// <summary>
        /// NetworkIdentity of the localPlayer
        /// </summary>
        NetworkIdentity LocalPlayer { get; }

        NetworkIdentity GetPrefab(Guid assetId);

        void RegisterPrefab(NetworkIdentity identity);

        void RegisterPrefab(NetworkIdentity identity, Guid newAssetId);

        void RegisterPrefab(NetworkIdentity identity, SpawnHandlerDelegate spawnHandler, UnSpawnDelegate unspawnHandler);

        void UnregisterPrefab(NetworkIdentity identity);

        void RegisterSpawnHandler(Guid assetId, SpawnHandlerDelegate spawnHandler, UnSpawnDelegate unspawnHandler);

        void UnregisterSpawnHandler(Guid assetId);

        void ClearSpawners();

        void DestroyAllClientObjects();

        void PrepareToSpawnSceneObjects();
    }
}
