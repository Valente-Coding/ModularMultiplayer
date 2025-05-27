# Networked GameObject Manager for Unity Netcode

A comprehensive solution for managing GameObjects across multiplayer clients using Unity Netcode for GameObjects. This system provides centralized control over spawning, destroying, and synchronizing networked objects with advanced features like ownership management and automatic cleanup.

## 📁 Files Overview

### Core Scripts
- **`MP_NetworkedGameObjectManager.cs`** - Main manager class for handling networked GameObjects
- **`MP_NetworkedObjectRegistry.cs`** - ScriptableObject for registering spawnable prefabs
- **`MP_NetworkedObjectSpawner.cs`** - Example implementation and utility script
- **`Editor/MP_NetworkedObjectRegistryEditor.cs`** - Custom Unity Editor for the registry

## 🚀 Features

### MP_NetworkedGameObjectManager
- ✅ **Centralized GameObject Management** - Single point of control for all networked objects
- ✅ **Server/Client Architecture** - Proper separation of server authority and client requests
- ✅ **Ownership Management** - Track and transfer ownership between clients
- ✅ **Automatic Cleanup** - Clean up objects when clients disconnect
- ✅ **Event System** - Comprehensive events for object lifecycle
- ✅ **Position Synchronization** - Teleport objects across all clients
- ✅ **Active State Management** - Enable/disable objects across the network
- ✅ **Performance Optimized** - Efficient caching and lookup systems
- ✅ **Debugging Support** - Optional debug logging for troubleshooting

### MP_NetworkedObjectRegistry
- ✅ **Prefab Management** - Centralized registry for all spawnable prefabs
- ✅ **Unique ID System** - Each prefab gets a unique network-safe ID
- ✅ **Validation** - Automatic validation of NetworkObject components
- ✅ **Editor Integration** - Custom Unity Editor for easy management
- ✅ **Runtime Lookup** - Fast prefab lookup by ID or GameObject reference

### MP_NetworkedObjectSpawner
- ✅ **Example Implementation** - Shows best practices for usage
- ✅ **Input Handling** - Keyboard controls for testing
- ✅ **Auto-Spawning** - Automatic object spawning for testing
- ✅ **Batch Operations** - Spawn/destroy multiple objects at once
- ✅ **Statistics** - Track spawned objects and performance

## 🛠️ Setup Instructions

### 1. Create the Registry
1. Right-click in your Project window
2. Select `Create > Multiplayer > Networked Object Registry`
3. Name it (e.g., "GameObjectRegistry")
4. Add your networked prefabs to the registry using the custom editor

### 2. Setup the Manager
1. Create an empty GameObject in your scene
2. Add the `MP_NetworkedGameObjectManager` component
3. Assign your `MP_NetworkedObjectRegistry` to the `Object Registry` field
4. Configure settings (max objects, debug logging, etc.)

### 3. Add to Network Prefabs
1. In your `NetworkManager`, add the `MP_NetworkedGameObjectManager` prefab to the Network Prefabs list
2. Ensure all your spawnable prefabs are also in the Network Prefabs list

### 4. (Optional) Add Example Spawner
1. Add the `MP_NetworkedObjectSpawner` component to test the system
2. Configure spawn points and input keys
3. Assign the same registry used by the manager

## 💻 Usage Examples

### Basic Spawning
```csharp
// Spawn an object by prefab ID
MP_NetworkedGameObjectManager.Instance.SpawnNetworkedObject(
    prefabId: 0, 
    position: Vector3.zero, 
    rotation: Quaternion.identity, 
    ownerClientId: NetworkManager.Singleton.LocalClientId
);
```

### Destroying Objects
```csharp
// Destroy a specific object
MP_NetworkedGameObjectManager.Instance.DestroyNetworkedObject(networkObject);
```

### Ownership Management
```csharp
// Transfer ownership to another client
MP_NetworkedGameObjectManager.Instance.ChangeObjectOwnership(networkObject, newOwnerClientId);

// Get all objects owned by a client
var ownedObjects = MP_NetworkedGameObjectManager.Instance.GetObjectsOwnedByClient(clientId);
```

### Object Manipulation
```csharp
// Teleport an object
MP_NetworkedGameObjectManager.Instance.TeleportObject(networkObject, newPosition);

// Set active state
MP_NetworkedGameObjectManager.Instance.SetObjectActive(networkObject, false);
```

### Event Handling
```csharp
void Start()
{
    // Subscribe to events
    MP_NetworkedGameObjectManager.Instance.OnGameObjectSpawned += OnObjectSpawned;
    MP_NetworkedGameObjectManager.Instance.OnGameObjectDestroyed += OnObjectDestroyed;
    MP_NetworkedGameObjectManager.Instance.OnOwnershipChanged += OnOwnershipChanged;
}

private void OnObjectSpawned(object sender, MP_NetworkedGameObjectManager.OnGameObjectSpawnedEventArgs e)
{
    Debug.Log($"Object spawned: {e.spawnedObject.name} by client {e.ownerClientId}");
}
```

