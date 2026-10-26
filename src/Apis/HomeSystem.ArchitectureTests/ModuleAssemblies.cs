namespace HomeSystem.ArchitectureTests;

using System.Reflection;
using System.Text.RegularExpressions;

/// <summary>
/// Module assemblies (<c>{Module}.{Layer}</c>) reachable from the host's references, so a new module
/// is covered without touching the tests and third-party DLLs that happen to match the name
/// pattern (e.g. <c>OpenTelemetry.Api</c>) are never mistaken for one.
/// </summary>
internal static partial class ModuleAssemblies
{
    /// <summary>Layers a module can have; <c>Mcp</c> counts as an endpoint layer like <c>Api</c>.</summary>
    internal static readonly string[] Layers = ["Domain", "Application", "Contracts", "Infrastructure", "Api", "Mcp"];

    /// <summary>Every module assembly, keyed by module name then layer.</summary>
    internal static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, Assembly>> ByModule = Load();

    /// <summary>The module's assembly for <paramref name="layer"/>, or null when the module has none.</summary>
    internal static Assembly? Find(string module, string layer)
        => ByModule[module].GetValueOrDefault(layer);

    /// <summary>Each module that has <paramref name="layer"/>, with that layer's assembly.</summary>
    internal static IEnumerable<(string Module, Assembly Assembly)> WithLayer(string layer)
        => ByModule
            .Where(m => m.Value.ContainsKey(layer))
            .Select(m => (m.Key, m.Value[layer]));

    // Module names come from walking the host's references (the host composes every module, and
    // a NuGet DLL is never reached through our assemblies); layers come from the output folder,
    // because the compiler drops a reference nothing uses (Notifications.Contracts today).
    private static Dictionary<string, IReadOnlyDictionary<string, Assembly>> Load()
    {
        var modules = ReachableModuleNames();
        return Directory.GetFiles(AppContext.BaseDirectory, "*.dll")
            .Select(path => ModuleAssemblyName().Match(Path.GetFileNameWithoutExtension(path)))
            .Where(m => m.Success && modules.Contains(m.Groups["module"].Value))
            .GroupBy(m => m.Groups["module"].Value)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyDictionary<string, Assembly>)g.ToDictionary(
                    m => m.Groups["layer"].Value,
                    m => Assembly.Load(m.Value)));
    }

    private static HashSet<string> ReachableModuleNames()
    {
        var seen = new HashSet<string>();
        var pending = new Queue<Assembly>([typeof(Program).Assembly]);
        while (pending.TryDequeue(out var assembly))
        {
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name is { } name && IsModuleAssembly(name) && seen.Add(name))
                    pending.Enqueue(Assembly.Load(reference));
            }
        }

        return [.. seen.Select(name => ModuleAssemblyName().Match(name).Groups["module"].Value)];
    }

    private static bool IsModuleAssembly(string name)
        => ModuleAssemblyName().Match(name) is { Success: true } m && m.Groups["module"].Value is not "Shared";

    [GeneratedRegex(@"^(?<module>[A-Za-z]+)\.(?<layer>Domain|Application|Contracts|Infrastructure|Api|Mcp)$")]
    private static partial Regex ModuleAssemblyName();
}
