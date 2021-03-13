using Mirage;

namespace ServerRpcTests.ServerRpcThatIgnoresAuthorityWithSenderConnection
{
    class ServerRpcThatIgnoresAuthorityWithSenderConnection : NetworkBehaviour
    {
        [ServerRpc(requireAuthority = false)]
        void CmdFunction(INetworkPlayer connection = null)
        {
            // do something
        }
    }
}
