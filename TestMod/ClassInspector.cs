using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Core.Dependency;
using ILogger = Core.Logging.ILogger;

namespace ShapezShifter.Utilities
{
    /// <summary>
    /// Utility class for inspecting and displaying class members using reflection
    /// </summary>
    public static class ClassInspector
    {
        private static ILogger _logger;

        /// <summary>
        /// Sets the logger to use for output
        /// </summary>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Recursively displays all fields and properties of a type or object instance
        /// </summary>
        /// <param name="type">The type to inspect</param>
        /// <param name="instance">Optional instance to get actual values from</param>
        /// <param name="maxDepth">Maximum recursion depth (default: 3)</param>
        public static void DisplayClassMembers(Type type, object instance = null, int maxDepth = 3)
        {
            var visited = new HashSet<Type>();
            DisplayClassMembersRecursive(type, instance, 0, maxDepth, "", visited);
        }

        private static void DisplayClassMembersRecursive(Type type, object instance, int currentDepth, int maxDepth, string indent, HashSet<Type> visited)
        {
            if (currentDepth > maxDepth)
            {
                _logger?.Info.Log($"{indent}[Max depth {maxDepth} reached]");
                return;
            }

            if (type == null)
            {
                _logger?.Info.Log($"{indent}[null type]");
                return;
            }

            // Avoid infinite recursion on circular references
            if (visited.Contains(type))
            {
                _logger?.Info.Log($"{indent}[Already visited: {type.Name}]");
                return;
            }

            visited.Add(type);

            _logger?.Info.Log($"{indent}=== {type.Name} ({type.FullName}) ===");

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            // Display Fields
            FieldInfo[] fields = type.GetFields(flags);
            if (fields.Length > 0)
            {
                _logger?.Info.Log($"{indent}--- Fields ({fields.Length}) ---");
                foreach (FieldInfo field in fields)
                {
                    string accessibility = field.IsPublic ? "public" : field.IsPrivate ? "private" : field.IsFamily ? "protected" : "internal";
                    string modifier = field.IsStatic ? "static " : "";
                    string readonlyMod = field.IsInitOnly ? "readonly " : "";

                    object value = null;
                    string valueStr = "";

                    // Try to get the actual value (static fields or instance fields when instance is provided)
                    bool shouldGetValue = field.IsStatic || instance != null;
                    if (shouldGetValue)
                    {
                        try
                        {
                            object target = field.IsStatic ? null : instance;
                            value = field.GetValue(target);
                            valueStr = value == null ? " = null" : $" = {FormatValue(value)}";
                        }
                        catch (Exception ex)
                        {
                            valueStr = $" [Error getting value: {ex.Message}]";
                        }
                    }

                    _logger?.Info.Log($"{indent}  {accessibility} {modifier}{readonlyMod}{field.FieldType.Name} {field.Name}{valueStr}");

                    // Recursively explore complex types
                    if (ShouldRecurse(field.FieldType) && value != null && currentDepth < maxDepth)
                    {
                        DisplayClassMembersRecursive(field.FieldType, value, currentDepth + 1, maxDepth, indent + "    ", new HashSet<Type>(visited));
                    }
                }
            }

            // Display Properties
            PropertyInfo[] properties = type.GetProperties(flags);
            if (properties.Length > 0)
            {
                _logger?.Info.Log($"{indent}--- Properties ({properties.Length}) ---");
                foreach (PropertyInfo prop in properties)
                {
                    string accessibility = "unknown";
                    if (prop.GetMethod != null)
                        accessibility = prop.GetMethod.IsPublic ? "public" : prop.GetMethod.IsPrivate ? "private" : prop.GetMethod.IsFamily ? "protected" : "internal";
                    else if (prop.SetMethod != null)
                        accessibility = prop.SetMethod.IsPublic ? "public" : prop.SetMethod.IsPrivate ? "private" : prop.SetMethod.IsFamily ? "protected" : "internal";

                    string modifier = "";
                    if (prop.GetMethod?.IsStatic ?? prop.SetMethod?.IsStatic ?? false)
                        modifier = "static ";

                    string getSet = "";
                    if (prop.CanRead) getSet += "get; ";
                    if (prop.CanWrite) getSet += "set; ";

                    object value = null;
                    string valueStr = "";

                    // Try to get the actual value if property has a getter (static properties or instance properties when instance is provided)
                    if (prop.CanRead)
                    {
                        bool isStatic = prop.GetMethod?.IsStatic ?? false;
                        bool shouldGetValue = isStatic || instance != null;

                        if (shouldGetValue)
                        {
                            try
                            {
                                object target = isStatic ? null : instance;
                                value = prop.GetValue(target);
                                valueStr = value == null ? " = null" : $" = {FormatValue(value)}";
                            }
                            catch (Exception ex)
                            {
                                valueStr = $" [Error getting value: {ex.Message}]";
                            }
                        }
                    }

                    _logger?.Info.Log($"{indent}  {accessibility} {modifier}{prop.PropertyType.Name} {prop.Name} {{ {getSet}}}{valueStr}");

                    // Recursively explore complex types
                    if (ShouldRecurse(prop.PropertyType) && value != null && currentDepth < maxDepth)
                    {
                        DisplayClassMembersRecursive(prop.PropertyType, value, currentDepth + 1, maxDepth, indent + "    ", new HashSet<Type>(visited));
                    }
                }
            }

            visited.Remove(type);
        }

        private static bool ShouldRecurse(Type type)
        {
            // Don't recurse into primitive types, strings, or common framework types
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
                return false;

            // Don't recurse into Unity math types
            if (type.Namespace != null && type.Namespace.StartsWith("Unity.Mathematics"))
                return false;

            // Don't recurse into system types
            if (type.Namespace != null && type.Namespace.StartsWith("System."))
                return false;

            return true;
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "null";

            Type type = value.GetType();

            // Handle strings
            if (type == typeof(string))
                return $"\"{value}\"";

            // Handle collections
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                var items = enumerable.Cast<object>().Take(5).ToList();
                if (items.Count == 0)
                    return "[]";

                string preview = string.Join(", ", items.Select(i => i?.ToString() ?? "null"));
                return $"[{preview}{(items.Count == 5 ? ", ..." : "")}]";
            }

            // Default ToString()
            return value.ToString();
        }

        /// <summary>
        /// Logs all classes (types) stored in a DependencyContainer
        /// </summary>
        /// <param name="container">The DependencyContainer to inspect</param>
        public static void LogDependencyContainerClasses(DependencyContainer container)
        {
            if (container == null)
            {
                _logger?.Info.Log("DependencyContainer is null");
                return;
            }

            var boundInstances = container.BoundInstancesByResolveType;

            var sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("=== DependencyContainer Registered Classes ===");
            sb.AppendLine($"Total registered types: {boundInstances.Count}");
            sb.AppendLine("");

            var sortedTypes = boundInstances.OrderBy(kvp => kvp.Key.FullName).ToList();

            foreach (var kvp in sortedTypes)
            {
                Type resolveType = kvp.Key;
                //object instance = kvp.Value;
                //Type instanceType = instance?.GetType();

                //string instanceTypeStr = instanceType != null ? instanceType.FullName : "null";
                sb.AppendLine($"{resolveType.FullName}");
                //sb.AppendLine($"  [{resolveType.FullName}]");
                //sb.AppendLine($"    -> Instance: {instanceTypeStr}");
            }

            sb.AppendLine("");
            sb.AppendLine("=== End of DependencyContainer Classes ===");

            _logger?.Info.Log(sb.ToString());
        }
    }
}
