using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DietPlanner.UnitTests")]
[assembly: InternalsVisibleTo("DietPlanner.Infrastructure")]
[assembly: InternalsVisibleTo("DietPlanner.IntegrationTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace DietPlanner.Application;

using System.Reflection;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