## 🎮 Testing Controls (MP_NetworkedObjectSpawner)

When using the example `MP_NetworkedObjectSpawner`:

- **G Key** - Spawn a random object
- **H Key** - Destroy a random object
- **J Key** - Teleport a random object

## 📋 Registry Management

The `MP_NetworkedObjectRegistry` provides a custom Unity Editor with these features:

### Adding Prefabs
1. Enter a name for your prefab entry
2. Drag your prefab (must have NetworkObject component)
3. Add an optional description
4. Click "Add Prefab to Registry"

### Management Tools
- **Validate Registry** - Removes invalid entries and checks for issues
- **Regenerate IDs** - Reassigns all prefab IDs sequentially
- **Clear Cache** - Clears the runtime lookup cache
- **Sort by Name** - Alphabetically sorts all entries

### Validation
The editor automatically warns about:
- ⚠️ Missing NetworkObject components
- ⚠️ Duplicate prefab IDs
- ⚠️ Duplicate prefab references
- ⚠️ Missing prefab references

## 🔧 Configuration Options

### MP_NetworkedGameObjectManager Settings
- **Enable Debug Logs** - Show detailed logging for troubleshooting
- **Max Managed Objects** - Maximum number of objects that can be managed (performance limit)
- **Object Registry** - Reference to your MP_NetworkedObjectRegistry

### MP_NetworkedObjectSpawner Settings
- **Auto Spawn on Start** - Automatically spawn objects when the game starts
- **Auto Spawn Count** - Maximum number of objects to auto-spawn
- **Auto Spawn Interval** - Time between auto-spawns
- **Spawn Points** - Predefined locations for spawning
- **Spawn Radius** - Random offset range for spawning

## 🏗️ Architecture Patterns

### Server Authority
- All spawn/destroy operations go through the server
- Server validates all requests before execution
- Server handles client disconnections automatically

### Client Ownership
- Each object has an owner client
- Owner can request operations on their objects
- Ownership can be transferred between clients

### Event-Driven Design
- All major operations trigger events
- Easy to extend with custom behaviors
- Loose coupling between components

### Caching Strategy
- Runtime caches for fast lookups
- Automatic cache invalidation
- Memory-efficient storage

## 🐛 Troubleshooting

### Common Issues

**Objects not spawning:**
- Ensure prefabs are in the NetworkManager's Network Prefabs list
- Check that prefabs have NetworkObject components
- Verify the MP_NetworkedGameObjectManager is spawned as a NetworkObject

**Registry not working:**
- Make sure the registry is assigned to the manager
- Check for validation errors in the registry editor
- Ensure prefab IDs are unique

**Events not firing:**
- Subscribe to events after MP_NetworkedGameObjectManager.Instance is available
- Check that the manager is properly spawned on the network

**Performance issues:**
- Reduce the max managed objects limit
- Disable debug logging in production
- Consider pooling for frequently spawned objects

### Debug Information

Enable debug logs in the MP_NetworkedGameObjectManager to see:
- Object spawn/destroy operations
- Ownership changes
- Client disconnect cleanup
- Cache operations

## 🔄 Integration with Existing Code

This system is designed to work alongside the existing kitchen game architecture:

### Similar to KitchenObject Pattern
```csharp
// Instead of KitchenObject.SpawnKitchenObject()
MP_NetworkedGameObjectManager.Instance.SpawnNetworkedObject(prefabId, position, rotation, ownerId);

// Instead of KitchenObject.DestroyKitchenObject()
MP_NetworkedGameObjectManager.Instance.DestroyNetworkedObject(networkObject);
```

### Works with Existing NetworkBehaviour Classes
Your existing NetworkBehaviour scripts will work normally with spawned objects. The manager handles the spawning/despawning, while your scripts handle the behavior.

## 📈 Performance Considerations

### Optimization Tips
1. **Limit Max Objects** - Set reasonable limits based on your game's needs
2. **Use Object Pooling** - For frequently spawned/destroyed objects
3. **Batch Operations** - Group multiple spawns together when possible
4. **Cleanup Strategy** - Regular cleanup of unused objects

### Memory Usage
- Each managed object stores ~64 bytes of metadata
- Registry cache is built once and reused
- Event subscriptions should be properly unsubscribed

## 🤝 Contributing

This system follows the patterns established in the reference code:
- Uses Unity Netcode for GameObjects
- Follows the ServerRpc/ClientRpc pattern
- Implements singleton pattern for global access
- Uses events for decoupled communication

Feel free to extend the system with additional features like:
- Object pooling integration
- Custom serialization for complex data
- Integration with game-specific managers
- Additional validation rules

## 📄 License

This code is provided as a reference implementation for Unity multiplayer development. Modify and use according to your project's needs.
