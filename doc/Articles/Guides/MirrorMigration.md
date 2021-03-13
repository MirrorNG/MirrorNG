# Mirror Migration Guide
This is a simple guide describing how to convert your Mirror project to Mirage.

## Namespace
First of all, `Mirror` namespace needs to be changed to `Mirage`. So in your code, replace all:

```cs
using Mirror;
```

with

```cs
using Mirage;
```

## Components
Many roles that `NetworkManager` fulfilled in Mirror were split into multiple simpler components in Mirage, such as `NetworkClient`, `NetworkServer` and `NetworkSceneManager`. Those clases are no longer static singletons, they are MonoBehaviours instead, so you need to add them to your scene and reference them. `NetworkManager` in Mirage only serves as a reference holder for server and client.

> [!TIP]
> The easiest way to get started is to right click in the Hierarchy > Network > NetworkManager. This will create a GameObject with all the necessary components and references already set up.

### Accessing Mirage components from NetworkBehaviour
Despite Mirage removing all static state, you can still access the important networking components from within `NetworkBehaviour` easily. This table shows how to access different components in comparison to Mirror:

| Mirror (static) | Mirage (property of `NetworkBehaviour`) |
|:---------------:|:---------------------------------------:|
| `NetworkServer` | `Server`                                |
| `NetworkClient` | `Client`                                |
| `NetworkTime`   | `NetworkTime`                           |
| doesn't exist   | `ClientObjectManager`                   |
| doesn't exist   | `ServerObjectManager`                   |

## Network Events Lifecycle
Lifecycle management is no longer based on overrides. Instead, there are many UnityEvents that can be hooked into without direct coupling. They can also be used to hook callbacks via Unity Inspector.

> [!TIP]
> This guide only shows the Mirror counterpart events, but Mirage has more events available, so be sure to check them out as they might be useful.

### Server and client events
The table below shows the override method names from Mirror's `NetworkManager` and the corresponding events from Mirage.

| Mirror (override)      | Mirage (event)                                                          |
|:----------------------:|:-----------------------------------------------------------------------:|
| `OnStartServer`        | [NetworkServer.Started](xref:Mirage.NetworkServer.Started)              |
| `OnServerConnect`      | [NetworkServer.Authenticated](xref:Mirage.NetworkServer.Authenticated)  |
| `OnServerDisconnect`   | [NetworkServer.Disconnected](xref:Mirage.NetworkServer.Disconnected)    |
| `OnStopServer`         | [NetworkServer.Stopped](xref:Mirage.NetworkServer.Stopped)              |
| `OnClientConnect`      | [NetworkClient.Authenticated](xref:Mirage.NetworkClient.Authenticated)  |
| `OnClientDisconnect`   | [NetworkClient.Disconnected](xref:Mirage.NetworkClient.Disconnected)    |


For example, this code from Mirror:

```cs
using Mirror;

class MyNetworkManager : NetworkManager {
    public override void OnStartServer() {
        // Server started
    }

    public override void OnServerConnect(NetworkConnection conn) {
        // Client connected on server
    }

    public override void OnStopServer() {
        // Server stopped
    }

    public override void OnClinetConnect(NetworkConnection conn) {
        // Client connected
    }

    public override void OnClientDisconenct(NetworkConnection conn) {
        // Client disconnected
    }
}
```

should be changed to:

```cs
using Mirage;

class MyNetworkManager : NetworkManager {
    void Awake() {
        Server.Started.AddListener(OnStartServer);
        Server.Authenticated.AddListener(OnServerConnect);
        Server.Stopped.AddListener(OnStopServer);
        Client.Authenticated.AddListener(OnClientConnect);
        Client.Disconnected.AddListener(OnClientDisconenct);
    }

    void OnStartServer() {
        // Server started
    }

    void OnServerConnect(INetworkPlayer conn) {
        // Client connected (and authenticated) on server
    }

    void OnStopServer() {
        // Server stopped
    }

    void OnClinetConnect(INetworkPlayer conn) {
        // Client connected
    }

    void OnClientDisconenct() {
        // Client disconnected
    }
}
```

### NetworkBehaviour events
Table below shows the Mirror's `NetworkBehaviour` override method names on the left and the Mirage events on the right.

| Mirror (override)      | Mirage (event)                                                                    |
|:----------------------:|:---------------------------------------------------------------------------------:|
| `OnStartServer`        | [NetIdentity.OnStartServer](xref:Mirage.NetworkIdentity.OnStartServer)            |
| `OnStopServer`         | [NetIdentity.OnStopServer](xref:Mirage.NetworkIdentity.OnStopServer)              |
| `OnStartClient`        | [NetIdentity.OnStartClient](xref:Mirage.NetworkIdentity.OnStartClient)            |
| `OnStopClient`         | [NetIdentity.OnStopClient](xref:Mirage.NetworkIdentity.OnStopClient)              |
| `OnStartLocalPlayer`   | [NetIdentity.OnStartLocalPlayer](xref:Mirage.NetworkIdentity.OnStartLocalPlayer)  |
| `OnStartAuthority`     | [NetIdentity.OnStartAuthority](xref:Mirage.NetworkIdentity.OnStartAuthority)      |
| `OnStopAuthority`      | [NetIdentity.OnStopAuthority](xref:Mirage.NetworkIdentity.OnStopAuthority)        |

