// Copyright (c) 2025 Sergio Aquilini
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
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Configuration;

public partial class BrokerOptionsBuilderSqliteExtensionsFixture
{
    [Fact]
    public void AddSqliteKafkaOffsetStore_ShouldConfigureOffsetStoreFactories()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
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
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
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
