// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Testing;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>UseMockedKafka</c> method to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderUseMockedKafkaExtensions
    {
        /// <summary>
        ///     Replaces the Kafka connectivity based on Confluent.Kafka with a mocked in-memory message broker that
        ///     <b>more or less</b> replicates the Kafka behavior.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="ISilverbackBuilder" />.
        /// </param>
        /// <param name="optionsAction">
        ///     Configures the mock options.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder UseMockedKafka(
            this ISilverbackBuilder builder,
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

            var optionsBuilder = new MockedKafkaOptionsBuilder(builder.Services);
            optionsAction?.Invoke(optionsBuilder);

            return builder;
        }
    }
}
