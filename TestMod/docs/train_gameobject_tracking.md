# Train GameObject Tracking Guide

## Overview
This guide explains how to get the position and rotation of trains in Shapez 2, which are animated using Unity AnimationCurves.

## Components Added

### 1. TrainGameObjectHelper.cs
A helper class that provides methods to:
- Find train GameObjects in the scene
- Get their position and rotation from the Transform or animation components
- Log all train-related GameObjects for debugging

### 2. Updated MyGameObject.cs
Modified to track and follow the actual train GameObject instead of just the chunk position.

## Usage

### In-Game Commands

1. **List all trains (simulation data)**
   ```
   listtrains
   ```
   Shows TrainId and chunk positions.

2. **List train GameObjects (visual objects)**
   ```
   listtrainobjects
   ```
   Logs all GameObjects with "train", "locomotive", or "wagon" in their names, including:
   - GameObject name
   - Active state
   - Transform position and rotation
   - Parent object
   - All attached components

### Camera Following

Press the **period key (.)** to:
1. Bind camera to the first train
2. Cycle to the next train
3. Unbind camera from trains

The camera now follows the actual train model's position and rotation rather than just the track chunk.

## How It Works

### Finding Train GameObjects

The `TrainGameObjectHelper.FindTrainGameObject(TrainId)` method searches for GameObjects by:
1. Looking for objects with "Train", "train", "locomotive", or "wagon" in their name
2. Checking for objects with a "Train" tag
3. Logging all components to help identify the correct object

### Getting Position and Rotation

The `GetTrainAnimatedTransform(GameObject)` method:
1. Checks for custom animation components that might have Position/Rotation properties
2. Falls back to reading the Transform (which Unity's animation system updates automatically)
3. Returns a tuple of (Vector3 position, Quaternion rotation)

### AnimationCurve Integration

Unity's animation system (including AnimationCurves) automatically updates the GameObject's Transform component. This means:
- **Position**: Read from `trainObject.transform.position`
- **Rotation**: Read from `trainObject.transform.rotation`
- These values are updated every frame by Unity's animation system

## Debugging

If the camera isn't following trains correctly:

1. Run `listtrainobjects` command to see all train GameObjects
2. Check the log for:
   - GameObject names
   - Component types attached to train objects
   - Position and rotation values

3. Look for components like:
   - `TrainView`
   - `TrainRenderer`
   - `TrainAnimator`
   - Or any custom component controlling the train visuals

## Code Example

```csharp
// Find the train GameObject
GameObject trainObj = TrainGameObjectHelper.FindTrainGameObject(trainId);

if (trainObj != null)
{
	// Get current position and rotation
	var (position, rotation) = TrainGameObjectHelper.GetTrainAnimatedTransform(trainObj);

	// Use the position
	Debug.Log($"Train at: {position}");

	// Get rotation as Euler angles (degrees)
	Vector3 eulerAngles = rotation.eulerAngles;
	Debug.Log($"Train rotation: {eulerAngles.y} degrees");
}
```

## Notes

- Trains are animated using Unity's built-in systems, which update the Transform automatically
- The AnimationCurve data is internal to the animation component - we read the result from Transform
- If trains use a custom animation system, the helper will attempt to find Position/Rotation properties on MonoBehaviour components
- The logging is verbose to help identify the exact component structure used by Shapez 2

## Next Steps

1. Run the game and use `listtrainobjects` to identify the exact component structure
2. If needed, update `FindTrainGameObject()` to use more specific search criteria
3. If trains use a custom component, add specific handling in `GetTrainAnimatedTransform()`
