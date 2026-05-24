# Train GameObject Investigation Guide

## Problem
No GameObjects found with "train", "locomotive", or "wagon" in their names.

## Possible Reasons

### 1. **Unity DOTS / Entity Component System (ECS)**
Shapez 2 might be using Unity's Data-Oriented Technology Stack (DOTS), which doesn't use traditional GameObjects for performance-critical objects like trains. In ECS:
- Trains might be **Entities** instead of GameObjects
- Visual representation handled by rendering systems
- No traditional Transform components

### 2. **Instanced Rendering**
Trains might use GPU instancing for performance:
- Single mesh rendered multiple times
- No individual GameObjects per train
- Position/rotation data in compute buffers

### 3. **Different Naming Convention**
The trains might be called something unexpected:
- "Vehicle", "Cargo", "Container"
- Generic names like "Mesh", "Entity", "Visual"
- Part of a hierarchy like "World/Simulation/Vehicles"

### 4. **Procedurally Generated**
Trains might not exist as persistent GameObjects:
- Created/destroyed dynamically
- Only exist during active movement
- Pooled and reused

## Investigation Commands

Run these commands in-game to investigate:

### 1. List All Unique Names
```
listallnames
```
This shows every unique GameObject name in the scene. Look for:
- Patterns in naming
- Objects that appear multiple times (could be trains)
- Simulation/rendering related names

### 2. Search by Pattern
```
searchobjects vehicle
searchobjects cargo
searchobjects mesh
searchobjects entity
searchobjects sim
searchobjects world
```

### 3. Check Rendered Objects
The updated `listtrainobjects` command now also:
- Lists all objects with MeshRenderer components
- Shows the first 50 rendered objects if no trains found by name
- Lists root GameObjects

### 4. Examine Scene Hierarchy
Look for parent objects that might contain trains:
- World managers
- Simulation containers
- Rendering systems

## Alternative Approaches

### Approach 1: Use TrainData Position Directly
Instead of finding the GameObject, calculate the position from simulation data:

```csharp
var trainData = TrainSim.GetTrainData(CurrentTrainId);
// trainData contains:
// - Head.Incoming (ChunkTileDirection)
// - Head.Outgoing (ChunkTileDirection)
// - Progress (0-1 within current track segment)

// Calculate interpolated position between incoming and outgoing
var inPos = trainData.Head.Incoming.Position.ToOrigin_G();
var outPos = trainData.Head.Outgoing.Position.ToOrigin_G();
// Use progress to interpolate: lerp(inPos, outPos, progress)
```

### Approach 2: Hook Into Rendering System
Look for rendering-related assemblies:
- `Game.Core.Rendering.dll`
- Check for train rendering systems/components

```
listasm Game.Core.Rendering
```

### Approach 3: Search for Animation-Related Types
```
listasm Game.Core
```
Look for:
- TrainAnimator
- TrainView
- TrainRenderer
- TrainVisual
- VehicleRenderer

### Approach 4: Unity ECS Entities
If using DOTS, trains might be entities. Look for:
- `Unity.Entities` assemblies
- Entity queries
- Component systems

Check if game uses ECS:
```
listasmall
```
Look for assemblies like:
- Unity.Entities
- Unity.Transforms
- Unity.Rendering.Hybrid

## Debugging Steps

1. **Run `listallnames`** - Get complete GameObject inventory
2. **Search for common patterns** - vehicle, cargo, entity, sim, mesh
3. **Check the logs** for MeshRenderer objects - trains must be rendered somehow
4. **Look at TrainSystem assembly** - `listasm Game.Core` to find train-related types
5. **Examine parent hierarchy** - trains might be children of a manager object

## Questions to Answer

1. **Are there ANY moving visual objects?**
   - If yes, they must be rendered somehow
   - Check MeshRenderer objects from `listtrainobjects`

2. **Is the game using Unity ECS/DOTS?**
   - Check for Unity.Entities in `listasmall`
   - Traditional GameObject approach won't work

3. **What's in Game.Core.Rendering?**
   - Might contain the visual representation system
   - Check with `listasm Game.Core.Rendering`

4. **Can you see trains in the game?**
   - If visible, they're being rendered
   - Must be a rendering approach we haven't found yet

## Next Steps Based on Findings

### If ECS is used:
- Need to query Entities instead of GameObjects
- Access position through TransformComponent
- Different approach entirely

### If instanced rendering:
- Track via simulation data only
- Calculate position mathematically
- No direct GameObject access

### If custom rendering:
- Find the rendering system in assemblies
- Hook into update loop
- Access position data from rendering system

## Report Back

After running the investigation commands, report:
1. Total number of unique GameObject names
2. Any suspicious patterns in names
3. Number of objects with MeshRenderer
4. Whether Unity.Entities assemblies are loaded
5. Types found in Game.Core.Rendering

This will help determine the actual approach needed!
