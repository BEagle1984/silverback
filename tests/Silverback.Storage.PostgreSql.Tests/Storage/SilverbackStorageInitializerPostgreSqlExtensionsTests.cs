// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Storage.DataAccess;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Storage;

public class SilverbackStorageInitializerPostgreSqlExtensionsTests : PostgresTests
{
    private readonly SilverbackStorageInitializer _storageInitializer;

    private readonly PostgreSqlDataAccess _dataAccess;

    public SilverbackStorageInitializerPostgreSqlExtensionsTests(PostgresContainerFixture fixture)
        : base(fixture)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .EnableStorage());

        _storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        _dataAccess = new PostgreSqlDataAccess(ConnectionString);
    }

    [Fact]
    public async Task CreatePostgreSqlOutboxAsync_ShouldCreateDefaultTable_WhenOnlyConnectionStringIsSpecified()
    {
        await _storageInitializer.CreatePostgreSqlOutboxAsync(ConnectionString);

        (await TableExistsAsync(new PostgreSqlOutboxSettings(ConnectionString).TableName)).ShouldBeTrue();
    }

    [Fact]
    public async Task CreatePostgreSqlLocksTableAsync_ShouldCreateDefaultTable_WhenOnlyConnectionStringIsSpecified()
    {
        await _storageInitializer.CreatePostgreSqlLocksTableAsync(ConnectionString);

        (await TableExistsAsync(new PostgreSqlTableLockSettings("lock", ConnectionString).TableName)).ShouldBeTrue();
    }

    [Fact]
    public async Task CreatePostgreSqlKafkaOffsetStoreAsync_ShouldCreateDefaultTable_WhenOnlyConnectionStringIsSpecified()
    {
        await _storageInitializer.CreatePostgreSqlKafkaOffsetStoreAsync(ConnectionString);

        (await TableExistsAsync(new PostgreSqlKafkaOffsetStoreSettings(ConnectionString).TableName)).ShouldBeTrue();
    }

    private async Task<bool> TableExistsAsync(string tableName) =>
        await _dataAccess.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{tableName}'",
            null,
            TimeSpan.FromSeconds(10)) == 1;
}
