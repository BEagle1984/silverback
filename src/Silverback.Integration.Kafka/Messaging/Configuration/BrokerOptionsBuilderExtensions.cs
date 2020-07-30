// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddKafka</c> method to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderExtensions
    {
        /// <summary>
        ///     Registers Apache Kafka as message broker.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddKafka(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            LogTemplates.ConfigureAdditionalData<KafkaConsumerEndpoint>("offset", "kafkaKey");
            LogTemplates.ConfigureAdditionalData<KafkaProducerEndpoint>("offset", "kafkaKey");

            return brokerOptionsBuilder.AddBroker<KafkaBroker>();
        }
    }
}
