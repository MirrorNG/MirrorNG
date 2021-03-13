using Mirage;

namespace NetworkBehaviourTests.NetworkBehaviourClientRpcParamNetworkConnectionNotFirst
{
    class NetworkBehaviourClientRpcParamNetworkConnectionNotFirst : NetworkBehaviour
    {
        [ClientRpc(target = Mirage.Client.Connection)]
        public void ClientRpcCantHaveParamOptional(int abc, INetworkPlayer monkeyCon) { }
    }
}
