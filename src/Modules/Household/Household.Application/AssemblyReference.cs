using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Household.UnitTests")]
[assembly: InternalsVisibleTo("Household.Infrastructure")]
[assembly: InternalsVisibleTo("Household.IntegrationTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Household.Application;

using System.Reflection;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
