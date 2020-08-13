// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation that is used for testing purpose only. The messages are not
    ///     transferred through a real message broker.
    /// </summary>
    public class InMemoryBroker : Broker<IProducerEndpoint, IConsumerEndpoint>
    {
        private readonly ConcurrentDictionary<string, InMemoryTopic> _topics =
            new ConcurrentDictionary<string, InMemoryTopic>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryBroker" /> class.
        /// </summary>
        /// <param name="behaviors">
        ///     The <see cref="IEnumerable{T}" /> containing the <see cref="IBrokerBehavior" /> to be passed to the
        ///     producers and consumers.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public InMemoryBroker(
            IEnumerable<IBrokerBehavior> behaviors,
            IServiceProvider serviceProvider)
            : base(behaviors, serviceProvider)
        {
        }

        /// <summary>
        ///     Resets the offset of all topics. The offset for the next messages will restart from 0. This can be
        ///     used to simulate the case when the very same messages are consumed once again.
        /// </summary>
        public void ResetOffsets() => _topics.Values.ForEach(topic => topic.ResetOffset());

        internal InMemoryTopic GetTopic(string name) =>
            _topics.GetOrAdd(name, _ => new InMemoryTopic(name));

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateProducer" />
        protected override IProducer InstantiateProducer(
            IProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new InMemoryProducer(
                this,
                endpoint,
                behaviors,
                serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<InMemoryProducer>>());

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateConsumer" />
        protected override IConsumer InstantiateConsumer(
            IConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider)
        {
            Check.NotNull(endpoint, nameof(endpoint));
            Check.NotNull(callback, nameof(callback));
            Check.NotNull(serviceProvider, nameof(serviceProvider));

            return GetTopic(endpoint.Name).Subscribe(
                new InMemoryConsumer(
                    this,
                    endpoint,
                    callback,
                    behaviors,
                    serviceProvider,
                    serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<InMemoryConsumer>>()));
        }
    }
}
