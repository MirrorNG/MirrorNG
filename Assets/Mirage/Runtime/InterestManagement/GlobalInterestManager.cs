using System.Collections.Generic;

namespace Mirage.InterestManagement
{
    /// <summary>
    /// A simple interest manager where all players see all objects
    /// </summary>
    public class GlobalInterestManager : InterestManager
    {
        public override void ForEach<T>(NetworkIdentity identity, T action)
        {
            // avoids allocations by looping directly on the HashSet
            // which avoids boxing the IEnumerator
            foreach (INetworkPlayer player in ServerObjectManager.Server.Players)
            {
                action.Run(player);
            }
        }

        public override IReadOnlyCollection<INetworkPlayer> Observers(NetworkIdentity identity)
        {
            return ServerObjectManager.Server.Players;
        }

        protected override void OnAuthenticated(INetworkPlayer player)
        {
            foreach (NetworkIdentity identity in ServerObjectManager.SpawnedObjects.Values)
            {
                ServerObjectManager.ShowForConnection(identity, player);
            }
        }

        protected override void OnSpawned(NetworkIdentity identity)
        {
            foreach (INetworkPlayer player in ServerObjectManager.Server.Players)
            {
                ServerObjectManager.ShowForConnection(identity, player);
            }
        }
    }
}