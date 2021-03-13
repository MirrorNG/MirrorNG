# General Overview

Mirage is a high level multiplayer library for Unity games. The goal is to make it as easy as possible to add multiplayer to your game.

Some of the key features of Mirage include:
* Sending and receiving messages
* State synchronization
* Client/Server and host mode

Mirage is made of 3 layers:
<div class="mermaid">
graph TD
    Obj[\Object Layer/] --> Msg
    Msg[\Message Layer/] --> Transport
    Transport[\Transport Layer/]
</div>

From the bottom up:

## Transport Layer

The `Transport Layer` is concerned about sending and receiving bytes.  It has no knowledge of what it is sending.  There are several transport implementations.  The default transport in Mirage is KCP. 

If you want to implement a transport, create a class that extends <xref:Mirage.Transport>.  It's primary responsibility is accepting and opening connections.
You will also need to create a class that represents a connection by implementing <xref:Mirage.IConnection>

## Message Layer

The message layer is concerned about sending and receiving [messages](../Guides/Communications/NetworkMessages.md)

If you wish to use this funtionality, you will need to have a <xref:Mirage.NetworkClient> in the client and a <xref:Mirage.NetworkServer> for the server. These classes provide events you can subscribe to for the life cycle of connections.  A connection is an implementation of <xref:Mirage.INetworkPlayer>, and can send and receive messages. 

## Object Layer

This layer is the highest level layer,  the classes in this layer are concerned about [synchcronizing state](../Guides/Sync/index.md) between objects, as well as sending [RPC calls](../Guides/Communications/RemoteActions.md).

The client needs a <xref:Mirage.ClientObjectManager>,  the server needs a <xref:Mirage.ServerObjectManager>. It will spawn and destroy objects and keep the objects in the client in sync with the objects in the server

# Clients and Servers 

Mirage supports 2 modes of operation which can work at the same time.

## Host mode

In host mode,  the server and client are running in the same application and share all networked objects.  There is a direct in-memory channel of communication between the <xref:Mirage.NetworkServer> and <xref:Mirage.NetworkClient>.  Since the objects are shared, there is no need to synchronize data.

Note that host mode bypasses the Transport Layer.

## Client / Server mode

In this mode,  the client is connected to a separate server, which is normally in another machine and reachable through the network.

In client / server mode, the objects are duplicated in the server and client.  For every networked object in the server, there is a corresponding object in the client with a matching network id.

Note a server can be in both host mode as well as server for other clients.

<div class="mermaid">
graph LR
    subgraph host["Host"]
        Client["Local Client"] --- Server
    end
    Server --- Client1
    Server --- Client2
    Server --- Client3
</div>
