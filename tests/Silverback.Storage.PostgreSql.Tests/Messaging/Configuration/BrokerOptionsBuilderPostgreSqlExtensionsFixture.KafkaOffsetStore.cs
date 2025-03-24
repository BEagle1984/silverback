// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Configuration;

public partial class BrokerOptionsBuilderPostgreSqlExtensionsFixture
{
    [Fact]
    public void AddPostgreSqlKafkaOffsetStore_ShouldConfigureOffsetStoreFactories()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka().AddPostgreSqlKafkaOffsetStore()));

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();

        IKafkaOffsetStore store = factory.GetStore(new PostgreSqlKafkaOffsetStoreSettings("conn"), serviceProvider);

        store.ShouldBeOfType<PostgreSqlKafkaOffsetStore>();
    }
}
