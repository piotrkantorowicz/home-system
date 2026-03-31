using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DietPlanner.UnitTests")]

namespace DietPlanner.Application;

using System.Reflection;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
