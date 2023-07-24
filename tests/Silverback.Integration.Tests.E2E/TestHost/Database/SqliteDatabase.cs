// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Silverback.Tests.Integration.E2E.TestHost.Database;

public sealed class SqliteDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDatabase()
    {
        _connection = new SqliteConnection(ConnectionString);
    }

    public string ConnectionString { get; } = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    public static async Task<SqliteDatabase> StartAsync()
    {
        SqliteDatabase database = new();
        await database._connection.OpenAsync();
        return database;
    }

    public void Dispose() => _connection.Dispose();
}
