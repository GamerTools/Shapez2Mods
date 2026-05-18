Building Deletion Code Findings
================================

CORE API - IMapModel Interface
-------------------------------
Location: IMapModel.cs (Game.Core.Map.Model assembly)

The primary method for deleting a building is:

```csharp
void DeleteBuilding(in BuildingId buildingId);
```

This is defined in the IMapModel interface, which is the main interface for map operations.

RELATED METHODS IN IMapModel
-----------------------------
```csharp
// Creating buildings
BuildingModel CreateBuilding(IBuildingDefinition definition, in GlobalTileTransform transform, IBuildingConfiguration configuration);
BuildingModel CreateBuilding(IBuildingDefinition definition, in GlobalTileTransform transform, in BuildingId buildingId, IBuildingConfiguration configuration);

// Deleting buildings
void DeleteBuilding(in BuildingId buildingId);

// Island operations
IslandModel CreateIsland(IIslandDefinition definition, GlobalChunkTransform transform, IIslandConfiguration configuration);
void DeleteIsland(in IslandId islandId);

// Querying buildings
void GetIslandBuildings(IslandId islandId, ICollection<BuildingModel> buildings);
IEnumerable<BuildingId> GetChunkBuildings(GlobalChunkCoordinate chunk_GC);
int GetBuildingsCount(IslandId id);
```

BUILDING MODEL STRUCTURE
-------------------------
From BuildingModel.cs:

```csharp
public readonly struct BuildingModel
{
	public readonly BuildingId Id;              // Unique identifier
	public readonly IMapModel Map;              // Reference to the map
	public GlobalTileTransform Transform;       // Position and rotation
	public GridRotation Rotation_G;             // Rotation (from Transform.Rotation)
	public IBuildingDefinition Definition;      // Building type definition
	public IBuildingConfiguration Configuration; // Building configuration
}
```

KEYBINDINGS FOR DELETION
-------------------------
From DefaultKeybindings.cs - "mass-selection" layer:

1. delete: X key (or Delete key)
   - new Keybinding("delete", new KeySet(KeyCode.X), new KeySet(KeyCode.Delete))

2. quick-delete-drag: Right Mouse Button
   - new Keybinding("quick-delete-drag", new KeySet(KeyCode.Mouse1))

3. cut-selection: Ctrl+X (Cmd+X on Mac)
   - new Keybinding("cut-selection", new KeySet(KeyCode.X, DefaultControlKeyCode))

PLAYER ACTION SYSTEM
--------------------
From GameSessionOrchestrator.cs:

The game has two PlayerActionManager instances:
- GodActions: For god-mode/admin actions
- PlayerActions: For normal player actions

These managers likely handle the deletion commands and call IMapModel.DeleteBuilding().

HOW TO DELETE A BUILDING IN CODE
---------------------------------
To delete a building programmatically:

```csharp
// Get the map from the game session or building
IMapModel map = building.Map;

// Get the building ID
BuildingId buildingId = building.Id;

// Delete the building
map.DeleteBuilding(in buildingId);
```

Or more concisely if you have a BuildingModel:
```csharp
building.Map.DeleteBuilding(in building.Id);
```

RELATED INTERFACES
------------------
- IBuildingModelAccessor: For accessing building models
- IBunchEditor: For batch operations on buildings
- ISimulator: Handles simulation updates when buildings are added/removed

NEXT STEPS TO FIND FULL DELETION FLOW
--------------------------------------
To find the complete deletion handling code, look for:
1. PlayerActionManager implementation
2. Mass selection state handlers that respond to "delete" keybinding
3. MapModel class (implementation of IMapModel.DeleteBuilding)
4. DeleteBuildingCommand or similar command pattern classes
5. Undo/redo system that wraps building deletion operations
