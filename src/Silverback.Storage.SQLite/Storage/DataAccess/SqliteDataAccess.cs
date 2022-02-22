// Copyright (c) 2020 Sergio Aquilini
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
// TODO: Avoid unnecessary commit/rollback calls (for queries)?
internal class SqliteDataAccess
{
    private readonly string _connectionString;

    public SqliteDataAccess(string connectionString)
    {
        _connectionString = Check.NotNullOrEmpty(connectionString, nameof(connectionString));
    }

    public static SqliteParameter CreateParameter(string name, object? value) =>
        value == null ? new SqliteParameter(name, DBNull.Value) : new SqliteParameter(name, value);

    public Task<IReadOnlyCollection<T>> ExecuteQueryAsync<T>(
        Func<DbDataReader, T> projection,
        string sql,
        params SqliteParameter[] parameters) =>
        ExecuteCommandAsync(
            sql,
            parameters,
            async command =>
            {
                DbDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                await using ConfiguredAsyncDisposable disposableReader = reader.ConfigureAwait(false);

                return (IReadOnlyCollection<T>)await MapAsync(reader, projection).ToListAsync().ConfigureAwait(false);
            });

    public Task<T?> ExecuteScalarAsync<T>(string sql, params SqliteParameter[] parameters) =>
        ExecuteCommandAsync(
            sql,
            parameters,
            async command =>
            {
                object? result = await command.ExecuteScalarAsync().ConfigureAwait(false);

                if (result == DBNull.Value)
                    return default;

                return (T?)result;
            });

    public Task ExecuteNonQueryAsync(string sql, params SqliteParameter[] parameters) =>
        ExecuteNonQueryAsync(null, sql, parameters);

    public Task ExecuteNonQueryAsync(
        SilverbackContext? context,
        string sql,
        params SqliteParameter[] parameters) =>
        ExecuteCommandAsync(sql, parameters, command => command.ExecuteNonQueryAsync(), context);

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
    public async Task ExecuteNonQueryAsync<T>(
        IEnumerable<T> items,
        string sql,
        Func<T, SqliteParameter[]> parametersProvider,
        SilverbackContext? context = null)
    {
        (DbTransaction transaction, bool isNewTransaction) = await GetTransactionAsync(context).ConfigureAwait(false);

        try
        {
            DbCommand command = transaction.Connection!.CreateCommand();
            await using ConfiguredAsyncDisposable disposableCommand = command.ConfigureAwait(false);

            command.CommandText = sql;

            foreach (T item in items)
            {
                command.Parameters.Clear();
                command.Parameters.AddRange(parametersProvider.Invoke(item));
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            if (isNewTransaction)
                await transaction.CommitAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            if (isNewTransaction)
                await transaction.RollbackAndDisposeAsync().ConfigureAwait(false);

            throw;
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
    private async Task<T> ExecuteCommandAsync<T>(
        string sql,
        SqliteParameter[] parameters,
        Func<DbCommand, Task<T>> executeFunction,
        SilverbackContext? context = null)
    {
        (DbTransaction transaction, bool isNewTransaction) = await GetTransactionAsync(context).ConfigureAwait(false);

        try
        {
            DbCommand command = transaction.Connection!.CreateCommand();
            await using ConfiguredAsyncDisposable disposableCommand = command.ConfigureAwait(false);

            command.CommandText = sql;
            command.Parameters.AddRange(parameters);

            T result = await executeFunction.Invoke(command).ConfigureAwait(false);

            if (isNewTransaction)
                await transaction.CommitAndDisposeAsync().ConfigureAwait(false);

            return result;
        }
        catch (Exception)
        {
            if (isNewTransaction)
                await transaction.RollbackAndDisposeAsync().ConfigureAwait(false);

            throw;
        }
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Connection disposed by caller")]
    private async Task<(DbTransaction Transaction, bool IsNew)> GetTransactionAsync(SilverbackContext? context)
    {
        if (context != null && context.TryGetActiveDbTransaction(out SqliteTransaction? sqliteTransaction))
            return (sqliteTransaction!, false);

        SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted).ConfigureAwait(false);
        return (transaction, true);
    }
}
