// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using Npgsql;

namespace Silverback.Tests.Integration.E2E.TestHost.Database;

public sealed class PostgresContainerFixture : IDisposable
{
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromMinutes(1);

    private IContainerService? _postgresContainer;

    private string? _connectionString;

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Test code")]
    public async Task<string> GetNewConnectionStringAsync()
    {
        string connectionString = await EnsureContainerInitializedAsync();

        // Create a new database
        string databaseName = Guid.NewGuid().ToString("N");
        await using NpgsqlConnection connection = new(connectionString.Replace("{database}", "default", StringComparison.Ordinal));
        connection.Open();
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync();
        await connection.CloseAsync();

        return connectionString.Replace("{database}", databaseName, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (_postgresContainer == null)
            return;

        _postgresContainer.Stop();
        _postgresContainer.Dispose();
        _postgresContainer = null;
    }

    private static async Task WaitForConnectionAsync(string connectionString)
    {
        bool connected = false;
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (!connected && stopwatch.Elapsed < ConnectionTimeout)
        {
            try
            {
                await using NpgsqlConnection connection = new(connectionString);
                await connection.OpenAsync();
                connected = true;
            }
            catch (NpgsqlException)
            {
                await Task.Delay(100);
            }
        }

        if (!connected)
            throw new TimeoutException("Could not connect to the database");
    }

    private async Task<string> EnsureContainerInitializedAsync()
    {
        if (_connectionString != null)
            return _connectionString;

        _postgresContainer = new Builder().UseContainer()
            .UseImage("postgres")
            .ExposePort(5432)
            .WithEnvironment("POSTGRES_PASSWORD=silverback", "POSTGRES_DB=default")
            .WaitForPort("5432/tcp", 30_000)
            .Build()
            .Start();

        IPEndPoint hostExposedEndpoint = _postgresContainer.ToHostExposedEndpoint("5432/tcp");
        _connectionString = $"User ID=postgres;Password=silverback;" +
                            $"Host={hostExposedEndpoint.Address};Port={hostExposedEndpoint.Port};" +
                            "Database={database};Pooling=true;Maximum Pool Size=100;Connection Lifetime=0;";

        await WaitForConnectionAsync(_connectionString.Replace("{database}", "default", StringComparison.Ordinal));

        return _connectionString;
    }
}
