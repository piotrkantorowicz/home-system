using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Household.UnitTests")]
[assembly: InternalsVisibleTo("Household.Infrastructure")]
[assembly: InternalsVisibleTo("Household.IntegrationTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Household.Application;

using System.Reflection;

/// <summary>Stable handle to the Application assembly for handler scanning in DI.</summary>
public static class AssemblyReference
{
    /// <summary>The <c>Household.Application</c> assembly.</summary>
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
