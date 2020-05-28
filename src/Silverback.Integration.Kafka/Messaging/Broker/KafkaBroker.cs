// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation for Apache Kafka.
    /// </summary>
    public class KafkaBroker : Broker<KafkaProducerEndpoint, KafkaConsumerEndpoint>
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaBroker" /> class.
        /// </summary>
        /// <param name="behaviors">
        ///     The <see cref="IEnumerable{T}" /> containing the <see cref="IBrokerBehavior" /> to be passed to the
        ///     producers and consumers.
        /// </param>
        /// <param name="loggerFactory">
        ///     The <see cref="ILoggerFactory" /> to be used to create the loggers.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public KafkaBroker(
            IEnumerable<IBrokerBehavior> behaviors,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
            : base(behaviors, loggerFactory, serviceProvider)
        {
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        protected override IProducer InstantiateProducer(
            KafkaProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new KafkaProducer(
                this,
                endpoint,
                behaviors,
                serviceProvider,
                _loggerFactory.CreateLogger<KafkaProducer>());

        /// <inheritdoc />
        protected override IConsumer InstantiateConsumer(
            KafkaConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new KafkaConsumer(
                this,
                endpoint,
                callback,
                behaviors,
                serviceProvider,
                _loggerFactory.CreateLogger<KafkaConsumer>());
    }
}
