// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound;
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
                .Services
                .AddTransient<IConfluentProducerBuilder, ConfluentProducerBuilder>()
                .AddTransient<IConfluentConsumerBuilder, ConfluentConsumerBuilder>()
                .AddSingleton<IConfluentProducersCache, ConfluentProducersCache>();

            brokerOptionsBuilder.GetLoggerCollection().AddInbound<KafkaConsumerEndpoint>(
                "offset",
                envelope =>
                    envelope.BrokerMessageIdentifier is KafkaOffset offset
                        ? $"{offset.Partition}@{offset.Offset}"
                        : null,
                "kafkaKey",
                envelope => envelope.Headers.GetValue(KafkaMessageHeaders.KafkaMessageKey));

            brokerOptionsBuilder.GetLoggerCollection().AddOutbound<KafkaProducerEndpoint>(
                "offset",
                envelope =>
                    envelope.BrokerMessageIdentifier is KafkaOffset offset
                        ? $"{offset.Partition}@{offset.Offset}"
                        : null,
                "kafkaKey",
                envelope => envelope.Headers.GetValue(KafkaMessageHeaders.KafkaMessageKey));
        }
    }
}
