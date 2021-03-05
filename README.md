[![Documentation](https://img.shields.io/badge/documentation-brightgreen.svg)](https://miragenet.github.io/Mirage/)
[![Forum](https://img.shields.io/badge/forum-brightgreen.svg)](https://forum.unity.com/threads/mirror-networking-for-unity-aka-hlapi-community-edition.425437/)
[![Discord](https://img.shields.io/discord/809535064551456888.svg)](https://discordapp.com/invite/DTBPBYvexy)
[![release](https://img.shields.io/github/release/MirageNet/Mirage.svg)](https://github.com/MirageNet/Mirage/releases/latest)
[![openupm](https://img.shields.io/npm/v/com.miragenet.mirage?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.miragenet.mirage/)

[![Build](https://github.com/MirageNet/Mirage/workflows/CI/badge.svg)](https://github.com/MirageNet/Mirage/actions?query=workflow%3ACI)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=MirageNet_Mirage&metric=alert_status)](https://sonarcloud.io/dashboard?id=MirageNet_Mirage)
[![SonarCloud Coverage](https://sonarcloud.io/api/project_badges/measure?project=MirageNet_Mirage&metric=coverage)](https://sonarcloud.io/component_measures?id=MirageNet_Mirage&metric=coverage)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=MirageNet_Mirage&metric=ncloc)](https://sonarcloud.io/dashboard?id=MirageNet_Mirage)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=MirageNet_Mirage&metric=sqale_index)](https://sonarcloud.io/dashboard?id=MirageNet_Mirage)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=MirageNet_Mirage&metric=code_smells)](https://sonarcloud.io/dashboard?id=MirageNet_Mirage)


Mirage is a **high level** Networking API for Unity.

Mirage is built [and tested](https://www.youtube.com/watch?v=mDCNff1S9ZU) for **MMO Scale** Networking by the developers of  [Cubica](https://cubica.net).

Mirage is optimized for **ease of use** and **probability of success**.

With Mirage the objects in the client are mirror images of the objects in the server.  Mirage provides all the tools necessary to keep them in sync and pass messages between them.

## Installation

To install Mirage, follow these steps:

1) Install [Unity 2020.1.17 or later](https://unity.com/)
2) Start a new project or open your existing one
3) Add openupm registry.  Click on the menu Edit -> Project settings...,  and add a scoped registry like so:
   ![Scoped Registry](doc/images/Scoped%20Registry.png)
4) Close the project settings
5) Open the package manager.  Click on menu Window -> Package Manager and select "Packages: My Registries", select the latest version of Mirage and click install, like so:
   ![Install Mirage](doc/images/Install%20Mirage.png)
6) You may come back to the package manager to unistall Mirage or upgrade it.

## Comparison with Mirror
When migrating a project from Mirror to Mirage, these will be the most notable differences.

| Mirage                                      | Mirror                                 |
| --------------------------------------------- | -------------------------------------- |
| Install via Unity Package Manager             | Install from Asset Store               |
| Errors are thrown as exceptions               | Errors are logged                      |
| `[ServerRpc]`                                 | `[Command]`                            |
| `[ClientRpc(target=Client.Owner)]`            | `[TargetRpc]`                          |
| Subscribe to events in `NetworkServer`        | Override methods in `NetworkManager`   |
| Subscribe to events in `NetworkClient`        | Override methods in `NetworkManager`   |
| Subscribe to events in `NetworkIdentity`      | Override methods in `NetworkBehaviour` |
| Methods use PascalCase (C# guidelines)        | No consistency                         |
| `NetworkTime` available in `NetworkBehaviour` | `NetworkTime` is global static         |
| Send any data as messages                     | Messages must implement NetworkMessage |
| Supports Unity 2019.3 or later                | Supports Unity 2018.4 or later         |

Mirage has many new features
* Mirage supports [fast domain reload](https://blogs.unity3d.com/2019/11/05/enter-play-mode-faster-in-unity-2019-3/)
* Components can be added in child objects
* Your client can connect to multiple servers. For example chat server and game server
* Modular,  use only the components you need.
* Error handling
* [Version defines](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html#define-symbols)
* Server Rpcs can [return values](https://miragenet.github.io/Mirage/Articles/Guides/Communications/RemoteActions.html)
* The default transport features DoS prevention
* The default transport has CRC64 integrity check
* Mirage provides unreliable channel out of the box

If you look under the hood,  the code base has some significant differences based on the core values of each project
* Mirage follows the [SOLID principles](https://en.wikipedia.org/wiki/SOLID).
* Mirage avoids singletons and static state in general.
* Mirage has high [![Test Coverage](https://sonarcloud.io/api/project_badges/measure?project=MirageNet_Mirage&metric=coverage)](https://sonarcloud.io/dashboard?id=MirageNet_Mirage)
* Mirage has low [![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=MirageNet_Mirage&metric=sqale_index)](https://sonarcloud.io/dashboard?id=MirageNet_Mirage)
* Mirage values code quality,  Mirror values API stability

## Development environment
If you want to contribute to  Mirage, follow these steps:

### Linux and Mac
1) Install git
2) clone this repo
3) Open in unity 2020.1.x or later

### Windows
1) Install [git](https://git-scm.com/download/win) or use your favorite git client
2) as administrator, clone this repo with symbolic links support:
    ```sh
    git clone -c core.symlinks=true https://github.com/MirageNet/Mirage.git
    ```
    It you don't want to use administrator, [add symlink support](https://www.joshkel.com/2018/01/18/symlinks-in-windows/) to your account.
    If you don't enable symlinks, you will be able to work on Mirage but Unity will not see the examples.
3) Open in unity 2019.4.x or later

## Transports
Here is a list of some transports supported by NG and how they compare to each other

|                |        Kcp         | [Websocket](https://github.com/MirageNet/WebsocketNG) | [Steam](https://github.com/dragonslaya84/FizzySteamyMirror) | [LiteNetLibNG](https://github.com/uweenukr/LiteNetLibNG) | [IgnoranceNG](https://github.com/dragonslaya84/IgnoranceNG) |
| -------------- | :----------------: | :--------------------------------------------------: | :---------------------------------------------------------: | :------------------------------------------------------: | :---------------------------------------------------------: |
| **CCU**        |       1000+        |                          ?                           |                              ?                              |                            ?                             |                              ?                              |
| **Protocol**   |        UDP         |                         TCP                          |                             UDP                             |                           UDP                            |                             UDP                             |
| **Unreliable** | :white_check_mark: |                                                      |                     :white_check_mark:                      |                    :white_check_mark:                    |                     :white_check_mark:                      |
| **WebGL**      |                    |                  :white_check_mark:                  |                                                             |                                                          |                                                             |
| **Mobile**     | :white_check_mark: |                                                      |                                                             |                            ?                             |                              ?                              |
| **CPU**        |        LOW         |                         HIGH                         |                              ?                              |                            ?                             |                              ?                              |
| **NAT Punch**  |                    |                                                      |                     :white_check_mark:                      |                                                          |                                                             |
| **Encryption** |                    |                  :white_check_mark:                  |                     :white_check_mark:                      |                                                          |                                                             |
| **IPV6**       | :white_check_mark: |                  :white_check_mark:                  |                              ?                              |                            ?                             |                              ?                              |
| **Managed**    | :white_check_mark: |                  :white_check_mark:                  |                                                             |                    :white_check_mark:                    |                                                             |
| **Based on**   |        KCP         |                      Websockets                      |                Steam Game Networking Sockets                |                        LiteNetLib                        |                            ENet                             |

## Contributing

There are several ways to contribute to this project:

* Pull requests for bug fixes and features are always appreciated.
* Pull requests to improve the documentation is also welcome
* Make tutorials on how to use this
* Test it and open issues
* Review existing pull requests
* Donations

When contributing code, please keep these things in mind:

* [KISS](https://en.wikipedia.org/wiki/KISS_principle) principle. Everything needs to be **as simple as possible**. 
* An API is like a joke,  if you have to explain it is not a good one.  Do not require people to read the documentation if you can avoid it.
* Follow [C# code conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions).
* Follow [SOLID principles](https://en.wikipedia.org/wiki/SOLID) as much as possible. 
* Keep your pull requests small and obvious,  if a PR can be split into several small ones, do so.
