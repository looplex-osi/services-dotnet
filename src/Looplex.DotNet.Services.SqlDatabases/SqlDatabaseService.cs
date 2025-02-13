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
        await UseDatabaseIfPossible(); 
        connection.Open();
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    public Task<int> ExecuteAsync(string sql, object? parameters = null, IDbTransaction? transaction = null, CommandType? commandType = null)
    {
        return connection.ExecuteAsync(sql, parameters, transaction, commandType: commandType);
    }

    public Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null, CommandType? commandType = null)
    {
        return connection.QueryAsync<T>(sql, parameters, transaction, commandType: commandType);
    }

    public Task<IEnumerable<TR>> QueryAsync<TF, TS, TR>(string sql, Func<TF, TS, TR> map, object? parameters = null, IDbTransaction? transaction = null, string splitOn = "Id", CommandType? commandType = null)
    {
        return connection.QueryAsync(sql, map, param: parameters, transaction, splitOn: splitOn, commandType: commandType);
    }

    public Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null, CommandType? commandType = null)
    {
        return connection.QueryFirstOrDefaultAsync<T>(sql, parameters, transaction, commandType: commandType);
    }
    
    public async Task<(IEnumerable<T1>, IEnumerable<T2>)> QueryMultipleAsync<T1, T2>(string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = null)
    {
        var reader = await connection.QueryMultipleAsync(sql, param, transaction, null, commandType);
        
        return (await reader.ReadAsync<T1>(), await reader.ReadAsync<T2>());
    }

    public DbTransaction BeginTransaction()
    {
        return connection.BeginTransaction();
    }

    private Task UseDatabaseIfPossible()
    {
        return !string.IsNullOrEmpty(DatabaseName)
            ? connection.ExecuteAsync($"USE @databaseName", new { DatabaseName })
            : Task.CompletedTask;
    }
}