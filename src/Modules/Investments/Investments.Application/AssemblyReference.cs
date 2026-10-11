using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Investments.UnitTests")]
[assembly: InternalsVisibleTo("Investments.Infrastructure")]
[assembly: InternalsVisibleTo("Investments.IntegrationTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Investments.Application;

using System.Reflection;

/// <summary>Stable handle to the Application assembly for handler scanning in DI.</summary>
public static class AssemblyReference
{
    /// <summary>The <c>Investments.Application</c> assembly.</summary>
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
