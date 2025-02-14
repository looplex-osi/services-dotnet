using System.Data;
using System.Data.Common;
using Dapper;
using Looplex.DotNet.Core.Application.Abstractions.Services;
using Microsoft.Data.SqlClient;

namespace Looplex.DotNet.Services.SqlDatabases;

public class SqlDatabaseService(SqlConnection connection) : ISqlDatabaseService
{
    public string? DatabaseName { get; set; }

    public async Task OpenConnectionAsync()
    {
        connection.Open();
        await UseDatabaseIfPossible();
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    public Task<int> ExecuteAsync(string sql, object? parameters = null, IDbTransaction? transaction = null,
        CommandType? commandType = null)
    {
        return connection.ExecuteAsync(sql, parameters, transaction, commandType: commandType);
    }

    public Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null,
        CommandType? commandType = null)
    {
        return connection.QueryAsync<T>(sql, parameters, transaction, commandType: commandType);
    }

    public Task<IEnumerable<TR>> QueryAsync<TF, TS, TR>(string sql, Func<TF, TS, TR> map, object? parameters = null,
        IDbTransaction? transaction = null, string splitOn = "Id", CommandType? commandType = null)
    {
        return connection.QueryAsync(sql, map, param: parameters, transaction, splitOn: splitOn,
            commandType: commandType);
    }

    public Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null,
        IDbTransaction? transaction = null, CommandType? commandType = null)
    {
        return connection.QueryFirstOrDefaultAsync<T>(sql, parameters, transaction, commandType: commandType);
    }

    public async Task<IEnumerable<T1>> QueryMultipleAsync<T1>(string sql, object? param = null,
        IDbTransaction? transaction = null, CommandType? commandType = null)
    {
        var reader = await connection.QueryMultipleAsync(sql, param, transaction, null, commandType);
        return await reader.ReadAsync<T1>();
    }

    public async Task<(IEnumerable<T1>, IEnumerable<T2>)> QueryMultipleAsync<T1, T2>(string sql, object? param = null,
        IDbTransaction? transaction = null, CommandType? commandType = null)
    {
        var reader = await connection.QueryMultipleAsync(sql, param, transaction, null, commandType);
        return (await reader.ReadAsync<T1>(), await reader.ReadAsync<T2>());
    }

    public async Task<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>)> QueryMultipleAsync<T1, T2, T3>(string sql,
        object? param = null, IDbTransaction? transaction = null, CommandType? commandType = null)
    {
        var reader = await connection.QueryMultipleAsync(sql, param, transaction, null, commandType);
        return (await reader.ReadAsync<T1>(), await reader.ReadAsync<T2>(), await reader.ReadAsync<T3>());
    }

    public async Task<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>)>
        QueryMultipleAsync<T1, T2, T3, T4>(string sql, object? param = null, IDbTransaction? transaction = null,
            CommandType? commandType = null)
    {
        var reader = await connection.QueryMultipleAsync(sql, param, transaction, null, commandType);
        return (await reader.ReadAsync<T1>(), await reader.ReadAsync<T2>(), await reader.ReadAsync<T3>(),
            await reader.ReadAsync<T4>());
    }

    public async Task<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>)>
        QueryMultipleAsync<T1, T2, T3, T4, T5>(string sql, object? param = null, IDbTransaction? transaction = null,
            CommandType? commandType = null)
    {
        var reader = await connection.QueryMultipleAsync(sql, param, transaction, null, commandType);
        return (await reader.ReadAsync<T1>(), await reader.ReadAsync<T2>(), await reader.ReadAsync<T3>(),
            await reader.ReadAsync<T4>(), await reader.ReadAsync<T5>());
    }

    public async
        Task<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>)>
        QueryMultipleAsync<T1, T2, T3, T4, T5, T6>(string sql, object? param = null, IDbTransaction? transaction = null,
            CommandType? commandType = null)
    {
        var reader = await connection.QueryMultipleAsync(sql, param, transaction, null, commandType);
        return (await reader.ReadAsync<T1>(), await reader.ReadAsync<T2>(), await reader.ReadAsync<T3>(),
            await reader.ReadAsync<T4>(), await reader.ReadAsync<T5>(), await reader.ReadAsync<T6>());
    }

    public async
        Task<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>,
            IEnumerable<T7>)> QueryMultipleAsync<T1, T2, T3, T4, T5, T6, T7>(string sql, object? param = null,
            IDbTransaction? transaction = null, CommandType? commandType = null)
    {
        var reader = await connection.QueryMultipleAsync(sql, param, transaction, null, commandType);
        return (await reader.ReadAsync<T1>(), await reader.ReadAsync<T2>(), await reader.ReadAsync<T3>(),
            await reader.ReadAsync<T4>(), await reader.ReadAsync<T5>(), await reader.ReadAsync<T6>(),
            await reader.ReadAsync<T7>());
    }

    public async
        Task<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>,
            IEnumerable<T7>, IEnumerable<T8>)> QueryMultipleAsync<T1, T2, T3, T4, T5, T6, T7, T8>(string sql,
            object? param = null, IDbTransaction? transaction = null, CommandType? commandType = null)
    {
        var reader = await connection.QueryMultipleAsync(sql, param, transaction, null, commandType);
        return (await reader.ReadAsync<T1>(), await reader.ReadAsync<T2>(), await reader.ReadAsync<T3>(),
            await reader.ReadAsync<T4>(), await reader.ReadAsync<T5>(), await reader.ReadAsync<T6>(),
            await reader.ReadAsync<T7>(), await reader.ReadAsync<T8>());
    }

    public async
        Task<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>,
            IEnumerable<T7>, IEnumerable<T8>, IEnumerable<T9>)> QueryMultipleAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = null)
    {
        var reader = await connection.QueryMultipleAsync(sql, param, transaction, null, commandType);
        return (await reader.ReadAsync<T1>(), await reader.ReadAsync<T2>(), await reader.ReadAsync<T3>(),
            await reader.ReadAsync<T4>(), await reader.ReadAsync<T5>(), await reader.ReadAsync<T6>(),
            await reader.ReadAsync<T7>(), await reader.ReadAsync<T8>(), await reader.ReadAsync<T9>());
    }

    public DbTransaction BeginTransaction()
    {
        return connection.BeginTransaction();
    }

    private Task UseDatabaseIfPossible()
    {
        return !string.IsNullOrEmpty(DatabaseName)
            ? connection.ExecuteAsync($"USE @DatabaseName", new { DatabaseName })
            : Task.CompletedTask;
    }
}