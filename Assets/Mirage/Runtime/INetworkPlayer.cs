using System;
using Cysharp.Threading.Tasks;

namespace Mirage
{
    /// <summary>
    /// An object that can send messages
    /// </summary>
    public interface IMessageSender
    {
        void Send<T>(T message, int channelId = Channel.Reliable);

        void Send(ArraySegment<byte> segment, int channelId = Channel.Reliable);
    }

    /// <summary>
    /// An object that can receive messages
    /// </summary>
    public interface IMessageReceiver
    {
        void RegisterHandler<T>(Action<INetworkPlayer, T> handler);

        void RegisterHandler<T>(Action<T> handler);

        void UnregisterHandler<T>();

        void ClearHandlers();

        /// <summary>
        /// ProcessMessages loop, should loop unitil object is closed
        /// </summary>
        /// <returns></returns>
        UniTask ProcessMessagesAsync();

        // todo remove channel
        void TransportReceive(ArraySegment<byte> data, int channel = default);
    }

    /// <summary>
    /// An object that can observe NetworkIdentities.
    /// this is useful for interest management
    /// </summary>
    public interface IVisibilityTracker
    {
        void AddToVisList(NetworkIdentity identity);
        void RemoveFromVisList(NetworkIdentity identity);
        void RemoveObservers();
    }

    /// <summary>
    /// An object that can own networked objects
    /// </summary>
    public interface IObjectOwner
    {
        NetworkIdentity Identity { get; set; }
        void RemoveOwnedObject(NetworkIdentity networkIdentity);
        void AddOwnedObject(NetworkIdentity networkIdentity);
        void DestroyOwnedObjects();
    }

    /// <summary>
    /// A connection to a remote endpoint.
    /// May be from the server to client or from client to server
    /// </summary>
    public interface INetworkPlayer : IVisibilityTracker, IObjectOwner
    {
        bool IsReady { get; set; }
        object AuthenticationData { get; set; }
    }
}
