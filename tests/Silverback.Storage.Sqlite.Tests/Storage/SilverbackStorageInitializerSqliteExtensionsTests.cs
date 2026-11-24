// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Storage;
using Silverback.Storage.DataAccess;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Storage;

public sealed class SilverbackStorageInitializerSqliteExtensionsTests : IDisposable
{
    private readonly string _connectionString;

    private readonly SqliteConnection _sqliteConnection;

    private readonly SqliteDataAccess _dataAccess;

    private readonly SilverbackStorageInitializer _storageInitializer;

    public SilverbackStorageInitializerSqliteExtensionsTests()
    {
        _connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _sqliteConnection = new SqliteConnection(_connectionString);
        _sqliteConnection.Open();

        _dataAccess = new SqliteDataAccess(_connectionString);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka().AddSqliteOutbox().AddSqliteKafkaOffsetStore()));

        _storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
    }

    [Fact]
    public async Task CreateSqliteOutboxAsync_ShouldCreateDefaultTable_WhenConnectionStringIsSpecified()
    {
        (await TableExistsAsync("SilverbackOutbox")).ShouldBeFalse();

        await _storageInitializer.CreateSqliteOutboxAsync(_connectionString);

        (await TableExistsAsync("SilverbackOutbox")).ShouldBeTrue();
    }

    [Fact]
    public async Task CreateSqliteKafkaOffsetStoreAsync_ShouldCreateDefaultTable_WhenConnectionStringIsSpecified()
    {
        (await TableExistsAsync("SilverbackKafkaOffsets")).ShouldBeFalse();

        await _storageInitializer.CreateSqliteKafkaOffsetStoreAsync(_connectionString);

        (await TableExistsAsync("SilverbackKafkaOffsets")).ShouldBeTrue();
    }

    public void Dispose() => _sqliteConnection.Dispose();

    private async Task<bool> TableExistsAsync(string tableName)
    {
        long count = await _dataAccess.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @TableName",
            [new SqliteParameter("@TableName", tableName)],
            TimeSpan.FromSeconds(1));

        return count > 0;
    }
}
