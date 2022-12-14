// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Silverback.Storage.Relational;
using Silverback.Util;

namespace Silverback.Storage.DataAccess;

// TODO: Generalize, split and move abstraction to Silverback.Storage.RelationalDatabase
internal class SqliteDataAccess
{
    private readonly string _connectionString;

    public SqliteDataAccess(string connectionString)
    {
        _connectionString = Check.NotNullOrEmpty(connectionString, nameof(connectionString));
    }

    public static SqliteParameter CreateParameter(string name, object? value) =>
        value == null ? new SqliteParameter(name, DBNull.Value) : new SqliteParameter(name, value);

    public IReadOnlyCollection<T> ExecuteQuery<T>(
        Func<DbDataReader, T> projection,
        string sql,
        params SqliteParameter[] parameters)
    {
        using DbCommand command = GetCommand(sql, parameters).DbCommand;
        using DbDataReader reader = command.ExecuteReader();

        return Map(reader, projection).ToList();
    }

    public async Task<IReadOnlyCollection<T>> ExecuteQueryAsync<T>(
        Func<DbDataReader, T> projection,
        string sql,
        params SqliteParameter[] parameters)
    {
        DbCommand command = (await GetCommandAsync(sql, parameters).ConfigureAwait(false)).DbCommand;
        await using ConfiguredAsyncDisposable disposableCommand = command.ConfigureAwait(false);
        DbDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        await using ConfiguredAsyncDisposable disposableReader = reader.ConfigureAwait(false);

        return await MapAsync(reader, projection).ToListAsync().ConfigureAwait(false);
    }

    public T? ExecuteScalar<T>(string sql, params SqliteParameter[] parameters)
    {
        DbCommand command = GetCommand(sql, parameters).DbCommand;

        object? result = command.ExecuteScalar();

        if (result == DBNull.Value)
            return default;

        return (T?)result;
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, params SqliteParameter[] parameters)
    {
        DbCommand command = (await GetCommandAsync(sql, parameters).ConfigureAwait(false)).DbCommand;
        await using ConfiguredAsyncDisposable disposableCommand = command.ConfigureAwait(false);

        object? result = await command.ExecuteScalarAsync().ConfigureAwait(false);

        if (result == DBNull.Value)
            return default;

        return (T?)result;
    }

    public Task ExecuteNonQueryAsync(string sql, params SqliteParameter[] parameters) => ExecuteNonQueryAsync(null, sql, parameters);

    public async Task ExecuteNonQueryAsync(SilverbackContext? context, string sql, params SqliteParameter[] parameters)
    {
        (DbCommand command, bool isNewTransaction) = await GetCommandAsync(sql, parameters, true, context).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable disposableCommand = command.ConfigureAwait(false);

        try
        {
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);

            if (isNewTransaction)
                await command.Transaction!.CommitAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            if (isNewTransaction)
                await command.Transaction!.RollbackAndDisposeAsync().ConfigureAwait(false);

            throw;
        }
    }

    public async Task ExecuteNonQueryAsync<T>(
        IEnumerable<T> items,
        string sql,
        SqliteParameter[] parameters,
        Action<T, SqliteParameter[]> parameterValuesProvider,
        SilverbackContext? context = null)
    {
        (DbCommand command, bool isNewTransaction) = await GetCommandAsync(sql, parameters, true, context).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable disposableCommand = command.ConfigureAwait(false);

        try
        {
            foreach (T item in items)
            {
                parameterValuesProvider.Invoke(item, parameters);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            if (isNewTransaction)
                await command.Transaction!.CommitAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            if (isNewTransaction)
                await command.Transaction!.RollbackAndDisposeAsync().ConfigureAwait(false);

            throw;
        }
    }

    private static IEnumerable<T> Map<T>(DbDataReader reader, Func<DbDataReader, T> projection)
    {
        while (reader.Read())
        {
            yield return projection(reader);
        }
    }

    private static async IAsyncEnumerable<T> MapAsync<T>(DbDataReader reader, Func<DbDataReader, T> projection)
    {
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            yield return projection(reader);
        }
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
    private (DbCommand DbCommand, bool IsNewTransaction) GetCommand(
        string sql,
        SqliteParameter[] parameters,
        bool beginTransaction = false,
        SilverbackContext? context = null)
    {
        ConnectionAndTransaction connectionAndTransaction = GetConnectionAndTransaction(context, beginTransaction);
        DbCommand command = connectionAndTransaction.Connection.CreateCommand();
        command.Transaction = connectionAndTransaction.Transaction;
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);

        return (command, connectionAndTransaction.IsNewTransaction);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
    private async Task<(DbCommand DbCommand, bool IsNewTransaction)> GetCommandAsync(
        string sql,
        SqliteParameter[] parameters,
        bool beginTransaction = false,
        SilverbackContext? context = null)
    {
        ConnectionAndTransaction connectionAndTransaction = await GetConnectionAndTransactionAsync(context, beginTransaction).ConfigureAwait(false);
        DbCommand command = connectionAndTransaction.Connection.CreateCommand();
        command.Transaction = connectionAndTransaction.Transaction;
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);

        return (command, connectionAndTransaction.IsNewTransaction);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Connection disposed by caller")]
    private ConnectionAndTransaction GetConnectionAndTransaction(SilverbackContext? context, bool beginTransaction)
    {
        DbTransaction? transaction = GetExistingTransaction(context);

        if (transaction != null)
            return new ConnectionAndTransaction(transaction.Connection!, transaction, false);

        SqliteConnection connection = new(_connectionString);
        connection.Open();

        if (beginTransaction)
            transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        return new ConnectionAndTransaction(connection, transaction, transaction != null);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Connection disposed by caller")]
    private async Task<ConnectionAndTransaction> GetConnectionAndTransactionAsync(SilverbackContext? context, bool beginTransaction)
    {
        DbTransaction? transaction = GetExistingTransaction(context);

        if (transaction != null)
            return new ConnectionAndTransaction(transaction.Connection!, transaction, false);

        SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        if (beginTransaction)
            transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted).ConfigureAwait(false);

        return new ConnectionAndTransaction(connection, transaction, transaction != null);
    }

    private DbTransaction? GetExistingTransaction(SilverbackContext? context)
    {
        if (context == null || !context.TryGetActiveDbTransaction(out SqliteTransaction? sqliteTransaction))
            return null;

        if (sqliteTransaction.Connection?.ConnectionString != _connectionString)
            throw new InvalidOperationException("The connection string of the active transaction does not match the configured connection string.");

        return sqliteTransaction;
    }

    private record ConnectionAndTransaction(DbConnection Connection, DbTransaction? Transaction, bool IsNewTransaction);
}
