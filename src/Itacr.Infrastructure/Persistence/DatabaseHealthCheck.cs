using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Itacr.Infrastructure.Persistence;

/// <summary>
/// Healthy when the API can connect and run a procedure: it checks the login, the firewall, and the EXECUTE
/// permission. A failure throws, and the health check service reports it as Unhealthy (503).
/// </summary>
internal sealed class DatabaseHealthCheck(DbSession session) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteScalarAsync<int>(session.Procedure("dbo.usp_Health_Ping", null, cancellationToken));
        return HealthCheckResult.Healthy();
    }
}
