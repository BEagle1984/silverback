// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation for RabbitMQ.
    /// </summary>
    public class RabbitBroker : Broker<RabbitProducerEndpoint, RabbitConsumerEndpoint>
    {
        private readonly IRabbitConnectionFactory _connectionFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitBroker" /> class.
        /// </summary>
        /// <param name="behaviors">
        ///     The <see cref="IEnumerable{T}" /> containing the <see cref="IBrokerBehavior" /> to be passed to the
        ///     producers and consumers.
        /// </param>
        /// <param name="connectionFactory">
        ///     The <see cref="IRabbitConnectionFactory" /> to be used to create the channels to connect to the
        ///     endpoints.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public RabbitBroker(
            IEnumerable<IBrokerBehavior> behaviors,
            IRabbitConnectionFactory connectionFactory,
            IServiceProvider serviceProvider)
            : base(behaviors, serviceProvider)
        {
            _connectionFactory = connectionFactory;
        }

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateProducer" />
        protected override IProducer InstantiateProducer(
            RabbitProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new RabbitProducer(
                this,
                endpoint,
                behaviors,
                _connectionFactory,
                serviceProvider.GetRequiredService<ISilverbackLogger<RabbitProducer>>());

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateConsumer" />
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
                serviceProvider.GetRequiredService<ISilverbackLogger<RabbitConsumer>>());

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.Dispose(bool)" />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            _connectionFactory?.Dispose();
        }
    }
}
