using UnityEngine;

namespace Mirage.SocketLayer
{
    /// <summary>
    /// Makes sure Peer updates early in frame
    /// </summary>
    public class PeerUpdater : MonoBehaviour
    {
        bool updatedThisFrame = false;

        public Peer peer;

        private void FixedUpdate()
        {
            if (peer == null) return;
            if (updatedThisFrame) return;
            updatedThisFrame = true;
            peer.Update();
        }
        private void Update()
        {
            if (peer == null) return;
            if (updatedThisFrame) return;
            updatedThisFrame = true;
            peer.Update();
        }
        private void LateUpdate()
        {
            updatedThisFrame = false;
        }
    }
}
