// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Shouldly;
using Silverback.Storage;
using Silverback.Storage.DataAccess;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Storage.Relational.Storage.DataAccess;

public sealed class DataAccessTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly SqliteConnection _sqliteConnection;

    private readonly TestDataAccess _dataAccess;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public DataAccessTests()
    {
        _sqliteConnection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _sqliteConnection.Open();

        using SqliteCommand command = _sqliteConnection.CreateCommand();
        command.CommandText =
            "CREATE TABLE Persons (Id INTEGER PRIMARY KEY, Name TEXT, Age INTEGER);" +
            "INSERT INTO Persons (Id, Name, Age) VALUES (1, 'Alice', 30);";
        command.ExecuteNonQuery();

        _dataAccess = new TestDataAccess(_sqliteConnection.ConnectionString);
    }

    [Fact]
    public void ExecuteQuery_ShouldThrowAndDisposeConnection_WhenConnectionCannotBeOpened()
    {
        TestDataAccess dataAccess = new($"Data Source={Guid.NewGuid():N}-missing.db;Mode=ReadOnly");

        Action act = () => dataAccess.ExecuteQuery(reader => reader.GetInt32(0), "SELECT 1", null, Timeout);

        act.ShouldThrow<SqliteException>();
        dataAccess.DisposedConnections.ShouldContain(dataAccess.CreatedConnections.ShouldHaveSingleItem());
    }

    [Fact]
    public void ExecuteScalar_ShouldReturnDefault_WhenResultIsDbNull()
    {
        string? result = _dataAccess.ExecuteScalar<string>("SELECT NULL", null, Timeout);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldDisposeConnection_WhenEnumerableIsDisposed()
    {
        IDisposableAsyncEnumerable<string> resultEnumerable = await _dataAccess.ExecuteQueryAsync(
            reader => reader.GetString(0),
            "SELECT Name FROM Persons",
            null,
            Timeout);

        SqliteConnection connection = _dataAccess.CreatedConnections.ShouldHaveSingleItem();
        connection.State.ShouldBe(ConnectionState.Open);

        resultEnumerable.Dispose();

        connection.State.ShouldBe(ConnectionState.Closed);
        _dataAccess.DisposedConnections.ShouldContain(connection);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ShouldRollbackOwnedTransaction_WhenCommandFails()
    {
        Func<Task> act = () => _dataAccess.ExecuteNonQueryAsync(
            "INSERT INTO Persons (Id, Name, Age) VALUES (100, 'Dave', 40);" +
            "INSERT INTO Persons (Id, Name, Age) VALUES (100, 'Eve', 35);",
            null,
            Timeout);

        await act.ShouldThrowAsync<SqliteException>();
        GetPersonNames().ShouldBe(["Alice"]);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ShouldNotRollbackEnlistedTransaction_WhenCommandFails()
    {
        await using SqliteConnection connection = new(_sqliteConnection.ConnectionString);
        await connection.OpenAsync();
        await using SqliteTransaction transaction = connection.BeginTransaction();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        context.EnlistDbTransaction(transaction);

        await _dataAccess.ExecuteNonQueryAsync(
            "INSERT INTO Persons (Id, Name, Age) VALUES (100, 'Dave', 40)",
            null,
            Timeout,
            context);
        Func<Task> act = () => _dataAccess.ExecuteNonQueryAsync(
            "INSERT INTO Persons (Id, Name, Age) VALUES (100, 'Eve', 35)",
            null,
            Timeout,
            context);

        await act.ShouldThrowAsync<SqliteException>();
        transaction.Connection.ShouldBe(connection);
        await transaction.CommitAsync();
        GetPersonNames().ShouldBe(["Alice", "Dave"]);
        _dataAccess.CreatedConnections.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ShouldThrow_WhenEnlistedTransactionHasNoConnection()
    {
        await using SqliteConnection connection = new(_sqliteConnection.ConnectionString);
        await connection.OpenAsync();
        await using SqliteTransaction transaction = connection.BeginTransaction();
        await transaction.CommitAsync();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        context.EnlistDbTransaction(transaction);

        Func<Task> act = () => _dataAccess.ExecuteNonQueryAsync(
            "INSERT INTO Persons (Id, Name, Age) VALUES (100, 'Dave', 40)",
            null,
            Timeout,
            context);

        Exception exception = await act.ShouldThrowAsync<InvalidOperationException>();
        exception.Message.ShouldBe("Transaction.Connection is null");
        GetPersonNames().ShouldBe(["Alice"]);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ShouldRollbackOwnedTransaction_WhenCommandFailsForAnItem()
    {
        Person[] items =
        [
            new(100, "Dave", 40),
            new(100, "Eve", 35)
        ];

        Func<Task> act = () => _dataAccess.ExecuteNonQueryAsync(
            items,
            "INSERT INTO Persons (Id, Name, Age) VALUES (@id, @name, @age)",
            CreateInsertParameters(),
            SetInsertParameters,
            Timeout);

        await act.ShouldThrowAsync<SqliteException>();
        GetPersonNames().ShouldBe(["Alice"]);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ShouldThrowWithoutCommitting_WhenCanceledWhileProcessingItems()
    {
        Person[] items =
        [
            new(100, "Dave", 40),
            new(101, "Eve", 35)
        ];

        Func<Task> act = () => _dataAccess.ExecuteNonQueryAsync(
            items,
            "INSERT INTO Persons (Id, Name, Age) VALUES (@id, @name, @age)",
            CreateInsertParameters(),
            (item, parameters) =>
            {
                SetInsertParameters(item, parameters);
                _cancellationTokenSource.Cancel();
            },
            Timeout,
            cancellationToken: _cancellationTokenSource.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
        GetPersonNames().ShouldBe(["Alice"]);
        _dataAccess.DisposedConnections.ShouldContain(_dataAccess.CreatedConnections.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ShouldRollbackOwnedTransaction_WhenCommandFailsForAnAsyncItem()
    {
        Person[] items =
        [
            new(100, "Dave", 40),
            new(100, "Eve", 35)
        ];

        Func<Task> act = () => _dataAccess.ExecuteNonQueryAsync(
            ToAsyncEnumerable(items),
            "INSERT INTO Persons (Id, Name, Age) VALUES (@id, @name, @age)",
            CreateInsertParameters(),
            SetInsertParameters,
            Timeout);

        await act.ShouldThrowAsync<SqliteException>();
        GetPersonNames().ShouldBe(["Alice"]);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ShouldThrowWithoutCommitting_WhenCanceledWhileProcessingAsyncItems()
    {
        Person[] items =
        [
            new(100, "Dave", 40),
            new(101, "Eve", 35)
        ];

        Func<Task> act = () => _dataAccess.ExecuteNonQueryAsync(
            ToAsyncEnumerable(items),
            "INSERT INTO Persons (Id, Name, Age) VALUES (@id, @name, @age)",
            CreateInsertParameters(),
            (item, parameters) =>
            {
                SetInsertParameters(item, parameters);
                _cancellationTokenSource.Cancel();
            },
            Timeout,
            cancellationToken: _cancellationTokenSource.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
        GetPersonNames().ShouldBe(["Alice"]);
        _dataAccess.DisposedConnections.ShouldContain(_dataAccess.CreatedConnections.ShouldHaveSingleItem());
    }

    public void Dispose()
    {
        _sqliteConnection.Dispose();
        _cancellationTokenSource.Dispose();
    }

    private static SqliteParameter[] CreateInsertParameters() =>
    [
        new("@id", 0),
        new("@name", ""),
        new("@age", 0)
    ];

    private static void SetInsertParameters(Person person, SqliteParameter[] parameters)
    {
        parameters[0].Value = person.Id;
        parameters[1].Value = person.Name;
        parameters[2].Value = person.Age;
    }

    private static async IAsyncEnumerable<Person> ToAsyncEnumerable(
        IEnumerable<Person> persons,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (Person person in persons)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return person;
        }
    }

    private List<string> GetPersonNames()
    {
        using SqliteCommand command = _sqliteConnection.CreateCommand();
        command.CommandText = "SELECT Name FROM Persons ORDER BY Id";
        using SqliteDataReader reader = command.ExecuteReader();

        List<string> names = [];

        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test data structure")]
    private sealed record Person(int Id, string Name, int Age);

    private sealed class TestDataAccess : DataAccess<SqliteConnection, SqliteTransaction, SqliteParameter>
    {
        public TestDataAccess(string connectionString)
            : base(connectionString)
        {
        }

        public List<SqliteConnection> CreatedConnections { get; } = [];

        public List<SqliteConnection> DisposedConnections { get; } = [];

        protected override SqliteConnection CreateConnection(string connectionString)
        {
            SqliteConnection connection = new(connectionString);
            connection.Disposed += (_, _) => DisposedConnections.Add(connection);
            CreatedConnections.Add(connection);

            return connection;
        }
    }
}
