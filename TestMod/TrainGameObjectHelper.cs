using Game.Core.Trains;
using UnityEngine;
using System.Linq;
using ILogger = Core.Logging.ILogger;

/// <summary>
/// Helper class to find and track train GameObjects in the scene
/// </summary>
public static class TrainGameObjectHelper
{
    private static ILogger _logger;

    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to find a GameObject associated with a TrainId.
    /// This searches for GameObjects that might contain train-related components.
    /// </summary>
    public static GameObject FindTrainGameObject(TrainId trainId)
    {
        if (trainId == TrainId.Invalid)
        {
            return null;
        }

        // Strategy 1: Search for objects with "Train" in their name
        var allObjects = Object.FindObjectsOfType<GameObject>();
        var trainObjects = allObjects.Where(go => 
            go.name.Contains("Train") || 
            go.name.Contains("train") ||
            go.tag == "Train" // If trains have a tag
        ).ToArray();

        _logger?.Info.Log($"Found {trainObjects.Length} potential train GameObjects");

        // Strategy 2: Look for train-specific components
        // Common component patterns: TrainView, TrainRenderer, TrainAnimator, etc.
        foreach (var obj in trainObjects)
        {
            // Log the object and its components for debugging
            var components = obj.GetComponents<Component>();
            _logger?.Info.Log($"Train GameObject: {obj.name}, Components: {string.Join(", ", components.Select(c => c?.GetType().Name ?? "null"))}");
        }

        // For now, return the first train object found
        // You'll need to refine this based on actual game structure
        return trainObjects.FirstOrDefault();
    }

    /// <summary>
    /// Gets the position and rotation of a train GameObject
    /// </summary>
    public static (Vector3 position, Quaternion rotation) GetTrainTransform(GameObject trainObject)
    {
        if (trainObject == null)
        {
            return (Vector3.zero, Quaternion.identity);
        }

        Transform transform = trainObject.transform;
        return (transform.position, transform.rotation);
    }

    /// <summary>
    /// Gets the position and rotation, accounting for AnimationCurve-based movement
    /// </summary>
    public static (Vector3 position, Quaternion rotation) GetTrainAnimatedTransform(GameObject trainObject)
    {
        if (trainObject == null)
        {
            return (Vector3.zero, Quaternion.identity);
        }

        // Check for custom animation/curve components
        var allComponents = trainObject.GetComponents<MonoBehaviour>();
        foreach (var component in allComponents)
        {
            if (component == null) continue;

            var componentType = component.GetType();

            // Look for properties that might contain position/rotation info
            var positionProp = componentType.GetProperty("Position");
            var rotationProp = componentType.GetProperty("Rotation");

            if (positionProp != null || rotationProp != null)
            {
                _logger?.Info.Log($"Found animation component: {componentType.Name}");

                try
                {
                    Vector3 position = trainObject.transform.position;
                    Quaternion rotation = trainObject.transform.rotation;

                    if (positionProp != null)
                    {
                        var pos = positionProp.GetValue(component);
                        if (pos is Vector3 vec3Pos)
                        {
                            position = vec3Pos;
                        }
                    }

                    if (rotationProp != null)
                    {
                        var rot = rotationProp.GetValue(component);
                        if (rot is Quaternion quatRot)
                        {
                            rotation = quatRot;
                        }
                    }

                    return (position, rotation);
                }
                catch (System.Exception ex)
                {
                    _logger?.Info.Log($"Error reading animation properties: {ex.Message}");
                }
            }
        }

        // Fallback: return the transform values (these are updated by Unity animations)
        return (trainObject.transform.position, trainObject.transform.rotation);
    }

