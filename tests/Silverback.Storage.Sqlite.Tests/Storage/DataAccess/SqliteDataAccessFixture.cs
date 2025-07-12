// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Shouldly;
using Silverback.Storage.DataAccess;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Storage.DataAccess;

public sealed class SqliteDataAccessFixture : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;

    private readonly SqliteDataAccess _dataAccess;

    public SqliteDataAccessFixture()
    {
        _sqliteConnection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _sqliteConnection.Open();

        SqliteCommand command = _sqliteConnection.CreateCommand();
        command.CommandText =
            "CREATE TABLE Persons (Id INTEGER PRIMARY KEY, Name TEXT, Age INTEGER);" +
            "INSERT INTO Persons (Name, Age) VALUES ('Alice', 30);" +
            "INSERT INTO Persons (Name, Age) VALUES ('Bob', 25);" +
            "INSERT INTO Persons (Name, Age) VALUES ('Charlie', 19);";

        command.ExecuteNonQuery();

        _dataAccess = new SqliteDataAccess(_sqliteConnection.ConnectionString);
    }

    [Fact]
    public void ExecuteQuery_ShouldExecuteParametrizedQuery()
    {
        IReadOnlyCollection<Person> results = _dataAccess.ExecuteQuery<Person>(
            reader => new Person(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)),
            "SELECT * FROM Persons WHERE Age > @age",
            [new SqliteParameter("@age", 20)],
            TimeSpan.FromSeconds(1));

        results.Count.ShouldBe(2);
        results.ShouldContain(person => person.Name == "Alice");
        results.ShouldContain(person => person.Name == "Bob");
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldExecuteParametrizedQuery()
    {
        IDisposableAsyncEnumerable<Person> resultEnumerable = await _dataAccess.ExecuteQueryAsync<Person>(
            reader => new Person(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)),
            "SELECT * FROM Persons WHERE Age > @age",
            [new SqliteParameter("@age", 20)],
            TimeSpan.FromSeconds(1));

        List<Person> results = await resultEnumerable.ToListAsync();
        results.Count.ShouldBe(2);
        results.ShouldContain(person => person.Name == "Alice");
        results.ShouldContain(person => person.Name == "Bob");
    }

    [Fact]
    public void ExecuteScalar_ShouldExecuteParametrizedCommand()
    {
        long result = _dataAccess.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM Persons WHERE Age > @age",
            [new SqliteParameter("@age", 20)],
            TimeSpan.FromSeconds(1));

        result.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteScalarAsync_ShouldExecuteParametrizedCommand()
    {
        long result = await _dataAccess.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM Persons WHERE Age > @age",
            [new SqliteParameter("@age", 20)],
            TimeSpan.FromSeconds(1));

        result.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ShouldExecuteParametrizedCommand()
    {
        int affectedRows = await _dataAccess.ExecuteNonQueryAsync(
            "UPDATE Persons SET Age = Age + 1 WHERE Name = @name",
            [new SqliteParameter("@name", "Alice")],
            TimeSpan.FromSeconds(1));

        affectedRows.ShouldBe(1);

        IReadOnlyCollection<Person> results = _dataAccess.ExecuteQuery<Person>(
            reader => new Person(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)),
            "SELECT * FROM Persons WHERE Name = @name",
            [new SqliteParameter("@name", "Alice")],
            TimeSpan.FromSeconds(1));

        results.Count.ShouldBe(1);
        results.First().Age.ShouldBe(31);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ShouldExecuteParametrizedCommandForEachItem()
    {
        Person[] items =
        [
            new(-1, "Dave", 40),
            new(-1, "Eve", 35)
        ];

        await _dataAccess.ExecuteNonQueryAsync(
            items,
            "INSERT INTO Persons (Name, Age) VALUES (@name, @age)",
            [
                new SqliteParameter("@name", string.Empty),
                new SqliteParameter("@age", 0)
            ],
            (item, parameters) =>
            {
                parameters[0].Value = item.Name;
                parameters[1].Value = item.Age;
            },
            TimeSpan.FromSeconds(1));

        IReadOnlyCollection<Person> results = _dataAccess.ExecuteQuery<Person>(
            reader => new Person(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)),
            "SELECT * FROM Persons WHERE Name IN (@name1, @name2)",
            [
                new SqliteParameter("@name1", "Dave"),
                new SqliteParameter("@name2", "Eve")
            ],
            TimeSpan.FromSeconds(1));

        results.Count.ShouldBe(2);
        results.ShouldContain(person => person.Name == "Dave" && person.Age == 40);
        results.ShouldContain(person => person.Name == "Eve" && person.Age == 35);
    }

    public void Dispose() => _sqliteConnection.Dispose();

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test data structure")]
    private sealed record Person(int Id, string Name, int Age);
}
