// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using Npgsql;

namespace Silverback.Tests.Integration.E2E.TestHost.Database;

public sealed class PostgreSqlDatabase : IDisposable
{
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromMinutes(1);

    private readonly IContainerService _postgresContainer;

    private PostgreSqlDatabase()
    {
        _postgresContainer = new Builder().UseContainer()
            .UseImage("postgres")
            .ExposePort(5432)
            .WithEnvironment("POSTGRES_PASSWORD=silverback", "POSTGRES_DB=silverback-storage-tests")
            .WaitForPort("5432/tcp", 30_000)
            .Build()
            .Start();

        IPEndPoint hostExposedEndpoint = _postgresContainer.ToHostExposedEndpoint("5432/tcp");
        ConnectionString = $"User ID=postgres;Password=silverback;" +
                           $"Host={hostExposedEndpoint.Address};Port={hostExposedEndpoint.Port};" +
                           $"Database=silverback-storage-tests;Pooling=true;Maximum Pool Size=100;Connection Lifetime=0;";
    }

    public string ConnectionString { get; }

    public static async Task<PostgreSqlDatabase> StartAsync()
    {
        PostgreSqlDatabase database = new();
        await database.WaitForConnectionAsync();
        return database;
    }

    public void Dispose()
    {
        _postgresContainer.Stop();
        _postgresContainer.Dispose();
    }

    private async Task WaitForConnectionAsync()
    {
        bool connected = false;
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (!connected && stopwatch.Elapsed < ConnectionTimeout)
        {
            try
            {
                await using NpgsqlConnection connection = new(ConnectionString);
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
