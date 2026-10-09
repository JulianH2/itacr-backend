using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;

namespace Itacr.Database.Migrator.Tests;

/// <summary>Builds and starts the SQL Server containers of the tests.</summary>
internal static class SqlServerContainers
{
    /// <summary>A fixed cumulative update, the same as compose.yaml, so the tests never change version on their own.</summary>
    public const string Image = "mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04";

    // CI runners have 8 GB and two SQL Server containers can run at once; each one would otherwise take most of it.
    private const string MemoryLimitVariable = "MSSQL_MEMORY_LIMIT_MB";
    private const string MemoryLimitMegabytes = "1536";

    private const int StartAttempts = 3;

    /// <summary>
    /// SQL Server on Linux sometimes stops right after it starts (an internal assert in NtumWaiter.cpp, seen on the CI
    /// runners), so a container that exits during startup is created and started again.
    /// </summary>
    public static async Task<MsSqlContainer> StartAsync(Func<MsSqlBuilder, MsSqlBuilder>? configure = null)
    {
        for (var attempt = 1; ; attempt++)
        {
            var builder = new MsSqlBuilder(Image).WithEnvironment(MemoryLimitVariable, MemoryLimitMegabytes);
            var container = (configure?.Invoke(builder) ?? builder).Build();
            try
            {
                await container.StartAsync();
                return container;
            }
            catch (ContainerNotRunningException) when (attempt < StartAttempts)
            {
                await container.DisposeAsync();
            }
        }
    }
}
