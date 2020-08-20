// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation for Apache Kafka.
    /// </summary>
    public class KafkaBroker : Broker<KafkaProducerEndpoint, KafkaConsumerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaBroker" /> class.
        /// </summary>
        /// <param name="behaviors">
        ///     The <see cref="IEnumerable{T}" /> containing the <see cref="IBrokerBehavior" /> to be passed to the
        ///     producers and consumers.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public KafkaBroker(
            IEnumerable<IBrokerBehavior> behaviors,
            IServiceProvider serviceProvider)
            : base(behaviors, serviceProvider)
        {
        }

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateProducer" />
        protected override IProducer InstantiateProducer(
            KafkaProducerEndpoint endpoint,
            IReadOnlyList<IProducerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new KafkaProducer(
                this,
                endpoint,
                behaviors,
                serviceProvider,
                serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<KafkaProducer>>());

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateConsumer" />
        protected override IConsumer InstantiateConsumer(
            KafkaConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyList<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new KafkaConsumer(
                this,
                endpoint,
                callback,
                behaviors,
                serviceProvider,
                serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<KafkaConsumer>>());
    }
}
