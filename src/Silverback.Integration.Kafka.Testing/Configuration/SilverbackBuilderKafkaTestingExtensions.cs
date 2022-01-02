// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Testing;
using Silverback.Util;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <c>UseMockedKafka</c> method to the <see cref="SilverbackBuilder" />.
/// </summary>
public static class SilverbackBuilderKafkaTestingExtensions
{
    /// <summary>
    ///     Replaces the Kafka connectivity based on Confluent.Kafka with a mocked in-memory message broker that
    ///     <b>more or less</b> replicates the Kafka behavior.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" />.
    /// </param>
    /// <param name="optionsAction">
    ///     Configures the mock options.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder UseMockedKafka(
        this SilverbackBuilder builder,
        Action<IMockedKafkaOptionsBuilder>? optionsAction = null)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services
            .RemoveAll<IConfluentConsumerBuilder>()
            .RemoveAll<IConfluentProducerBuilder>()
            .RemoveAll<IConfluentAdminClientBuilder>()
            .AddSingleton<IMockedKafkaOptions>(new MockedKafkaOptions())
            .AddTransient<IConfluentProducerBuilder, MockedConfluentProducerBuilder>()
            .AddTransient<IConfluentConsumerBuilder, MockedConfluentConsumerBuilder>()
            .AddTransient<IConfluentAdminClientBuilder, MockedConfluentAdminClientBuilder>()
            .AddSingleton<IMockedConsumerGroupsCollection, MockedConsumerGroupsCollection>()
            .AddSingleton<IInMemoryTopicCollection, InMemoryTopicCollection>()
            .AddSingleton<IKafkaTestingHelper, KafkaTestingHelper>();

        MockedKafkaOptionsBuilder optionsBuilder = new(builder.Services);
        optionsAction?.Invoke(optionsBuilder);

        return builder;
    }
}
