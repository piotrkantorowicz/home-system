namespace Notifications.Infrastructure.Persistence.Migrations;

using DbUp;
using DbUp.Engine;

internal static class DbUpRunner
{
    public static DatabaseUpgradeResult Run(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(DbUpRunner).Assembly,
                name => name.Contains(".Persistence.Migrations.", StringComparison.Ordinal)
                        && name.EndsWith(".sql", StringComparison.Ordinal))
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful) throw result.Error;
        return result;
    }
}
