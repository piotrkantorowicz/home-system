using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Budget.UnitTests")]
[assembly: InternalsVisibleTo("Budget.Infrastructure")]
[assembly: InternalsVisibleTo("Budget.IntegrationTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Budget.Application;

using System.Reflection;

/// <summary>Stable handle to the Application assembly for handler scanning in DI.</summary>
public static class AssemblyReference
{
    /// <summary>The <c>Budget.Application</c> assembly.</summary>
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
