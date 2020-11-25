// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Messaging.Broker.ConfluentWrappers;
using Silverback.Messaging.Broker.Topics;
using Silverback.Messaging.Configuration;
using Silverback.Testing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>UseMockedKafka</c> method to the <see cref="IServiceCollection" />.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Replaces the Kafka connectivity based on Confluent.Kafka with a mocked in-memory message broker that
        ///     <b>more or less</b> replicates the Kafka behavior.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="optionsAction">
        ///     Additional options (such as topics and partitions settings).
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection UseMockedKafka(
            this IServiceCollection services,
            Action<IMockedKafkaOptionsBuilder>? optionsAction = null)
        {
            services
                .RemoveAll<IConfluentConsumerBuilder>()
                .RemoveAll<IConfluentProducerBuilder>()
                .AddSingleton<IMockedKafkaOptions>(new MockedKafkaOptions())
                .AddTransient<IConfluentProducerBuilder, MockedConfluentProducerBuilder>()
                .AddTransient<IConfluentConsumerBuilder, MockedConfluentConsumerBuilder>()
                .AddSingleton<IInMemoryTopicCollection, InMemoryTopicCollection>()
                .AddSingleton<IKafkaTestingHelper, KafkaTestingHelper>();

            var optionsBuilder = new MockedKafkaOptionsBuilder(services);
            optionsAction?.Invoke(optionsBuilder);

            return services;
        }
    }
}
