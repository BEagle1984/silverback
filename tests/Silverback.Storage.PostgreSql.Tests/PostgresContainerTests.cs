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
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql;

[Trait("Dependency", "Docker")]
[Trait("Database", "PostgreSql")]
public abstract class PostgresContainerTests : IAsyncLifetime
{
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromMinutes(1);

    private readonly IContainerService _postgresContainer;

    protected PostgresContainerTests()
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

    protected string ConnectionString { get; }

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
