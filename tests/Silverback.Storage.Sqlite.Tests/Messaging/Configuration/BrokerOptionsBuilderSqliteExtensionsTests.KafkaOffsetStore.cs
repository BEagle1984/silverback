// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Storage;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Configuration;

public partial class BrokerOptionsBuilderSqliteExtensionsTests
{
    [Fact]
    public void AddSqliteKafkaOffsetStore_ShouldConfigureOffsetStoreFactories()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka().AddSqliteKafkaOffsetStore()));

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();

        IKafkaOffsetStore store = factory.GetStore(new SqliteKafkaOffsetStoreSettings("conn"), serviceProvider);

        store.ShouldBeOfType<SqliteKafkaOffsetStore>();
    }

    [Fact]
    public void UseSqliteKafkaOffsetStore_ShouldOverrideAllOffsetStoreSettingsTypes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka().UseSqliteKafkaOffsetStore("conn")));

        KafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<KafkaOffsetStoreFactory>();

        factory.AddFactory<KafkaOffsetStoreSettings1>((_, _) => new KafkaOffsetStore1());
        factory.AddFactory<KafkaOffsetStoreSettings2>((_, _) => new KafkaOffsetStore2());

        IKafkaOffsetStore store1 = factory.GetStore(new KafkaOffsetStoreSettings1(), serviceProvider);
        IKafkaOffsetStore store2 = factory.GetStore(new KafkaOffsetStoreSettings2(), serviceProvider);

        store1.ShouldBeOfType<SqliteKafkaOffsetStore>();
        store2.ShouldBeOfType<SqliteKafkaOffsetStore>();
    }

    [Fact]
    public void AddSqliteKafkaOffsetStore_ShouldNotThrow_WhenCalledTwice()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka().AddSqliteKafkaOffsetStore().AddSqliteKafkaOffsetStore()));

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();

        IKafkaOffsetStore store = factory.GetStore(new SqliteKafkaOffsetStoreSettings("conn"), serviceProvider);

        store.ShouldBeOfType<SqliteKafkaOffsetStore>();
    }

    [Fact]
    public void AddSqliteKafkaOffsetStore_ShouldThrow_WhenAddKafkaNotCalled()
    {
        BrokerOptionsBuilder builder = new(new ServiceCollection().AddSilverback());

        Action act = () => builder.AddSqliteKafkaOffsetStore();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("AddKafka has not been called");
    }

    [Fact]
    public void UseSqliteKafkaOffsetStore_ShouldThrow_WhenAddKafkaNotCalled()
    {
        BrokerOptionsBuilder builder = new(new ServiceCollection().AddSilverback());

        Action act = () => builder.UseSqliteKafkaOffsetStore("conn");

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("AddKafka has not been called");
    }

    [Fact]
    public void UseSqliteKafkaOffsetStore_ShouldUseSpecifiedSettings_WhenSettingsAreSqliteKafkaOffsetStoreSettings()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka().UseSqliteKafkaOffsetStore("conn")));

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();

        SqliteKafkaOffsetStoreSettings offsetStoreSettings = new("other-conn")
        {
            TableName = "other-table"
        };

        IKafkaOffsetStore store = factory.GetStore(offsetStoreSettings, serviceProvider);

        GetSettings(store.ShouldBeOfType<SqliteKafkaOffsetStore>()).ShouldBeSameAs(offsetStoreSettings);
    }

    [Fact]
    public void UseSqliteKafkaOffsetStore_ShouldUseDefaultSettings_WhenSettingsAreNotDatabaseConnectionSettings()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka().UseSqliteKafkaOffsetStore("conn")));

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();

        IKafkaOffsetStore store = factory.GetStore(new KafkaOffsetStoreSettings1(), serviceProvider);

        GetSettings(store.ShouldBeOfType<SqliteKafkaOffsetStore>()).ShouldBe(new SqliteKafkaOffsetStoreSettings("conn"));
    }

    [Fact]
    public void UseSqliteKafkaOffsetStore_ShouldMapTimeouts_WhenSettingsAreDatabaseConnectionSettings()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka().UseSqliteKafkaOffsetStore("conn")));

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();

        DatabaseKafkaOffsetStoreSettings offsetStoreSettings = new()
        {
            DbCommandTimeout = TimeSpan.FromSeconds(42),
            CreateTableTimeout = TimeSpan.FromSeconds(84)
        };

        IKafkaOffsetStore store = factory.GetStore(offsetStoreSettings, serviceProvider);

        SqliteKafkaOffsetStoreSettings storeSettings = GetSettings(store.ShouldBeOfType<SqliteKafkaOffsetStore>()).ShouldBeOfType<SqliteKafkaOffsetStoreSettings>();
        storeSettings.ConnectionString.ShouldBe("conn");
        storeSettings.DbCommandTimeout.ShouldBe(TimeSpan.FromSeconds(42));
        storeSettings.CreateTableTimeout.ShouldBe(TimeSpan.FromSeconds(84));
    }

    private record DatabaseKafkaOffsetStoreSettings : KafkaOffsetStoreSettings, IDatabaseConnectionSettings
    {
        public string ConnectionString => "other-conn";

        public string TableName { get; init; } = "other-table";

        public TimeSpan DbCommandTimeout { get; init; }

        public TimeSpan CreateTableTimeout { get; init; }
    }

    private record KafkaOffsetStoreSettings1 : KafkaOffsetStoreSettings;

    private record KafkaOffsetStoreSettings2 : KafkaOffsetStoreSettings;

    private class KafkaOffsetStore1 : IKafkaOffsetStore
    {
        public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId) => throw new NotSupportedException();

        public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null) => throw new NotSupportedException();
    }

    private class KafkaOffsetStore2 : IKafkaOffsetStore
    {
        public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId) => throw new NotSupportedException();

        public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null) => throw new NotSupportedException();
    }
}