Let's take this `Player` class as an example. In Mirror, you would do:

```cs
using Mirror;

public class Player : NetworkBehaviour {
    public override void OnStartServer() {
        // Player started on server
    }

    public override void OnStartClient() {
        // Player started on client
    }
}
```

Which should be changed like so in Mirage:

```cs
using Mirage;

public class Player : NetworkBehaviour {
    void Awake() {
        NetIdentity.OnStartServer.AddListener(OnStartServer);
        NetIdentity.OnStartClient.AddListener(OnStartClient);
    }

    void OnStartServer() {
        // Player started on server
    }

    void OnStartClient() {
        // Player started on client
    }
}
```

> [!NOTE]
> Please note that due to timing all event callbacks should be registered in `Awake` method or via Unity inspector in order for them to be invoked consistently.

## Attributes
The table below shows the new attribute names in Mirage.

| Mirror             | Mirage                                                                    |
|:------------------:|:-------------------------------------------------------------------------:|
| `[Command]`        | [[ServerRpc]](xref:Mirage.ServerRpcAttribute)                             |
| `[TargetRpc]`      | [[ClientRpc(target = Mirage.Client enum)](xref:Mirage.ClientRpcAttribute) |
| `[ServerCallback]` | [[Server(error = false)]](xref:Mirage.ServerAttribute)                    |

## Renames
These fields/properties have been renamed:

| Mirror                                | Mirage                                                                                 |
|:-------------------------------------:|:--------------------------------------------------------------------------------------:|
| `ClientScene.localPlayer`             | [ClientObjectManager.LocalPlayer](xref:Mirage.ClientObjectManager.LocalPlayer)         |
| `ClientScene.ready`                   | [NetworkClient.Connection.IsReady](xref:Mirage.NetworkPlayer.IsReady)              |
| `NetworkIdentity.assetId`             | [NetworkIdentity.AssetId](xref:Mirage.NetworkIdentity.AssetId)                         |
| `NetworkIdentity.netId`               | [NetworkIdentity.NetId](xref:Mirage.NetworkIdentity.NetId)                             |
| `NetworkIdentity.connectionToClient`  | [NetworkIdentity.ConnectionToClient](xref:Mirage.NetworkIdentity.ConnectionToClient)   |
| `NetworkBehaviour.isServer`           | [NetworkBehaviour.IsServer](xref:Mirage.NetworkBehaviour.IsServer)                     |
| `NetworkBehaviour.connectionToClient` | [NetworkBehaviour.ConnectionToClient](xref:Mirage.NetworkBehaviour.ConnectionToClient) |
| `NetworkBehaviour.connectionToServer` | [NetworkBehaviour.ConnectionToServer](xref:Mirage.NetworkBehaviour.ConnectionToServer) |
| `NetworkBehaviour.hasAuthority`       | [NetworkBehaviour.HasAuthority](xref:Mirage.NetworkBehaviour.HasAuthority)             |
| `NetworkBehaviour.netIdentity`        | [NetworkBehaviour.NetIdentity](xref:Mirage.NetworkBehaviour.NetIdentity)               |
| `NetworkBehaviour.netId`              | [NetworkBehaviour.NetId](xref:Mirage.NetworkBehaviour.NetId)                           |
| `NetworkBehaviour.isClientOnly`       | [NetworkBehaviour.IsClientOnly](xref:Mirage.NetworkBehaviour.IsClientOnly)             |
| `NetworkBehaviour.islocalPlayer`      | [NetworkBehaviour.IsLocalPlayer](xref:Mirage.NetworkBehaviour.IsLocalPlayer)           |
| `NetworkConnection.isReady`           | [NetworkPlayer.IsReady](xref:Mirage.NetworkPlayer.IsReady)                     |
| `NetworkConnection.identity`          | [NetworkPlayer.Identity](xref:Mirage.NetworkPlayer.Identity)                   |
| `NetworkServer.active`                | [NetworkServer.Active](xref:Mirage.NetworkServer.Active)                               |
| `NetworkServer.localConnection`       | [NetworkServer.LocalConnection](xref:Mirage.NetworkServer.LocalConnection)             |
| `NetworkClient.connection`            | [NetworkClient.Connection](xref:Mirage.NetworkClient.Connection)                       |
| `NetworkTime.time`                    | [NetworkTime.Time](xref:Mirage.NetworkTime.Time)                                       |

## Object Management
Registered spawnable prefabs were moved from `NetworkManager` to <xref:Mirage.ClientObjectManager> component. You can use the Inspector to register all NetworkIdentities via single click.

### Spawning and destroying
Table below shows how to spawn objects in Mirage from `NetworkBehaviour`:

| Mirror                  | Mirage                                                                                     |
|:-----------------------:|:------------------------------------------------------------------------------------------:|
| `NetworkServer.Spawn`   | [ServerObjectManager.Spawn](xref:Mirage.ServerObjectManager.Spawn(Mirage.NetworkIdentity)) |
| `NetworkServer.Destroy` | [ServerObjectManager.Destroy](xref:Mirage.ServerObjectManager.Destroy(UnityEngine.GameObject))         |