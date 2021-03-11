using Cysharp.Threading.Tasks;

namespace Mirage.Tests
{
    public class MockComponent : NetworkBehaviour
    {
        public int cmdArg1;
        public string cmdArg2;

        [ServerRpc]
        public void Test(int arg1, string arg2)
        {
            cmdArg1 = arg1;
            cmdArg2 = arg2;
        }

        public NetworkIdentity cmdNi;

        [ServerRpc]
        public void CmdNetworkIdentity(NetworkIdentity ni)
        {
            cmdNi = ni;
        }

        public int rpcResult;

        [ServerRpc]
        public UniTask<int> GetResult()
        {
            return UniTask.FromResult(rpcResult);
        }

        public int rpcArg1;
        public string rpcArg2;

        [ClientRpc]
        public void RpcTest(int arg1, string arg2)
        {
            rpcArg1 = arg1;
            rpcArg2 = arg2;
        }

        public int targetRpcArg1;
        public string targetRpcArg2;
        public INetworkPlayer targetRpcConn;

        [ClientRpc(target = Mirage.Client.Connection)]
        public void ClientConnRpcTest(INetworkPlayer conn, int arg1, string arg2)
        {
            targetRpcConn = conn;
            targetRpcArg1 = arg1;
            targetRpcArg2 = arg2;
        }

        public int rpcOwnerArg1;
        public string rpcOwnerArg2;

        [ClientRpc(target = Mirage.Client.Owner)]
        public void RpcOwnerTest(int arg1, string arg2)
        {
            rpcOwnerArg1 = arg1;
            rpcOwnerArg2 = arg2;
        }
    }
}
