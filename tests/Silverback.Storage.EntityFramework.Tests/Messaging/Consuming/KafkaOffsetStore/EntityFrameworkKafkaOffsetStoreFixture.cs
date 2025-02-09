// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Consuming.KafkaOffsetStore;

public sealed class EntityFrameworkKafkaOffsetStoreFixture : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;

    private readonly EntityFrameworkKafkaOffsetStoreSettings _offsetStoreSettings;

    public EntityFrameworkKafkaOffsetStoreFixture()
    {
        _sqliteConnection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _sqliteConnection.Open();

        _offsetStoreSettings = new EntityFrameworkKafkaOffsetStoreSettings(
            typeof(TestDbContext),
            (serviceProvider, _) => serviceProvider.GetRequiredService<TestDbContext>());
    }

    [Fact]
    public async Task GetStoredOffsets_ShouldReturnStoredOffsetsForGroup()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddDbContext<TestDbContext>(options => options.UseSqlite(_sqliteConnection))
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka().AddEntityFrameworkKafkaOffsetStore()));

        using IServiceScope scope = serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();
        EntityFrameworkKafkaOffsetStore store = (EntityFrameworkKafkaOffsetStore)factory.GetStore(_offsetStoreSettings, serviceProvider);

        await store.StoreOffsetsAsync(
            "group1",
            [
                new KafkaOffset("topic1", 0, 42),
                new KafkaOffset("topic1", 1, 42)
            ]);
        await store.StoreOffsetsAsync(
            "group2",
            [
                new KafkaOffset("topic1", 0, 42)
            ]);

        IReadOnlyCollection<KafkaOffset> offsets = store.GetStoredOffsets("group1");

        offsets.Count.ShouldBe(2);
        offsets.ShouldBe(
            [
                new KafkaOffset("topic1", 0, 42),
                new KafkaOffset("topic1", 1, 42)
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task StoreOffsetsAsync_ShouldStoreOffsets()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddDbContext<TestDbContext>(options => options.UseSqlite(_sqliteConnection))
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka().AddEntityFrameworkKafkaOffsetStore()));

        using IServiceScope scope = serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();
        IKafkaOffsetStore store = factory.GetStore(_offsetStoreSettings, serviceProvider);

        KafkaOffset[] offsets =
        [
            new("topic1", 3, 42),
            new("topic1", 5, 42)
        ];

        await store.StoreOffsetsAsync("group1", offsets);

        IReadOnlyCollection<KafkaOffset> storedOffsets = store.GetStoredOffsets("group1");
        storedOffsets.Count.ShouldBe(2);
        storedOffsets.ShouldBe(offsets);
    }

    public void Dispose() => _sqliteConnection.Dispose();

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<SilverbackStoredOffset> Offsets { get; set; } = null!;
    }
}
