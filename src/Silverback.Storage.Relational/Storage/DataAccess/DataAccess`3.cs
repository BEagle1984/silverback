// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Storage.DataAccess;

// TODO: Test directly (all implementations)
internal abstract class DataAccess<TConnection, TTransaction, TParameter>
    where TConnection : DbConnection
    where TTransaction : DbTransaction
    where TParameter : DbParameter
{
    private readonly string _connectionString;

    protected DataAccess(string connectionString)
    {
        _connectionString = Check.NotNullOrEmpty(connectionString, nameof(connectionString));
    }

    public IReadOnlyCollection<T> ExecuteQuery<T>(Func<DbDataReader, T> projection, string sql, TParameter[]? parameters, TimeSpan timeout)
    {
        using DbCommandWrapper wrapper = GetCommand(sql, parameters, timeout);
        using DbDataReader reader = wrapper.Command.ExecuteReader();

        return Map(reader, projection).ToList();
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed in the returned enumerable")]
    public async Task<IDisposableAsyncEnumerable<T>> ExecuteQueryAsync<T>(
        Func<DbDataReader, T> projection,
        string sql,
        TParameter[]? parameters,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        DbCommandWrapper wrapper = await GetCommandAsync(sql, parameters, timeout, cancellationToken: cancellationToken).ConfigureAwait(false);
        DbDataReader reader = await wrapper.Command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            IAsyncEnumerable<T> asyncEnumerable = MapAsync(reader, projection, cancellationToken);

            return new DataReaderAsyncEnumerable<T>(asyncEnumerable, reader, wrapper);
        }
        catch
        {
            await reader.DisposeAsync().ConfigureAwait(false);
            wrapper.Dispose();
            throw;
        }
    }

    public T? ExecuteScalar<T>(string sql, TParameter[]? parameters, TimeSpan timeout)
    {
        using DbCommandWrapper wrapper = GetCommand(sql, parameters, timeout);

        object? result = wrapper.Command.ExecuteScalar();

        return result == DBNull.Value ? default : (T?)result;
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        TParameter[]? parameters,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using DbCommandWrapper wrapper = await GetCommandAsync(sql, parameters, timeout, cancellationToken: cancellationToken).ConfigureAwait(false);

        object? result = await wrapper.Command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        if (result == DBNull.Value)
            return default;

        return (T?)result;
    }

    public async Task<int> ExecuteNonQueryAsync(
        string sql,
        TParameter[]? parameters,
        TimeSpan timeout,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default)
    {
        using DbCommandWrapper wrapper = await GetCommandAsync(sql, parameters, timeout, true, context, cancellationToken).ConfigureAwait(false);

        try
        {
            int affected = await wrapper.Command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await wrapper.CommitOwnedTransactionAsync(cancellationToken).ConfigureAwait(false);
            return affected;
        }
        catch (Exception)
        {
            await wrapper.RollbackOwnedTransactionAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task ExecuteNonQueryAsync<T>(
        IEnumerable<T> items,
        string sql,
        TParameter[] parameters,
        Action<T, TParameter[]> parameterValuesProvider,
        TimeSpan timeout,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default)
    {
        using DbCommandWrapper wrapper = await GetCommandAsync(sql, parameters, timeout, true, context, cancellationToken).ConfigureAwait(false);

        try
        {
            foreach (T item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                parameterValuesProvider.Invoke(item, parameters);
                await wrapper.Command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await wrapper.CommitOwnedTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            if (!cancellationToken.IsCancellationRequested)
                await wrapper.RollbackOwnedTransactionAsync(cancellationToken).ConfigureAwait(false);

            throw;
        }
    }

    public async Task ExecuteNonQueryAsync<T>(
        IAsyncEnumerable<T> items,
        string sql,
        TParameter[] parameters,
        Action<T, TParameter[]> parameterValuesProvider,
        TimeSpan timeout,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default)
    {
        using DbCommandWrapper wrapper = await GetCommandAsync(sql, parameters, timeout, true, context, cancellationToken).ConfigureAwait(false);

        try
        {
            await foreach (T item in items.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                parameterValuesProvider.Invoke(item, parameters);
                await wrapper.Command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await wrapper.CommitOwnedTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            if (!cancellationToken.IsCancellationRequested)
                await wrapper.RollbackOwnedTransactionAsync(cancellationToken).ConfigureAwait(false);

            throw;
        }
    }

    protected abstract TConnection CreateConnection(string connectionString);

    private static IEnumerable<T> Map<T>(DbDataReader reader, Func<DbDataReader, T> projection)
    {
        while (reader.Read())
        {
            yield return projection(reader);
        }
    }

    private static async IAsyncEnumerable<T> MapAsync<T>(
        DbDataReader reader,
        Func<DbDataReader, T> projection,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return projection(reader);
        }
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
    private DbCommandWrapper GetCommand(
        string sql,
        TParameter[]? parameters,
        TimeSpan timeout,
        bool beginTransaction = false,
        ISilverbackContext? context = null)
    {
        bool isNewConnection = false;
        bool isNewTransaction = false;
        DbConnection connection;

        DbTransaction? transaction = context.GetActiveDbTransaction<TTransaction>();

        if (transaction != null)
        {
            connection = transaction.Connection ?? throw new InvalidOperationException("Transaction.Connection is null");
        }
        else
        {
            connection = CreateConnection(_connectionString);
            isNewConnection = true;

            try
            {
                connection.Open();

                if (beginTransaction)
                {
                    transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
                    isNewTransaction = true;
                }
            }
            catch (Exception)
            {
                connection.Dispose();
                throw;
            }
        }

        DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;

        if (parameters != null)
            command.Parameters.AddRange(parameters);

        command.CommandTimeout = (int)timeout.TotalSeconds;

        return new DbCommandWrapper(command, connection, isNewConnection, transaction, isNewTransaction);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
    private async Task<DbCommandWrapper> GetCommandAsync(
        string sql,
        TParameter[]? parameters,
        TimeSpan timeout,
        bool beginTransaction = false,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default)
    {
        bool isNewConnection = false;
        bool isNewTransaction = false;
        DbConnection connection;

        DbTransaction? transaction = context.GetActiveDbTransaction<TTransaction>();

        if (transaction != null)
        {
            connection = transaction.Connection ?? throw new InvalidOperationException("Transaction.Connection is null");
        }
        else
        {
            connection = CreateConnection(_connectionString);
            isNewConnection = true;

            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                if (beginTransaction)
                {
                    transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
                    isNewTransaction = true;
                }
            }
            catch (Exception)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;

        if (parameters != null)
            command.Parameters.AddRange(parameters);

        command.CommandTimeout = (int)timeout.TotalSeconds;

        return new DbCommandWrapper(command, connection, isNewConnection, transaction, isNewTransaction);
    }

    private sealed class DbCommandWrapper : IDisposable
    {
        private readonly DbConnection _connection;

        private readonly bool _isConnectionOwner;

        private readonly DbTransaction? _transaction;

        private readonly bool _isTransactionOwner;

        public DbCommandWrapper(
            DbCommand command,
            DbConnection connection,
            bool isConnectionOwner,
            DbTransaction? transaction,
            bool isTransactionOwner)
        {
            Command = command;
            _connection = connection;
            _isConnectionOwner = isConnectionOwner;
            _transaction = transaction;
            _isTransactionOwner = isTransactionOwner;
        }

        public DbCommand Command { get; }

        public void Dispose()
        {
            Command.Dispose();

            if (_isTransactionOwner && _transaction != null)
                _transaction.Dispose();

            if (_isConnectionOwner)
                _connection.Dispose();
        }

        public ValueTask CommitOwnedTransactionAsync(CancellationToken cancellationToken = default) =>
            _isTransactionOwner && _transaction != null ? new ValueTask(_transaction.CommitAsync(cancellationToken)) : default;

        public ValueTask RollbackOwnedTransactionAsync(CancellationToken cancellationToken = default) =>
            _isTransactionOwner && _transaction != null ? new ValueTask(_transaction.RollbackAsync(cancellationToken)) : default;
    }

    private sealed class DataReaderAsyncEnumerable<T> : DisposableAsyncEnumerable<T>
    {
        private readonly DbDataReader _reader;

        private readonly DbCommandWrapper _commandWrapper;

        public DataReaderAsyncEnumerable(IAsyncEnumerable<T> wrappedAsyncEnumerable, DbDataReader reader, DbCommandWrapper commandWrapper)
            : base(wrappedAsyncEnumerable)
        {
            _reader = reader;
            _commandWrapper = commandWrapper;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _reader.Dispose();
            _commandWrapper.Dispose();
        }
    }
}
