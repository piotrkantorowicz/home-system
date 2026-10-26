namespace HomeSystem.ArchitectureTests;

using System.Reflection;
using System.Text.RegularExpressions;

/// <summary>
/// Module assemblies found next to the test binary (<c>{Module}.{Layer}.dll</c>), so a new module
/// is covered without touching the tests.
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

    private static Dictionary<string, IReadOnlyDictionary<string, Assembly>> Load()
        => Directory.GetFiles(AppContext.BaseDirectory, "*.dll")
            .Select(path => ModuleAssemblyName().Match(Path.GetFileNameWithoutExtension(path)))
            .Where(m => m.Success && m.Groups["module"].Value is not ("Shared" or "HomeSystem"))
            .GroupBy(m => m.Groups["module"].Value)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyDictionary<string, Assembly>)g.ToDictionary(
                    m => m.Groups["layer"].Value,
                    m => Assembly.Load(m.Value)));

    [GeneratedRegex(@"^(?<module>[A-Za-z]+)\.(?<layer>Domain|Application|Contracts|Infrastructure|Api|Mcp)$")]
    private static partial Regex ModuleAssemblyName();
}
