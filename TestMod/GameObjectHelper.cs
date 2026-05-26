using UnityEngine;
using System.Linq;
using ILogger = Core.Logging.ILogger;

public static class GameObjectHelper
{
    private static ILogger _logger;

    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    public static void LogRootGameObjects()
    {
        // Also search for all root GameObjects
        _logger?.Info.Log($"\n=== All Root GameObjects ===");
        var allObjects = Object.FindObjectsOfType<GameObject>();
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
        //_logger?.Info.Log($"  Position: {obj.transform.position}");
        //_logger?.Info.Log($"  Rotation: {obj.transform.rotation.eulerAngles}");
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
    public static void ListObjectsByType<T>() where T : Object
    {
        var allObjects = Object.FindObjectsByType<T>(FindObjectsSortMode.None);

        _logger?.Info.Log($"=== All Objects of Type {typeof(T).Name} ({allObjects.Length}) ===");
        foreach (var obj in allObjects)
        {
            _logger?.Info.Log($"{obj.GetType().FullName} {obj.name}");
        }
    }
}
