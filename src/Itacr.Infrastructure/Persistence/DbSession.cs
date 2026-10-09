using System.Data;
using Dapper;
using Itacr.Application.Common.Persistence;
using Microsoft.Data.SqlClient;

namespace Itacr.Infrastructure.Persistence;

public sealed record DatabaseConnection(string ConnectionString);

/// <summary>
/// One connection per request scope, opened on first use. Only opening the connection is retried (for transient
/// errors and the serverless resume); commands are never retried, so a command inside a transaction never runs twice.
/// </summary>
public sealed class DbSession(DatabaseConnection database) : IDbSession, IAsyncDisposable
{
    private const int OpenAttempts = 4;
    private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(20);

    private static readonly SqlRetryLogicBaseProvider OpenRetryProvider = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(
        new SqlRetryLogicOption
        {
            NumberOfTries = OpenAttempts,
            DeltaTime = FirstRetryDelay,
            MaxTimeInterval = MaxRetryDelay,
        });

    private SqlConnection? _connection;
    private SqlTransaction? _transaction;

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already running in this session; nested transactions are not supported.");
        }

        var connection = await GetOpenConnectionAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        _transaction = transaction;
        try
        {
            await work(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            // Disposing a transaction that was not committed rolls it back.
            _transaction = null;
        }
    }

    internal async Task<SqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            return _connection;
        }

        var connection = new SqlConnection(database.ConnectionString) { RetryLogicProvider = OpenRetryProvider };
        try
        {
            await connection.OpenAsync(cancellationToken);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }

        _connection = connection;
        return connection;
    }

    /// <summary>A stored procedure call that joins the session's transaction when one is running.</summary>
    internal CommandDefinition Procedure(string name, object? parameters, CancellationToken cancellationToken) =>
        new(name, parameters, _transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
