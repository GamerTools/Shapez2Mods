# Unity AnimationCurve Explained

## What is AnimationCurve?

`AnimationCurve` is a Unity class that represents a **curve** that can be evaluated over time. It's essentially a function that maps an input value (usually time) to an output value.

```csharp
AnimationCurve curve = new AnimationCurve();
float value = curve.Evaluate(time); // Get value at specific time
```

## Core Concepts

### 1. Keyframes
An `AnimationCurve` is defined by a series of **keyframes** (key points):

```csharp
AnimationCurve curve = new AnimationCurve(
	new Keyframe(0f, 0f),    // At time 0, value is 0
	new Keyframe(1f, 1f),    // At time 1, value is 1
	new Keyframe(2f, 0f)     // At time 2, value is 0
);
```

### 2. Interpolation
Between keyframes, Unity **interpolates** (smoothly blends) the values:

```
Time:  0.0  →  0.5  →  1.0  →  1.5  →  2.0
Value: 0.0  →  0.5  →  1.0  →  0.5  →  0.0
	   └──── smooth ────┘  └──── smooth ────┘
```

### 3. Tangents
Each keyframe has **tangents** that control the curve shape:
- **In Tangent**: Controls the curve coming INTO the keyframe
- **Out Tangent**: Controls the curve going OUT of the keyframe

```csharp
Keyframe key = new Keyframe(1f, 1f);
key.inTangent = 1f;   // Steep approach
key.outTangent = 0f;  // Flat exit
```

## Common Tangent Modes

```csharp
// Linear - Straight line between keyframes
AnimationUtility.SetKeyLeftTangentMode(curve, 0, AnimationUtility.TangentMode.Linear);

// Smooth - Unity calculates smooth curve
AnimationUtility.SetKeyLeftTangentMode(curve, 0, AnimationUtility.TangentMode.ClampedAuto);

// Constant - Step function (no interpolation)
AnimationUtility.SetKeyLeftTangentMode(curve, 0, AnimationUtility.TangentMode.Constant);
```

## Visual Representation

```
	Value
	↑
1.0 |     ╱╲
	|    ╱  ╲
0.5 |   ╱    ╲
	|  ╱      ╲
0.0 |─┴────────┴─→ Time
	0   1   2   3

	Keyframes at: (0,0), (1,1), (2,0)
	Smooth interpolation between points
```

## How TrainAnimationConfig Likely Uses AnimationCurve

Based on the context of trains moving along tracks, here's how `AnimationCurve` is probably used:

### 1. Position Animation (X, Y, Z)
```csharp
public class TrainAnimationConfig
{
	public AnimationCurve PositionX;  // X position over time
	public AnimationCurve PositionY;  // Y position over time
	public AnimationCurve PositionZ;  // Z position over time

	// Evaluate position at specific time
	public Vector3 GetPositionAtTime(float time)
	{
		return new Vector3(
			PositionX.Evaluate(time),
			PositionY.Evaluate(time),
			PositionZ.Evaluate(time)
		);
	}
}
```

**Example: Train moving from (0,0,0) to (10,0,0)**
```csharp
// PositionX curve
new Keyframe(0.0f, 0.0f),   // Start at X=0
new Keyframe(1.0f, 10.0f)   // End at X=10

// PositionY and PositionZ stay at 0
```

### 2. Rotation Animation
```csharp
public AnimationCurve RotationY;  // Y-axis rotation (heading)

// Train turning 90 degrees
new Keyframe(0.0f, 0.0f),    // Facing north (0°)
new Keyframe(0.5f, 45.0f),   // Halfway turn (45°)
new Keyframe(1.0f, 90.0f)    // Facing east (90°)
```

### 3. Speed/Easing Curves
```csharp
public AnimationCurve SpeedCurve;  // Controls acceleration/deceleration

// Ease in, cruise, ease out
new Keyframe(0.0f, 0.0f),    // Start slow
new Keyframe(0.2f, 1.0f),    // Speed up
new Keyframe(0.8f, 1.0f),    // Maintain speed
new Keyframe(1.0f, 0.0f)     // Slow down
```

## How It Works in Practice

### Frame-by-Frame Animation
```csharp
public class TrainAnimator : MonoBehaviour
{
	public TrainAnimationConfig config;
	private float currentTime = 0f;

	void Update()
	{
		currentTime += Time.deltaTime;

		// Evaluate curves at current time
		Vector3 position = new Vector3(
			config.PositionX.Evaluate(currentTime),
			config.PositionY.Evaluate(currentTime),
			config.PositionZ.Evaluate(currentTime)
		);

		float rotationY = config.RotationY.Evaluate(currentTime);

		// Apply to transform
		transform.position = position;
		transform.rotation = Quaternion.Euler(0, rotationY, 0);
	}
}
```

### Progress-Based Animation
Instead of time, you can use **progress** (0-1):

```csharp
// ChunkProgress = 0.0 to 1.0 (from TrainSimulationData)
Vector3 position = new Vector3(
	config.PositionX.Evaluate(chunkProgress),
	config.PositionY.Evaluate(chunkProgress),
	config.PositionZ.Evaluate(chunkProgress)
);
```

## Why Use AnimationCurve for Trains?

### ✅ Benefits

1. **Non-Linear Movement**
   - Trains don't move at constant speed
   - Accelerate from stations
   - Decelerate when approaching

