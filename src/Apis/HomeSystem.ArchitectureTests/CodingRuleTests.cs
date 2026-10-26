namespace HomeSystem.ArchitectureTests;

using System.Reflection;
using System.Runtime.CompilerServices;
using NetArchTest.Rules;

/// <summary>Non-negotiable coding rules from CLAUDE.md that the compiler alone does not enforce.</summary>
public sealed class CodingRuleTests
{
    // NetArchTest matches generic types by their metadata name, arity included.
    private static readonly string[] HandlerInterfaces =
    [
        "Shared.Abstractions.Cqrs.ICommandHandler`1",
        "Shared.Abstractions.Cqrs.ICommandHandler`2",
        "Shared.Abstractions.Cqrs.IQueryHandler`2",
    ];

    /// <summary>Rule 8: every concrete class is sealed. Generated EF migrations are exempt.</summary>
    [Fact]
    public void ConcreteClasses_InModules_AreSealed()
    {
        string[] unsealed =
        [
            .. ModuleAssemblies.ByModule.Values
                .SelectMany(layers => layers.Values)
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false, IsSealed: false })
                .Where(t => !IsGenerated(t))
                .Select(t => t.FullName ?? t.Name),
        ];

        unsealed.ShouldBeEmpty();
    }

    /// <summary>Rule 9: endpoints (REST and MCP) reach handlers only through the dispatchers.</summary>
    [Fact]
    public void Endpoints_Always_DependOnNoHandlerInterface()
    {
        string[] violations =
        [
            .. ModuleAssemblies.ByModule.Keys
                .SelectMany(module => new[] { ModuleAssemblies.Find(module, "Api"), ModuleAssemblies.Find(module, "Mcp") })
                .OfType<Assembly>()
                .Append(typeof(Program).Assembly)
                .Select(assembly => Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(HandlerInterfaces).GetResult())
                .Where(result => !result.IsSuccessful)
                .SelectMany(result => result.FailingTypeNames),
        ];

        violations.ShouldBeEmpty();
    }

    private static bool IsGenerated(Type type)
        => type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
           || type.Name.Contains('<', StringComparison.Ordinal)
           || type.Namespace?.EndsWith(".Migrations", StringComparison.Ordinal) == true;
}
