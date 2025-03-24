// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
public abstract class PostgresContainerFixture : IAsyncLifetime
{
    private readonly IContainerService _postgresContainer;

    protected PostgresContainerFixture()
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
        return Task.FromResult(Task.CompletedTask);
    }

    private async Task WaitForConnectionAsync()
    {
        bool connected = false;
        int tryCount = 0;

        while (!connected)
        {
            try
            {
                await using NpgsqlConnection connection = new(ConnectionString);
                await connection.OpenAsync();
                connected = true;
            }
            catch (NpgsqlException)
            {
                if (++tryCount > 20)
                    throw;

                await Task.Delay(100);
            }
        }
    }
}