2. **Smooth Turns**
   - Curves handle rotation interpolation
   - No sudden snapping between angles

3. **Pre-Calculated Paths**
   - Define entire track segment as curve
   - Evaluate at any point instantly

4. **Designer-Friendly**
   - Artists can edit curves in Unity Inspector
   - No code changes needed for animation tweaks

5. **Performance**
   - `Evaluate()` is very fast (O(1) with caching)
   - No complex physics calculations needed

## Typical Train Animation Setup

### Straight Track Segment
```csharp
// Position curve for moving 10 units forward
PositionX = new AnimationCurve(
	new Keyframe(0f, 0f),
	new Keyframe(1f, 10f)
);

// No rotation change
RotationY = AnimationCurve.Constant(0f, 1f, 0f);
```

### Curved Track Segment (90° Turn)
```csharp
// X and Z describe arc path
PositionX = new AnimationCurve(
	new Keyframe(0f, 0f),
	new Keyframe(0.5f, 7.07f),  // ~45° point on arc
	new Keyframe(1f, 10f)
);

PositionZ = new AnimationCurve(
	new Keyframe(0f, 0f),
	new Keyframe(0.5f, 2.93f),
	new Keyframe(1f, 10f)
);

// Rotation smoothly turns 90°
RotationY = new AnimationCurve(
	new Keyframe(0f, 0f),
	new Keyframe(1f, 90f)
);
```

## How Shapez 2 Likely Uses This

### Theory: Pre-Baked Track Animations

1. **Each track piece** has a `TrainAnimationConfig`
2. **Curves are pre-calculated** for that track geometry
3. **At runtime:**
   ```csharp
   // Train enters track segment
   TrainAnimationConfig config = track.GetAnimationConfig();

   // Update loop
   float progress = trainSimData.ChunkProgress; // 0.0 to 1.0

   Vector3 localPosition = new Vector3(
	   config.PositionX.Evaluate(progress),
	   config.PositionY.Evaluate(progress),
	   config.PositionZ.Evaluate(progress)
   );

   Quaternion localRotation = Quaternion.Euler(
	   0,
	   config.RotationY.Evaluate(progress),
	   0
   );

   // Transform to world space
   Vector3 worldPosition = track.TransformPoint(localPosition);
   Quaternion worldRotation = track.rotation * localRotation;
   ```

4. **GPU instancing** renders all trains:
   - Each train's position/rotation calculated from its curve
   - Passed to GPU as instance data
   - Single draw call renders all trains!

## Example: Complete Train Animation

```csharp
public class TrainAnimationConfig : ScriptableObject
{
	[Header("Position Curves")]
	public AnimationCurve PositionX;
	public AnimationCurve PositionY;
	public AnimationCurve PositionZ;

	[Header("Rotation Curves")]
	public AnimationCurve RotationX;
	public AnimationCurve RotationY;
	public AnimationCurve RotationZ;

	[Header("Speed Curve")]
	public AnimationCurve SpeedMultiplier;

	[Header("Timing")]
	public float Duration = 1.0f;  // How long to traverse this segment

	/// <summary>
	/// Get train transform at normalized progress (0-1)
	/// </summary>
	public void EvaluateAt(float progress, out Vector3 position, out Quaternion rotation)
	{
		position = new Vector3(
			PositionX.Evaluate(progress),
			PositionY.Evaluate(progress),
			PositionZ.Evaluate(progress)
		);

		rotation = Quaternion.Euler(
			RotationX.Evaluate(progress),
			RotationY.Evaluate(progress),
			RotationZ.Evaluate(progress)
		);
	}

	/// <summary>
	/// Get speed multiplier at progress point
	/// </summary>
	public float GetSpeedAt(float progress)
	{
		return SpeedMultiplier.Evaluate(progress);
	}
}
```

## Key Takeaways

1. **AnimationCurve = Flexible Function**
   - Input: Time or progress (float)
   - Output: Value (float)
   - Smooth interpolation between keyframes

2. **Perfect for Train Animation**
   - Pre-calculate entire path as curves
   - Evaluate at any point instantly
   - Smooth acceleration/deceleration/turning

3. **Used with ChunkProgress**
   - Simulation tracks `ChunkProgress` (0-1)
   - AnimationCurve defines path shape
   - `curve.Evaluate(chunkProgress)` = position

4. **GPU Instancing Compatible**
   - Calculate position per train
   - Send to GPU as instance data
   - Efficient rendering of many trains

5. **Why You Don't See GameObjects**
   - Curves are evaluated on CPU
   - Results sent directly to GPU
   - No need for GameObject per train
   - Your simulation-based approach is correct!

## Visual Example: Train on Curved Track

```
Track Layout (Top View):

	Start ─→─→─→╮
				│
				↓
			  End

Animation Curves:

X Position:           Y Position:
0 ├─────────┐         0 ├────────────
  │         │           │
  │         └─ 10       └─ 0 (flat track)
  0        1.0          0        1.0

Rotation Y:           Speed:
0 ├─────────┐         1 ├──╲     ╱──
  │         │           │   ╲   ╱
90│         └─ 90     0 │    ╲─╱
  0        1.0          0        1.0
						(ease in/out)

As ChunkProgress goes 0→1:
- Position follows the arc
- Rotation smoothly turns 90°
- Speed eases in and out
```

**That's how AnimationCurve creates smooth, realistic train movement without per-train GameObjects!** 🚂✨
