using System.Data;
using System.Data.Common;
using Dapper;
using Looplex.DotNet.Core.Application.Abstractions.Services;
using Microsoft.Data.SqlClient;

namespace Looplex.DotNet.Services.SqlDatabases;

public class SqlDatabasesService(SqlConnection connection) : ISqlDatabaseService
{
    public void OpenConnection()
    {
        connection.Open();
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    public Task<int> ExecuteAsync(string sql, object? parameters = null, IDbTransaction? transaction = null)
    {
        return connection.ExecuteAsync(sql, parameters, transaction);
    }

    public Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null)
    {
        return connection.QueryAsync<T>(sql, parameters, transaction);
    }

    public Task<IEnumerable<TR>> QueryAsync<TF, TS, TR>(string sql, Func<TF, TS, TR> map, object? parameters = null, IDbTransaction? transaction = null, string splitOn = "Id")
    {
        return connection.QueryAsync(sql, map, param: parameters, transaction, splitOn: splitOn);
    }

    public Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null)
    {
        return connection.QueryFirstOrDefaultAsync<T>(sql, parameters, transaction);
    }

    public DbTransaction BeginTransaction()
    {
        return connection.BeginTransaction();
    }
}