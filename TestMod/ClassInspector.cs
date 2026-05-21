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

        /// <summary>
        /// Lists all classes and types in an assembly
        /// </summary>
        /// <param name="assembly">The assembly to inspect</param>
        /// <param name="includeNamespaces">If true, groups by namespace (default: true)</param>
        /// <param name="includeNested">If true, includes nested types (default: false)</param>
        public static void LogAssemblyTypes(Assembly assembly, bool includeNamespaces = true, bool includeNested = false)
        {
            if (assembly == null)
            {
                _logger?.Info.Log("Assembly is null");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine($"=== Assembly: {assembly.GetName().Name} ===");
            sb.AppendLine($"Full Name: {assembly.FullName}");
            sb.AppendLine($"Location: {assembly.Location}");
            sb.AppendLine("");

            try
            {
                Type[] types = assembly.GetTypes();

                // Filter out nested types if requested
                if (!includeNested)
                {
                    types = types.Where(t => !t.IsNested).ToArray();
                }

                sb.AppendLine($"Total types: {types.Length}");
                sb.AppendLine("");

                if (includeNamespaces)
                {
                    // Group by namespace
                    var groupedTypes = types
                        .GroupBy(t => t.Namespace ?? "<no namespace>")
                        .OrderBy(g => g.Key);

                    foreach (var namespaceGroup in groupedTypes)
                    {
                        sb.AppendLine($"Namespace: {namespaceGroup.Key}");
                        sb.AppendLine(new string('-', 60));

                        var sortedTypes = namespaceGroup.OrderBy(t => t.Name);

                        foreach (Type type in sortedTypes)
                        {
                            string typeInfo = GetTypeInfo(type);
                            sb.AppendLine($"  {typeInfo}");
                        }

                        sb.AppendLine("");
                    }
                }
                else
                {
                    // Simple alphabetical list
                    var sortedTypes = types.OrderBy(t => t.FullName ?? t.Name);

                    foreach (Type type in sortedTypes)
                    {
                        string typeInfo = GetTypeInfo(type);
                        sb.AppendLine($"{typeInfo}");
                    }
                }

                sb.AppendLine("");
                sb.AppendLine($"=== End of {assembly.GetName().Name} Types ===");
            }
            catch (ReflectionTypeLoadException ex)
            {
                sb.AppendLine($"Error loading types: {ex.Message}");
                sb.AppendLine("Loader exceptions:");
                foreach (var loaderEx in ex.LoaderExceptions)
                {
                    sb.AppendLine($"  - {loaderEx?.Message}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error inspecting assembly: {ex.Message}");
            }

            _logger?.Info.Log(sb.ToString());
        }

        /// <summary>
        /// Gets a formatted string describing a type
        /// </summary>
        private static string GetTypeInfo(Type type)
        {
            var parts = new List<string>();

            // Type category
            if (type.IsInterface)
                parts.Add("interface");
            else if (type.IsEnum)
                parts.Add("enum");
            else if (type.IsValueType)
                parts.Add("struct");
            else if (type.IsAbstract && type.IsSealed)
                parts.Add("static class");
            else if (type.IsAbstract)
                parts.Add("abstract class");
            else if (type.IsSealed)
                parts.Add("sealed class");
            else if (type.IsClass)
                parts.Add("class");
            else
                parts.Add("type");

            // Visibility
            if (type.IsPublic || type.IsNestedPublic)
                parts.Add("public");
            else if (type.IsNestedPrivate)
                parts.Add("private");
            else if (type.IsNestedFamily)
                parts.Add("protected");
            else if (type.IsNestedAssembly)
                parts.Add("internal");
            else if (type.IsNotPublic)
                parts.Add("internal");

            // Generic indicator
            if (type.IsGenericType)
            {
                string genericName = type.Name.Split('`')[0];
                int genericArgCount = type.GetGenericArguments().Length;
                parts.Add($"{genericName}<{new string(',', genericArgCount - 1)}>");
            }
            else
            {
                parts.Add(type.Name);
            }

            return $"{string.Join(" ", parts.Take(parts.Count - 1))} {parts.Last()}";
        }

        /// <summary>
        /// Logs types from an assembly by name
        /// </summary>
        /// <param name="assemblyName">Name of the assembly (e.g., "Core", "Game.Core")</param>
        public static void LogAssemblyTypesByName(string assemblyName)
        {
            try
            {
                // Try to find the assembly in currently loaded assemblies
                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));

                if (assembly == null)
                {
                    _logger?.Info.Log($"Assembly '{assemblyName}' not found in loaded assemblies.");
                    _logger?.Info.Log("Available assemblies:");
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.GetName().Name))
                    {
                        _logger?.Info.Log($"  - {asm.GetName().Name}");
                    }
                    return;
                }

                LogAssemblyTypes(assembly);
            }
            catch (Exception ex)
            {
                _logger?.Info.Log($"Error loading assembly '{assemblyName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Generates MediaWiki formatted documentation for all game assemblies (excluding System and Unity)
        /// </summary>
        public static void LogAllAssembliesAsMediaWiki()
        {
            var sb = new StringBuilder();

            // Get all loaded assemblies, exclude System and Unity assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => {
                    string name = a.GetName().Name;
                    return !name.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                           !name.StartsWith("Unity", StringComparison.OrdinalIgnoreCase) &&
                           !name.StartsWith("Mono.", StringComparison.OrdinalIgnoreCase) &&
                           !name.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) &&
                           !name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(a => a.GetName().Name)
                .ToList();

            sb.AppendLine("= Shapez 2 Assembly Reference =");
            sb.AppendLine("");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("");
            sb.AppendLine($"Total assemblies documented: {assemblies.Count}");
            sb.AppendLine("");
            sb.AppendLine("== Table of Contents ==");
            sb.AppendLine("");

            // Generate TOC
            foreach (var assembly in assemblies)
            {
                string assemblyName = assembly.GetName().Name;
                sb.AppendLine($"* [[#{assemblyName}|{assemblyName}]]");
            }
            sb.AppendLine("");

            // Generate content for each assembly
            foreach (var assembly in assemblies)
            {
                try
                {
                    GenerateMediaWikiForAssembly(assembly, sb);
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Error processing assembly {assembly.GetName().Name}: {ex.Message}");
                    sb.AppendLine("");
                }
            }

            _logger?.Info.Log(sb.ToString());
        }

        /// <summary>
        /// Generates MediaWiki formatted documentation for a single assembly
        /// </summary>
        private static void GenerateMediaWikiForAssembly(Assembly assembly, StringBuilder sb)
        {
            string assemblyName = assembly.GetName().Name;

            sb.AppendLine($"== {assemblyName} ==");
            sb.AppendLine("");
            sb.AppendLine($"'''Assembly:''' {assemblyName}");
            sb.AppendLine("");
            sb.AppendLine($"'''Full Name:''' <code>{assembly.FullName}</code>");
            sb.AppendLine("");

            Type[] types;
            try
            {
                types = assembly.GetTypes()
                    .Where(t => !t.IsNested) // Exclude nested types for cleaner output
                    .Where(t => {
                        string ns = t.Namespace ?? "";
                        return !ns.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                               !ns.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase);
                    })
                    .ToArray();
            }
            catch (ReflectionTypeLoadException ex)
            {
                sb.AppendLine($"'''Warning:''' Could not load all types from this assembly.");
                sb.AppendLine("");
                types = ex.Types
                    .Where(t => t != null && !t.IsNested)
                    .Where(t => {
                        string ns = t.Namespace ?? "";
                        return !ns.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                               !ns.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase);
                    })
                    .ToArray();
            }

            sb.AppendLine($"'''Total Types:''' {types.Length}");
            sb.AppendLine("");

            // Create single table for all types
            sb.AppendLine("{| class=\"wikitable sortable\"");
            sb.AppendLine("! Type !! Namespace !! Category !! Visibility");

            var sortedTypes = types.OrderBy(t => t.Namespace ?? "<no namespace>").ThenBy(t => t.Name);

            foreach (Type type in sortedTypes)
            {
                sb.AppendLine("|-");

                // Type name (with code formatting)
                string typeName = GetSimpleTypeName(type);
                sb.Append($"| <code>{typeName}</code> || ");

                // Namespace
                string namespaceName = type.Namespace ?? "<no namespace>";
                sb.Append($"{namespaceName} || ");

                // Category
                string category = GetTypeCategory(type);
                sb.Append($"{category} || ");

                // Visibility
                string visibility = GetTypeVisibility(type);
                sb.AppendLine($"{visibility}");
            }

            sb.AppendLine("|}");
            sb.AppendLine("");

            sb.AppendLine("");
        }

        /// <summary>
        /// Gets a simple type name suitable for MediaWiki
        /// </summary>
        private static string GetSimpleTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                string baseName = type.Name.Split('`')[0];
                var genericArgs = type.GetGenericArguments();
                string args = string.Join(", ", genericArgs.Select(t => t.Name));
                return $"{baseName}&lt;{args}&gt;";
            }
            return type.Name;
        }

        /// <summary>
        /// Gets the type category for MediaWiki output
        /// </summary>
        private static string GetTypeCategory(Type type)
        {
            if (type.IsInterface)
                return "Interface";
            if (type.IsEnum)
                return "Enum";
            if (type.IsValueType)
                return "Struct";
            if (type.IsAbstract && type.IsSealed)
                return "Static Class";
            if (type.IsAbstract)
                return "Abstract Class";
            if (type.IsSealed)
                return "Sealed Class";
            if (type.IsClass)
                return "Class";
            return "Type";
        }

        /// <summary>
        /// Gets the type visibility for MediaWiki output
        /// </summary>
        private static string GetTypeVisibility(Type type)
        {
            if (type.IsPublic || type.IsNestedPublic)
                return "Public";
            if (type.IsNestedPrivate)
                return "Private";
            if (type.IsNestedFamily)
                return "Protected";
            if (type.IsNestedAssembly)
                return "Internal";
            if (type.IsNotPublic)
                return "Internal";
            return "Unknown";
        }
    }
}
