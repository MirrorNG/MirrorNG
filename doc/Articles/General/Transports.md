# Transports
Transports are low level components that are responsible for sending and receiving raw data over the netowrk. This allows Mirage to support many networking protocols and backends, such as UDP, WebSocket, Steam, etc.

## Available Transports
The table below shows a list of available transports.

|                |        Kcp (built-in)        | [Websocket](https://github.com/MirageNet/WebsocketNG) | Steam ([SteamWorks.NET](https://github.com/MirageNet/FizzySteamyMirror), [Facepunch](https://github.com/MirageNet/SteamyFaceNG)) |
| -------------- | :----------------: | :--------------------------------------------------: | :---------------------------------------------------------: |
| **CCU**        |       1000+        |                          ?                           |                              ?                              |
| **Protocol**   |        UDP         |                         TCP                          |                             UDP                             |
| **Unreliable** | :white_check_mark: |                                                      |                     :white_check_mark:                      |
| **WebGL**      |                    |                  :white_check_mark:                  |                                                             |
| **Mobile**     | :white_check_mark: |                                                      |                                                             |
| **CPU**        |        LOW         |                         HIGH                         |                              ?                              |
| **NAT Punch**  |                    |                                                      |                     :white_check_mark:                      |
| **Encryption** |                    |                  :white_check_mark:                  |                     :white_check_mark:                      |
| **IPV6**       | :white_check_mark: |                  :white_check_mark:                  |                              ?                              |
| **Managed**    | :white_check_mark: |                  :white_check_mark:                  |                                                             |
| **Based on**   |        KCP         |                      Websockets                      |                Steam Game Networking Sockets                |

## Changing a transport
To change a transport, follow the steps below:
1. Install the desired transport. Instructions are available on each transport's repository.
2. Add the transport as a new component on the object where you have the other Mirage components.
3. Assign a reference to this component in the `Transport` field of [NetworkServer](xref:Mirage.NetworkServer) and [NetworkClient](xref:Mirage.NetworkClient) components.
4. Check if the transport has any required additional steps.
5. Done. Mirage should now be using the newly added transport.
