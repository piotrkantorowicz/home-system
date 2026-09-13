using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DietPlanner.UnitTests")]
[assembly: InternalsVisibleTo("DietPlanner.Infrastructure")]
[assembly: InternalsVisibleTo("DietPlanner.IntegrationTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace DietPlanner.Application;

using System.Reflection;

/// <summary>Stable handle to the Application assembly for handler scanning in DI.</summary>
public static class AssemblyReference
{
    /// <summary>The <c>DietPlanner.Application</c> assembly.</summary>
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
