# Train Debugging Commands - Quick Reference

## Problem: No train GameObjects found

Use these commands to investigate what trains actually are in Shapez 2.

## Investigation Commands

### 1. Check for Unity ECS
```
checkecs
```
**Purpose**: Determines if the game uses Entity Component System instead of GameObjects  
**What it tells you**: If trains are Entities (not GameObjects), a completely different approach is needed

### 2. List All GameObject Names
```
listallnames
```
**Purpose**: Shows every unique GameObject name in the scene  
**What to look for**: 
- Any patterns that might be trains
- Objects that appear many times (count shown as "x5", etc.)
- Names like "vehicle", "cargo", "sim", "entity"

### 3. Search by Pattern
```
searchobjects <pattern>
```
**Examples**:
```
searchobjects vehicle
searchobjects cargo
searchobjects mesh
searchobjects visual
searchobjects sim
```
**Purpose**: Find GameObjects matching a specific word  
**What to look for**: Anything that might be train-related

### 4. List Train Objects (Enhanced)
```
listtrainobjects
```
**Purpose**: Searches for train-related objects  
**What it does**:
- Searches for "train", "locomotive", "wagon", "cargo", "vehicle"
- If nothing found, lists first 50 objects with MeshRenderer
- Shows all root GameObjects

### 5. Get Train Simulation Data
```
traindata
```
**Purpose**: Shows detailed simulation data for all trains  
**What it shows**:
- Train positions (incoming/outgoing chunks)
- Directions
- World coordinates
- Progress (if available)
- Calculated position and rotation

### 6. List Existing Trains
```
listtrains
```
**Purpose**: Lists all TrainId values and their chunk positions  
**Confirms**: Trains exist in simulation even if visuals aren't found

## Investigation Workflow

### Step 1: Confirm ECS Usage
```
checkecs
```
- If ECS found → Trains are Entities, need different approach
- If no ECS → Continue with GameObject investigation

### Step 2: Get All Names
```
listallnames
```
- Look through the list for anything suspicious
- Note any patterns or repeated names

### Step 3: Search Common Terms
```
searchobjects vehicle
searchobjects mesh
searchobjects entity
```

### Step 4: Check Rendered Objects
```
listtrainobjects
```
- This will list mesh renderers if no trains found by name
- Trains must be visible, so they're rendered somehow

### Step 5: Examine Simulation Data
```
traindata
```
- Shows where trains actually are in the simulation
- Can use this data even without GameObjects

## Expected Outcomes

### Outcome A: Found GameObjects
✅ Great! Note the names and update `TrainGameObjectHelper.FindTrainGameObject()` to search for those names.

### Outcome B: ECS Detected
⚠️ Need to query EntityManager instead of GameObjects. This requires:
- Accessing Unity.Entities
- Creating Entity queries
- Reading position from TransformComponent

### Outcome C: Instanced Rendering
⚠️ Trains rendered without individual GameObjects. Options:
- Use simulation data only (already implemented in `TrainSimulationHelper`)
- Calculate positions mathematically
- Hook into rendering system

### Outcome D: Custom System
⚠️ Need to find the rendering implementation:
```
listasm Game.Core.Rendering
listasm Game.Core
```
Look for train-specific rendering classes.

## Fallback: Use Simulation Data

If no GameObjects can be found, use `TrainSimulationHelper`:

```csharp
var trainData = trainSim.GetTrainData(trainId);
var (position, rotation) = TrainSimulationHelper.CalculateTrainPosition(trainData);

// Use this for camera positioning
camera.CurrentPosition = position;
camera.TargetRotationDegrees = rotation;
```

This calculates position from the track data even without visual GameObjects.

## Report Your Findings

After running these commands, note:
1. ✓/✗ Unity ECS detected
2. Total unique GameObject names: ___
3. Any suspicious patterns: ___
4. Objects with MeshRenderer: ___
5. Total trains in simulation: ___

This information will determine the next approach!
