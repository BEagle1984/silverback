// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Inbound.ExactlyOnce.Repositories;
using Silverback.Tests.Integration.TestTypes.Database;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Inbound.ExactlyOnce.Repositories;

public sealed class DbOffsetStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;

    private readonly IServiceScope _scope;

    private readonly TestDbContext _dbContext;

    private readonly DbOffsetStore _offsetStore;

    public DbOffsetStoreTests()
    {
        _connection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connection.Open();

        ServiceCollection services = new();

        services
            .AddLoggerSubstitute()
            .AddDbContext<TestDbContext>(
                options => options
                    .UseSqlite(_connection.ConnectionString))
            .AddSilverback()
            .UseDbContext<TestDbContext>();

        ServiceProvider? serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = true
            });

        _scope = serviceProvider.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<TestDbContext>();
        _dbContext.Database.EnsureCreated();

        _offsetStore = new DbOffsetStore(_scope.ServiceProvider.GetRequiredService<IDbContext>());
    }

    [Fact]
    public async Task Store_SomeOffsets_TableStillEmpty()
    {
        await _offsetStore.StoreAsync(new TestOffset("topic1", "1"), new TestConsumerConfiguration("topic1"));
        await _offsetStore.StoreAsync(new TestOffset("topic1", "2"), new TestConsumerConfiguration("topic1"));
        await _offsetStore.StoreAsync(new TestOffset("topic2", "1"), new TestConsumerConfiguration("topic2"));

        _dbContext.StoredOffsets.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreAndCommit_SomeOffsets_OffsetsStored()
    {
        await _offsetStore.StoreAsync(new TestOffset("topic1", "1"), new TestConsumerConfiguration("topic1"));
        await _offsetStore.StoreAsync(new TestOffset("topic2", "1"), new TestConsumerConfiguration("topic1"));
        await _offsetStore.StoreAsync(new TestOffset("topic3", "1"), new TestConsumerConfiguration("topic2"));

        await _offsetStore.CommitAsync();

        _dbContext.StoredOffsets.Should().HaveCount(3);
    }

    [Fact]
    public async Task StoreAndCommit_MultipleOffsetsForSameTopic_StoredOffsetUpdated()
    {
        await _offsetStore.StoreAsync(new TestOffset("topic1", "1"), new TestConsumerConfiguration("topic1"));
        await _offsetStore.StoreAsync(new TestOffset("topic2", "1"), new TestConsumerConfiguration("topic2"));
        await _offsetStore.StoreAsync(new TestOffset("topic1", "2"), new TestConsumerConfiguration("topic1"));

        await _offsetStore.CommitAsync();

        _dbContext.StoredOffsets.Should().HaveCount(2);
        _dbContext.StoredOffsets
            .Single(offset => offset.Key == "topic1|default-group|topic1")
            .Value.Should().Be("2");
    }

    [Fact]
    public async Task StoreAndCommit_UpdatedOffsetForSameTopic_StoredOffsetUpdated()
    {
        await _offsetStore.StoreAsync(new TestOffset("topic1", "1"), new TestConsumerConfiguration("topic1"));
        await _offsetStore.StoreAsync(new TestOffset("topic2", "1"), new TestConsumerConfiguration("topic2"));
        await _offsetStore.CommitAsync();

        await _offsetStore.StoreAsync(new TestOffset("topic1", "2"), new TestConsumerConfiguration("topic1"));
        await _offsetStore.CommitAsync();

        _dbContext.StoredOffsets.Should().HaveCount(2);
        _dbContext.StoredOffsets
            .Single(offset => offset.Key == "topic1|default-group|topic1")
            .Value.Should().Be("2");
    }

    [Fact]
    public async Task StoreAndRollback_SomeOffsets_TableStillEmpty()
    {
        await _offsetStore.StoreAsync(new TestOffset("topic1", "1"), new TestConsumerConfiguration("topic1"));
        await _offsetStore.StoreAsync(new TestOffset("topic1", "2"), new TestConsumerConfiguration("topic1"));
        await _offsetStore.StoreAsync(new TestOffset("topic2", "1"), new TestConsumerConfiguration("topic2"));

        await _offsetStore.RollbackAsync();

        _dbContext.StoredOffsets.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreAndCommit_Offset_OffsetCorrectlyStored()
    {
        await _offsetStore.StoreAsync(new TestOffset("topic1", "42"), new TestConsumerConfiguration("topic1"));
        await _offsetStore.CommitAsync();

        StoredOffset? storedOffset = _dbContext.StoredOffsets.First();

        storedOffset.Key.Should().Be("topic1|default-group|topic1");
        storedOffset.Value.Should().Be("42");
    }

    [Fact]
    public async Task GetLatestValue_SomeStoredOffsets_LatestOffsetReturned()
    {
        _dbContext.StoredOffsets.Add(
            new StoredOffset
            {
                Key = "topic1|group1|topic1",
                ClrType = typeof(TestOffset).AssemblyQualifiedName,
                Value = "1"
            });
        _dbContext.StoredOffsets.Add(
            new StoredOffset
            {
                Key = "topic2|group1|topic1",
                ClrType = typeof(TestOffset).AssemblyQualifiedName,
                Value = "5"
            });
        await _dbContext.SaveChangesAsync();

        TestConsumerConfiguration endpoint = new("topic1") { GroupId = "group1" };
        IBrokerMessageOffset? latestOffset = await _offsetStore.GetLatestValueAsync("topic1", endpoint);

        latestOffset.Should().NotBeNull();
        latestOffset!.Value.Should().Be("1");
    }

    [Fact]
    public async Task GetLatestValue_SomeStoredOffsetsForDifferentConsumerGroups_CorrectLatestOffsetReturned()
    {
        _dbContext.StoredOffsets.Add(
            new StoredOffset
            {
                Key = "topic1|group1|topic1",
                ClrType = typeof(TestOffset).AssemblyQualifiedName,
                Value = "1"
            });
        _dbContext.StoredOffsets.Add(
            new StoredOffset
            {
                Key = "topic2|group1|topic1",
                ClrType = typeof(TestOffset).AssemblyQualifiedName,
                Value = "5"
            });
        _dbContext.StoredOffsets.Add(
            new StoredOffset
            {
                Key = "topic1|group2|topic1",
                ClrType = typeof(TestOffset).AssemblyQualifiedName,
                Value = "2"
            });
        await _dbContext.SaveChangesAsync();

        TestConsumerConfiguration endpoint = new("topic1") { GroupId = "group1" };
        IBrokerMessageOffset? latestOffset = await _offsetStore.GetLatestValueAsync("topic1", endpoint);

        latestOffset.Should().NotBeNull();
        latestOffset!.Value.Should().Be("1");
    }

    [Fact]
    public async Task GetLatestValue_SomeStoredOffsetsWithLegacySerialization_LatestOffsetReturned()
    {
        _dbContext.StoredOffsets.Add(
            new StoredOffset
            {
                Key = "topic1|group1|topic1",
#pragma warning disable CS0618 // Obsolete
                Offset = $"{{\"$type\":\"{typeof(TestOffset).AssemblyQualifiedName}\"," +
                         "\"Key\":\"topic1|group1\",\"Value\":\"42\"}"
#pragma warning restore CS0618 // Obsolete
            });
        await _dbContext.SaveChangesAsync();

        TestConsumerConfiguration endpoint = new("topic1") { GroupId = "group1" };
        IBrokerMessageOffset? latestOffset = await _offsetStore.GetLatestValueAsync("topic1", endpoint);

        latestOffset.Should().NotBeNull();
        latestOffset!.Value.Should().Be("42");
    }

    [Fact]
    public async Task GetLatestValue_NoStoredOffsets_NullReturned()
    {
        IBrokerMessageOffset? latestOffset = await _offsetStore.GetLatestValueAsync("topic1", new TestConsumerConfiguration("topic1"));

        latestOffset.Should().BeNull();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
        _scope.Dispose();
    }
}
