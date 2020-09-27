// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.ConfluentWrappers;
using Silverback.Messaging.Broker.Topics;

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
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection UseMockedKafka(this IServiceCollection services)
        {
            services
                .RemoveAll<IConfluentConsumerBuilder>()
                .RemoveAll<IConfluentProducerBuilder>()
                .AddTransient<IConfluentProducerBuilder, MockedConfluentProducerBuilder>()
                .AddTransient<IConfluentConsumerBuilder, MockedConfluentConsumerBuilder>()
                .AddSingleton<IInMemoryTopicCollection, InMemoryTopicCollection>();

            return services;
        }
    }
}
