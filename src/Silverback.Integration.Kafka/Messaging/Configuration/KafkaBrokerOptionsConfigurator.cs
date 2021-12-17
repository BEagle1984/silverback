// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     This class will be located via assembly scanning and invoked when a <see cref="KafkaBroker" /> is
    ///     added to the <see cref="IServiceCollection" />.
    /// </summary>
    public class KafkaBrokerOptionsConfigurator : IBrokerOptionsConfigurator<KafkaBroker>
    {
        /// <inheritdoc cref="IBrokerOptionsConfigurator{TBroker}.Configure" />
        public void Configure(IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder
                .AddSingletonBrokerBehavior<KafkaMessageKeyInitializerProducerBehavior>()
                .AddSingletonBrokerBehavior<KafkaPartitionResolverProducerBehavior>()
                .AddSingletonBrokerCallbackHandler<KafkaConsumerLocalTimeoutMonitor>()
                .Services
                .AddTransient<IConfluentProducerBuilder, ConfluentProducerBuilder>()
                .AddTransient<IConfluentConsumerBuilder, ConfluentConsumerBuilder>()
                .AddTransient<IConfluentAdminClientBuilder, ConfluentAdminClientBuilder>()
                .AddSingleton<IConfluentProducersCache, ConfluentProducersCache>()
                .AddSingleton<IBrokerLogEnricher<KafkaProducerEndpoint>, KafkaLogEnricher>()
                .AddSingleton<IBrokerLogEnricher<KafkaConsumerEndpoint>, KafkaLogEnricher>()
                .AddSingleton<IBrokerActivityEnricher<KafkaProducerEndpoint>, KafkaActivityEnricher>()
                .AddSingleton<IBrokerActivityEnricher<KafkaConsumerEndpoint>, KafkaActivityEnricher>()
                .AddSingleton<IMovePolicyMessageEnricher<KafkaProducerEndpoint>,
                    KafkaMovePolicyMessageEnricher>()
                .AddSingleton<IMovePolicyMessageEnricher<KafkaConsumerEndpoint>,
                    KafkaMovePolicyMessageEnricher>();
        }
    }
}