    /// <summary>
    /// Logs all train-related GameObjects in the scene for debugging
    /// </summary>
    public static void LogAllTrainObjects()
    {
        var allObjects = Object.FindObjectsOfType<GameObject>();

        // First, try common train-related names
        var trainObjects = allObjects.Where(go => 
            go.name.ToLower().Contains("train") || 
            go.name.ToLower().Contains("locomotive") ||
            go.name.ToLower().Contains("wagon") ||
            go.name.ToLower().Contains("cargo") ||
            go.name.ToLower().Contains("vehicle")
        ).ToArray();

        _logger?.Info.Log($"=== All Train-Related GameObjects ===");
        _logger?.Info.Log($"Total found by name: {trainObjects.Length}");

        foreach (var obj in trainObjects)
        {
            LogGameObjectDetails(obj);
        }

        // If no trains found, let's search for objects with mesh renderers that are actively moving
        if (trainObjects.Length == 0)
        {
            _logger?.Info.Log($"\n=== No trains found by name, searching all active objects with MeshRenderer ===");
            var rendereredObjects = allObjects.Where(go => 
                go.activeInHierarchy && 
                go.GetComponent<MeshRenderer>() != null
            ).ToArray();

            _logger?.Info.Log($"Total active rendered objects: {rendereredObjects.Length}");
            _logger?.Info.Log($"Listing first 50 for inspection:");

            for (int i = 0; i < System.Math.Min(50, rendereredObjects.Length); i++)
            {
                LogGameObjectDetails(rendereredObjects[i]);
            }
        }

        // Also search for all root GameObjects
        _logger?.Info.Log($"\n=== All Root GameObjects ===");
        var rootObjects = allObjects.Where(go => go.transform.parent == null).ToArray();
        _logger?.Info.Log($"Total root objects: {rootObjects.Length}");

        foreach (var obj in rootObjects)
        {
            _logger?.Info.Log($"\nRoot: {obj.name} (Active: {obj.activeInHierarchy}, Children: {obj.transform.childCount})");
        }
    }

    private static void LogGameObjectDetails(GameObject obj)
    {
        _logger?.Info.Log($"\nGameObject: {obj.name}");
        _logger?.Info.Log($"  Path: {GetGameObjectPath(obj)}");
        _logger?.Info.Log($"  Active: {obj.activeInHierarchy}");
        _logger?.Info.Log($"  Layer: {LayerMask.LayerToName(obj.layer)} ({obj.layer})");
        _logger?.Info.Log($"  Tag: {obj.tag}");
        _logger?.Info.Log($"  Position: {obj.transform.position}");
        _logger?.Info.Log($"  Rotation: {obj.transform.rotation.eulerAngles}");
        _logger?.Info.Log($"  Parent: {obj.transform.parent?.name ?? "null"}");
        _logger?.Info.Log($"  Children: {obj.transform.childCount}");

        var components = obj.GetComponents<Component>();
        _logger?.Info.Log($"  Components ({components.Length}):");
        foreach (var component in components)
        {
            if (component != null)
            {
                _logger?.Info.Log($"    - {component.GetType().FullName}");
            }
        }
    }

    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    /// <summary>
    /// Search for GameObjects by name pattern
    /// </summary>
    public static void SearchGameObjects(string pattern)
    {
        var allObjects = Object.FindObjectsOfType<GameObject>();
        var matching = allObjects.Where(go => 
            go.name.ToLower().Contains(pattern.ToLower())
        ).ToArray();

        _logger?.Info.Log($"=== GameObjects matching '{pattern}' ===");
        _logger?.Info.Log($"Total found: {matching.Length}");

        foreach (var obj in matching)
        {
            LogGameObjectDetails(obj);
        }
    }

    /// <summary>
    /// Lists all unique GameObject names in the scene (useful for discovering naming patterns)
    /// </summary>
    public static void ListAllUniqueGameObjectNames()
    {
        var allObjects = Object.FindObjectsOfType<GameObject>();
        var uniqueNames = allObjects
            .Select(go => go.name)
            .Distinct()
            .OrderBy(name => name)
            .ToArray();

        _logger?.Info.Log($"=== All Unique GameObject Names ({uniqueNames.Length}) ===");

        foreach (var name in uniqueNames)
        {
            var count = allObjects.Count(go => go.name == name);
            _logger?.Info.Log($"{name} (x{count})");
        }
    }
}
