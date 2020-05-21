// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Broker
{
    /// <summary> An <see cref="IBroker" /> implementation for RabbitMQ. </summary>
    /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}" />
    public class RabbitBroker : Broker<RabbitProducerEndpoint, RabbitConsumerEndpoint>
    {
        private readonly IRabbitConnectionFactory _connectionFactory;

        private readonly ILoggerFactory _loggerFactory;

        /// <summary> Initializes a new instance of the <see cref="RabbitBroker" /> class. </summary>
        /// <param name="behaviors">
        ///     The <see cref="IEnumerable{T}" /> containing the <see cref="IBrokerBehavior" /> to be passed to the
        ///     producers and consumers.
        /// </param>
        /// <param name="connectionFactory">
        ///     The <see cref="IRabbitConnectionFactory" /> to be used to create the channels to connect to the
        ///     endpoints.
        /// </param>
        /// <param name="loggerFactory"> The <see cref="ILoggerFactory" /> to be used to create the loggers. </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public RabbitBroker(
            IEnumerable<IBrokerBehavior> behaviors,
            IRabbitConnectionFactory connectionFactory,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
            : base(behaviors, loggerFactory, serviceProvider)
        {
            _loggerFactory = loggerFactory;
            _connectionFactory = connectionFactory;
        }

        /// <inheritdoc />
        protected override IProducer InstantiateProducer(
            RabbitProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new RabbitProducer(
                this,
                endpoint,
                behaviors,
                _connectionFactory,
                _loggerFactory.CreateLogger<RabbitProducer>());

        /// <inheritdoc />
        protected override IConsumer InstantiateConsumer(
            RabbitConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new RabbitConsumer(
                this,
                endpoint,
                callback,
                behaviors,
                _connectionFactory,
                serviceProvider,
                _loggerFactory.CreateLogger<RabbitConsumer>());

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            _connectionFactory?.Dispose();
        }
    }
}
