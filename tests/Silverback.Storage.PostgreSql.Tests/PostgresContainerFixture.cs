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
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql;

public class PostgresContainerFixture : IAsyncLifetime
{
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromMinutes(1);

    private readonly IContainerService _postgresContainer;

    private readonly string _connectionString;

    public PostgresContainerFixture()
    {
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
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Test code")]
    public string GetNewConnectionString()
    {
        // Create a new database
        string databaseName = Guid.NewGuid().ToString("N");
        using NpgsqlConnection connection = new(_connectionString.Replace("{database}", "default", StringComparison.Ordinal));
        connection.Open();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        command.ExecuteNonQuery();
        connection.Close();

        return _connectionString.Replace("{database}", databaseName, StringComparison.Ordinal);
    }

    public Task InitializeAsync() => WaitForConnectionAsync();

    public Task DisposeAsync()
    {
        _postgresContainer.Stop();
        _postgresContainer.Dispose();
        return Task.CompletedTask;
    }

    private async Task WaitForConnectionAsync()
    {
        bool connected = false;
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (!connected && stopwatch.Elapsed < ConnectionTimeout)
        {
            try
            {
                await using NpgsqlConnection connection = new(_connectionString.Replace("{database}", "default", StringComparison.Ordinal));
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
}
