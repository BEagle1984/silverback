// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Connectors.Repositories;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation that is used by the  <see cref="DeferredOutboundConnector"/> to write into the outbound queue.
    /// </summary>
    public class OutboundQueueBroker : Broker<IProducerEndpoint, IConsumerEndpoint>
    {
        private readonly IOutboundQueueWriter _queueWriter;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundQueueBroker" /> class.
        /// </summary>
        /// <param name="queueWriter">
        ///     The <see cref="IOutboundQueueWriter"/> to be used to write to the queue.
        /// </param>
        /// <param name="behaviors">
        ///     The <see cref="IEnumerable{T}" /> containing the <see cref="IBrokerBehavior" /> to be passed to the
        ///     producers and consumers.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public OutboundQueueBroker(
            IOutboundQueueWriter queueWriter,
            IEnumerable<IBrokerBehavior> behaviors,
            IServiceProvider serviceProvider)
            : base(behaviors, serviceProvider)
        {
            _queueWriter = queueWriter;
        }

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateProducer" />
        protected override IProducer InstantiateProducer(
            IProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new OutboundQueueProducer(
                _queueWriter,
                this,
                endpoint,
                behaviors,
                serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<OutboundQueueProducer>>());

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateConsumer" />
        protected override IConsumer InstantiateConsumer(
            IConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            throw new InvalidOperationException(
                "This IBroker implementation is used to write to outbound queue. " +
                "Only the producers are therefore supported.");
    }
}
