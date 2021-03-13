# SyncDictionary
A <xref:Mirage.SyncDictionary`2> is an associative array containing an unordered list of key, value pairs. Keys and values can be any of [Mirage supported types](../DataTypes.md).

SyncDictionary works much like [SyncLists](SyncLists.md): when you make a change on the server, the change is propagated to all clients and the appropriate callback is called.

## Usage
Add a field of type `SyncDictionary<TKey, TValue>` on any <xref:Mirage.NetworkBehaviour> where `TKey` and `TValue` can be any supported Mirage type and initialize it.

> [!IMPORTANT]
> You need to initialize the SyncDictionary immediately after definition in order for them to work. You can mark them as `readonly` to enforce proper usage.

### Basic example
```cs
using UnityEngine;
using Mirage;

[System.Serializable]
public struct Item
{
    public string name;
    public int hitPoints;
    public int durability;
}

public class Player : NetworkBehaviour
{
    public readonly SyncDictionary<stirng, Item> Equipment = new SyncDictionary<string, Item>();

    void Awake() {
        NetIdentity.OnStartServer.AddListener(OnStartServer);
    }

    void OnStartServer()
    {
        Equipment.Add("head", new Item { name = "Helmet", hitPoints = 10, durability = 20 });
        Equipment.Add("body", new Item { name = "Epic Armor", hitPoints = 50, durability = 50 });
        Equipment.Add("feet", new Item { name = "Sneakers", hitPoints = 3, durability = 40 });
        Equipment.Add("hands", new Item { name = "Sword", hitPoints = 30, durability = 15 });
    }
}
```

## Callbacks
You can detect when a SyncDictionary changes on the client and/or server. This is especially useful for refreshing your UI, character appearance etc.

There are different callbacks for different operations, such as `OnChange` (any change to the dictionary), `OnInsert` (adding new element) etc. Please check the [SyncDictionary API reference](xref:Mirage.SyncIDictionary`2) for the complete list of callbacks.

Depending on where you want to invoke the callbacks, you can use these methods to register them:
- `Awake` for both client and server
- `NetIdentity.OnStartServer` event for server-only
- `NetIdentity.OnStartClient` event for cleint-only

> [!NOTE]
> By the time you subscribe, the dictionary will already be initialized, so you will not get a call for the initial data, only updates.

### Example
```cs
using Mirage;

public class Player : NetworkBehaviour {
    public readonly SyncDictionary<stirng, Item> Equipment = new SyncDictionary<string, Item>();
    public readonly SyncDictionary<stirng, Item> Hotbar = new SyncDictionary<string, Item>();

    // this will hook the callback on both server and client
    void Awake()
    {
        Equipment.OnChange += UpdateEquipment;
        NetIdentity.OnStartClient.AddListener(OnStartClient);
    }

    // hotbar changes will only be invoked on clients
    void OnStartClient() {
        Hotbar.OnChange += UpdateHotbar;
    }

    void UpdateEquipment()
    {
        // here you can refresh your UI for instance
    }

    void UpdateHotbar()
    {
        // here you can refresh your UI for instance
    }
}
```

By default, `SyncDictionary` uses a `Dictionary` to store its data. If you want to use a different dictionary implementation, add a constructor and pass the dictionary implementation to the parent constructor. For example:

```cs
public SyncDictionary<string, Item> myDict = new SyncIDictionary<string, Item>(new MyDictionary<string, Item>());
```