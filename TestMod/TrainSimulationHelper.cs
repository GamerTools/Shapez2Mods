using Game.Core.Trains;
using Unity.Mathematics;
using Game.Core.Coordinates;
using System.Linq;
using ILogger = Core.Logging.ILogger;

/// <summary>
/// Helper to work with train simulation data when GameObjects aren't available
/// </summary>
public static class TrainSimulationHelper
{
    private static ILogger _logger;

    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates the smooth interpolated world position of a train based on simulation data.
    /// Uses ChunkProgress for smooth movement within track segments.
    /// </summary>
    public static (double2 position, float rotation) CalculateTrainPosition(TrainData trainData)
    {
        // Get the incoming and outgoing track positions
        var incoming = trainData.Head.Incoming;
        var outgoing = trainData.Head.Outgoing;

        // Convert to world coordinates
        var inPos = incoming.Position.ToOrigin_G();
        var outPos = outgoing.Position.ToOrigin_G();

        // Try to get ChunkProgress using reflection
        float progress = 0.5f; // Default to middle if we can't access it
        try
        {
            var progressData = trainData.Progress;
            var progressType = progressData.GetType();
            var chunkProgressField = progressType.GetField("ChunkProgress");

            if (chunkProgressField != null)
            {
                var progressValue = chunkProgressField.GetValue(progressData);
                if (progressValue is float floatProgress)
                {
                    progress = floatProgress;
                }
            }
        }
        catch
        {
            // If we can't access ChunkProgress, use outgoing position
            progress = 1.0f;
        }

        // Interpolate position between incoming and outgoing based on progress
        double2 interpolatedPos = new double2(
            math.lerp(inPos.x, outPos.x, progress),
            math.lerp(inPos.y, outPos.y, progress)
        );

        // Negate Y for camera coordinates
        double2 position = new double2(interpolatedPos.x, -interpolatedPos.y);

        // Calculate rotation based on direction
        // If turning, interpolate between incoming and outgoing rotation
        float inRotation = GetRotationFromDirection(incoming.Direction);
        float outRotation = GetRotationFromDirection(outgoing.Direction);

        // Handle rotation wrapping (e.g., 270° -> 0° should interpolate through 315°, not backwards)
        float rotationDiff = outRotation - inRotation;
        if (rotationDiff > 180f)
        {
            inRotation += 360f;
        }
        else if (rotationDiff < -180f)
        {
            outRotation += 360f;
        }

        float rotation = math.lerp(inRotation, outRotation, progress) % 360f;
        if (rotation < 0f) rotation += 360f;

        return (position, rotation);
    }

    /// <summary>
    /// Gets rotation in degrees for a given direction
    /// </summary>
    private static float GetRotationFromDirection(ChunkDirection direction)
    {
        switch (direction.Value)
        {
            case ChunkDirection.Serializable.North:
                return 0f;
            case ChunkDirection.Serializable.East:
                return 90f;
            case ChunkDirection.Serializable.South:
                return 180f;
            case ChunkDirection.Serializable.West:
                return 270f;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Logs detailed information about a train's simulation data
    /// </summary>
    public static void LogTrainSimulationData(TrainId trainId, TrainsSimulation trainSim)
    {
        if (trainId == TrainId.Invalid)
        {
            _logger?.Info.Log("Cannot log data for invalid train ID");
            return;
        }

        try
        {
            var trainData = trainSim.GetTrainData(trainId);

            _logger?.Info.Log($"=== Train Simulation Data for {trainId} ===");
            _logger?.Info.Log($"Head Incoming:");
            _logger?.Info.Log($"  Position: {trainData.Head.Incoming.Position}");
            _logger?.Info.Log($"  Direction: {trainData.Head.Incoming.Direction.Value}");
            _logger?.Info.Log($"  World Pos: {trainData.Head.Incoming.Position.ToOrigin_G()}");

            _logger?.Info.Log($"Head Outgoing:");
            _logger?.Info.Log($"  Position: {trainData.Head.Outgoing.Position}");
            _logger?.Info.Log($"  Direction: {trainData.Head.Outgoing.Direction.Value}");
            _logger?.Info.Log($"  World Pos: {trainData.Head.Outgoing.Position.ToOrigin_G()}");

            _logger?.Info.Log($"Progress: {trainData.Progress} (0.0 = incoming, 1.0 = outgoing)");

            // Calculate estimated position using ChunkProgress
            var (pos, rot) = CalculateTrainPosition(trainData);
            _logger?.Info.Log($"\nCalculated Smooth Position: {pos}");
            _logger?.Info.Log($"Calculated Smooth Rotation: {rot}°");
        }
        catch (System.Exception ex)
        {
            _logger?.Info.Log($"Error logging train data: {ex.Message}");
            _logger?.Info.Log($"Stack: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Checks if the game is using Unity ECS/DOTS
    /// </summary>
    public static void CheckForECS()
    {
        _logger?.Info.Log("=== Checking for Unity ECS/DOTS ===");

        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

        // Core ECS assemblies (must have these for full DOTS)
        var coreECS = new[]
        {
            "Unity.Entities",
            "Unity.Transforms"
        };

        // Supporting DOTS assemblies (can exist without full ECS)
        var supportingAssemblies = new[]
        {
            "Unity.Rendering.Hybrid",
            "Unity.Jobs",
            "Unity.Burst"
        };

        _logger?.Info.Log("\nCore ECS Assemblies:");
        bool hasFullECS = true;
        foreach (var ecsName in coreECS)
        {
            var found = assemblies.Any(a => a.GetName().Name == ecsName);
            _logger?.Info.Log($"  {ecsName}: {(found ? "FOUND" : "Not found")}");
            if (!found) hasFullECS = false;
        }

        _logger?.Info.Log("\nSupporting DOTS Assemblies:");
        foreach (var name in supportingAssemblies)
        {
            var found = assemblies.Any(a => a.GetName().Name == name);
            _logger?.Info.Log($"  {name}: {(found ? "FOUND" : "Not found")}");
        }

        _logger?.Info.Log("");
        if (hasFullECS)
        {
            _logger?.Info.Log("✓ Full Unity ECS/DOTS detected!");
            _logger?.Info.Log("Trains are likely Entities, not GameObjects.");
            _logger?.Info.Log("You'll need to query the EntityManager instead of using FindObjectsOfType.");
        }
        else
        {
            _logger?.Info.Log("✗ Unity.Entities NOT found - game does not use full ECS/DOTS.");
            _logger?.Info.Log("Unity.Jobs and Unity.Burst are present, but these are used for performance.");
            _logger?.Info.Log("Trains should be traditional GameObjects or use instanced rendering.");
            _logger?.Info.Log("\nTry these next:");
            _logger?.Info.Log("  1. Run 'listallnames' to see all GameObject names");
            _logger?.Info.Log("  2. Run 'listtrainobjects' to search for rendered objects");
            _logger?.Info.Log("  3. Run 'listasm Game.Core' to find train-related types");
        }
    }
}
