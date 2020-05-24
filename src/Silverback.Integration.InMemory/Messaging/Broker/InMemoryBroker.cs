// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}" />
    public class InMemoryBroker : Broker<IProducerEndpoint, IConsumerEndpoint>
    {
        private readonly ConcurrentDictionary<string, InMemoryTopic> _topics =
            new ConcurrentDictionary<string, InMemoryTopic>();

        /// <summary> Initializes a new instance of the <see cref="InMemoryBroker" /> class. </summary>
        /// <param name="behaviors">
        ///     The <see cref="IEnumerable{T}" /> containing the <see cref="IBrokerBehavior" /> to be passed to the
        ///     producers and consumers.
        /// </param>
        /// <param name="loggerFactory"> The <see cref="ILoggerFactory" /> to be used to create the loggers. </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public InMemoryBroker(
            IEnumerable<IBrokerBehavior> behaviors,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
            : base(behaviors, loggerFactory, serviceProvider)
        {
        }

        internal InMemoryTopic GetTopic(string name) =>
            _topics.GetOrAdd(name, _ => new InMemoryTopic(name));

        /// <inheritdoc />
        protected override IProducer InstantiateProducer(
            IProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new InMemoryProducer(
                this,
                endpoint,
                behaviors,
                LoggerFactory.CreateLogger<InMemoryProducer>());

        /// <inheritdoc />
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
                    LoggerFactory.CreateLogger<InMemoryConsumer>()));
        }

        /// <inheritdoc />
        protected override void Disconnect(IEnumerable<IConsumer> consumers)
        {
            base.Disconnect(consumers);

            _topics.Clear();
        }
    }
}
