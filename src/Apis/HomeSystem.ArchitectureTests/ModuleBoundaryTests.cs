namespace HomeSystem.ArchitectureTests;

using System.Reflection;
using NetArchTest.Rules;

/// <summary>Module and layer boundaries (CLAUDE.md rule 2, backend-module-structure.md § Layer Dependency Rules).</summary>
public sealed class ModuleBoundaryTests
{
    private static readonly string[] DataAccessNamespaces = ["Microsoft.EntityFrameworkCore", "Dapper", "Npgsql"];

    /// <summary>The loader finds the modules at all — guards every other test against passing vacuously.</summary>
    [Fact]
    public void ModuleAssemblies_Always_DiscoversEveryModule()
        => ModuleAssemblies.ByModule.Keys.ShouldBe(
            ["Assistant", "Budget", "DietPlanner", "Household", "Investments", "Notifications", "Operations"],
            ignoreOrder: true);

    /// <summary>A module reaches another module only through its Contracts.</summary>
    [Fact]
    public void Modules_ReferencingOtherModules_UseOnlyContracts()
    {
        var violations = new List<string>();
        foreach (var (module, assemblies) in ModuleAssemblies.ByModule)
        {
            string[] forbidden =
            [
                .. ModuleAssemblies.ByModule.Keys
                    .Where(other => other != module)
                    .SelectMany(other => ModuleAssemblies.Layers.Where(l => l != "Contracts").Select(l => $"{other}.{l}")),
            ];
            foreach (var assembly in assemblies.Values)
                violations.AddRange(FailingTypes(assembly, forbidden));
        }

        violations.ShouldBeEmpty();
    }

    /// <summary>Domain knows nothing about persistence or the outer layers.</summary>
    [Fact]
    public void Domain_Always_DependsOnNoOuterLayerOrDataAccess()
    {
        string[] violations =
        [
            .. ModuleAssemblies.WithLayer("Domain").SelectMany(d => FailingTypes(
                d.Assembly,
                [.. OuterLayers(d.Module, "Application", "Infrastructure", "Api", "Mcp"), .. DataAccessNamespaces])),
        ];

        violations.ShouldBeEmpty();
    }

    /// <summary>Application depends inward only: never on Infrastructure or the endpoint layers.</summary>
    [Fact]
    public void Application_Always_DependsOnNoInfrastructureOrEndpoints()
    {
        string[] violations =
        [
            .. ModuleAssemblies.WithLayer("Application").SelectMany(a => FailingTypes(
                a.Assembly,
                [.. OuterLayers(a.Module, "Infrastructure", "Api", "Mcp")])),
        ];

        violations.ShouldBeEmpty();
    }

    private static IEnumerable<string> OuterLayers(string module, params string[] layers)
        => layers.Select(layer => $"{module}.{layer}");

    // One check per target so the failure names the dependency, not the whole forbidden list.
    private static IEnumerable<string> FailingTypes(Assembly assembly, string[] forbidden)
        => forbidden.SelectMany(target =>
        {
            var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOn(target).GetResult();
            return result.IsSuccessful ? [] : result.FailingTypeNames.Select(type => $"{type} → {target}");
        });
}
