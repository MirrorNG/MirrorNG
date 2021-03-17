using System;
using Cysharp.Threading.Tasks;

namespace Mirage
{
    /// <summary>
    /// An object that can send messages
    /// </summary>
    public interface IMessageSender
    {
        void Send<T>(INetworkPlayer player, T message, int channelId = Channel.Reliable);
        void Send(INetworkPlayer player, ArraySegment<byte> segment, int channelId = Channel.Reliable);
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
        UniTask ProcessMessagesAsync(INetworkPlayer player);
    }

    /// <summary>
    /// An object that can send notify messages
    /// </summary>
    public interface INotifySender
    {
        /// <summary>
        /// Sends a message, but notify when it is delivered or lost
        /// </summary>
        /// <typeparam name="T">type of message to send</typeparam>
        /// <param name="message">message to send</param>
        /// <param name="token">a arbitrary object that the sender will receive with their notification</param>
        void SendNotify<T>(INetworkPlayer player, T message, object token, int channelId = Channel.Unreliable);
    }

    /// <summary>
    /// An object that can receive notify messages
    /// </summary>
    public interface INotifyReceiver
    {
        /// <summary>
        /// Raised when a message is delivered
        /// </summary>
        event Action<INetworkPlayer, object> NotifyDelivered;

        /// <summary>
        /// Raised when a message is lost
        /// </summary>
        event Action<INetworkPlayer, object> NotifyLost;
    }

    /// <summary>
    /// An object that can send and receive messages and notify messages
    /// </summary>
    public interface IMessageHandler : IMessageSender, IMessageReceiver, INotifySender, INotifyReceiver
    {

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
    public interface INetworkPlayer : IMessageSender, IVisibilityTracker, IObjectOwner, IAuthenticatedObject, ISceneLoader
    {
        IConnection Connection { get; }

        IMessageHandler messageHandler { get; }
        void Send<T>(T message, int channelId = 0);
        void Send(ArraySegment<byte> segment, int channelId = 0);
    }

    public interface IAuthenticatedObject
    {
        object AuthenticationData { get; set; }
    }

    public interface ISceneLoader
    {
        bool IsReady { get; set; }
    }
}
