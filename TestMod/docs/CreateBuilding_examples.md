# CreateBuilding Usage Examples

## From MapModel.cs Implementation

The `CreateBuilding` method in `MapModel.cs` shows how it's used internally:

```csharp
public BuildingModel CreateBuilding(IBuildingDefinition definition, in GlobalTileTransform transform, IBuildingConfiguration configuration)
{
	// Call the internal map's CreateBuilding which returns a BuildingId
	BuildingId buildingId = Map.CreateBuilding(definition, in transform, configuration);

	// Verify the building was added to the layout
	if (!LayoutModel.TryGetBuilding(in transform.Position, out var building))
	{
		throw new InvalidOperationException("The layout model does not include the building that was just created");
	}

	// Create and return the BuildingModel wrapper
	return CreateModel(buildingId, building);
}
```

## Example Usage Pattern

Based on the API structure, here's how you would call `CreateBuilding`:

```csharp
// Get the map from the game session
IMapModel map = GameHelper.Core.LocalPlayer.Map;  // or from session orchestrator

// Get the building definition (the type of building to create)
IBuildingDefinition definition = /* get from building registry or definition manager */;

// Create the transform (position and rotation)
GlobalTileCoordinate position = new GlobalTileCoordinate(x, y, layer);
GridRotation rotation = GridRotation.North;  // or South, East, West
GlobalTileTransform transform = new GlobalTileTransform(position, rotation);

// Get or create configuration (building-specific settings)
IBuildingConfiguration configuration = definition.CreateDefaultConfiguration();
// OR
IBuildingConfiguration configuration = null;  // for buildings that don't need configuration

// Create the building
BuildingModel newBuilding = map.CreateBuilding(definition, transform, configuration);

// The BuildingModel is now created and can be used
_logger.Info.Log($"Created building: {newBuilding.Definition.Id} at {newBuilding.Transform}");
```

## Alternative with Explicit BuildingId

If you need to specify a building ID (e.g., for deserialization):

```csharp
IMapModel map = GameHelper.Core.LocalPlayer.Map;
IBuildingDefinition definition = /* ... */;
GlobalTileTransform transform = /* ... */;
IBuildingConfiguration configuration = /* ... */;
BuildingId buildingId = new BuildingId(123);  // Specific ID

BuildingModel newBuilding = map.CreateBuilding(definition, transform, buildingId, configuration);
```

## Complete Example with Error Handling

```csharp
try
{
	// Get dependencies
	IMapModel map = GameHelper.Core.LocalPlayer.Map;

	// Define where to place the building
	var position = new GlobalTileCoordinate(10, 15, 0);  // x=10, y=15, layer=0
	var rotation = GridRotation.East;
	var transform = new GlobalTileTransform(position, rotation);

	// Get building definition (example - you'd get this from registry)
	// IBuildingDefinitionRegistry registry = ...;
	// IBuildingDefinition definition = registry.Get(new BuildingDefinitionId("Cutter"));

	// For now, assume we have the definition
	IBuildingDefinition definition = /* ... */;

	// Create configuration if needed
	IBuildingConfiguration configuration = definition.CreateDefaultConfiguration();

	// Create the building
	BuildingModel building = map.CreateBuilding(definition, transform, configuration);

	_logger?.Info.Log($"Successfully created {building.Definition.Id}");
	_logger?.Info.Log($"  Position: {building.Tile_G}");
	_logger?.Info.Log($"  Rotation: {building.Rotation_G}");
	_logger?.Info.Log($"  Building ID: {building.Id}");
}
catch (Exception ex)
{
	_logger?.Info.Log($"Failed to create building: {ex.Message}");
}
```

## Key Points

1. **IMapModel.CreateBuilding()** is the main API method
2. You need three/four things:
   - `IBuildingDefinition` - What type of building
   - `GlobalTileTransform` - Where and at what rotation
   - `IBuildingConfiguration` - Building-specific settings (can be null)
   - `BuildingId` (optional) - Usually auto-generated

3. The method returns a `BuildingModel` struct that wraps the created building

4. The building is immediately placed on the map and visible

5. For undo/redo support, you should use the game's command system (PlayerActionManager) instead of calling CreateBuilding directly

## Finding Building Definitions

To get a building definition, you typically need:

```csharp
// From a BuildingDefinitionId
BuildingDefinitionId definitionId = new BuildingDefinitionId("Cutter");
// Then look it up in the registry (exact API depends on game version)

// Or from an existing building
BuildingModel existing = /* ... */;
IBuildingDefinition definition = existing.Definition;
```

## Common Use Cases

- **Placing buildings programmatically**: Use CreateBuilding directly (god mode)
- **Blueprint paste**: Loop through blueprint data, call CreateBuilding for each
- **Save game loading**: Deserialize data, call CreateBuilding with explicit BuildingId
- **User placement**: Use PlayerActionManager commands (for undo/redo support)
