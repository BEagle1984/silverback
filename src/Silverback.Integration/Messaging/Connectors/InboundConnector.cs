// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Subscribes to a message broker and forwards the incoming integration messages to the internal bus.
    /// </summary>
    public class InboundConnector : IInboundConnector
    {
        private readonly IBrokerCollection _brokerCollection;

        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<InboundConnector> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="InboundConnector" /> class.
        /// </summary>
        /// <param name="brokerCollection">
        ///     The collection containing the available brokers.
        /// </param>
        /// <param name="serviceProvider"> The <see cref="IServiceProvider" />. </param>
        /// <param name="logger"> The <see cref="ILogger" />. </param>
        public InboundConnector(
            IBrokerCollection brokerCollection,
            IServiceProvider serviceProvider,
            ILogger<InboundConnector> logger)
        {
            _brokerCollection = brokerCollection ?? throw new ArgumentNullException(nameof(brokerCollection));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        /// <inheritdoc />
        public virtual IInboundConnector Bind(
            IConsumerEndpoint endpoint,
            IErrorPolicy? errorPolicy = null,
            InboundConnectorSettings? settings = null)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            settings ??= new InboundConnectorSettings();

            for (var i = 0; i < settings.Consumers; i++)
            {
                CreateConsumer(endpoint, errorPolicy, settings);
            }

            return this;
        }

        /// <summary>
        ///     Creates and initializes the <see cref="IConsumer" /> to be used to consume the specified endpoint.
        /// </summary>
        /// <param name="endpoint"> The endpoint to be consumed. </param>
        /// <param name="errorPolicy">
        ///     The optional error policy to be applied when an exception is thrown during the processing of a
        ///     message.
        /// </param>
        /// <param name="settings">
        ///     The additional settings such as batch consuming.
        /// </param>
        protected virtual void CreateConsumer(
            IConsumerEndpoint endpoint,
            IErrorPolicy? errorPolicy,
            InboundConnectorSettings settings)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _logger.LogTrace("Connecting to inbound endpoint '{endpointName}'...", endpoint.Name);

            settings.Validate();

            var consumer = _brokerCollection.GetConsumer(
                endpoint,
                args => RelayMessages(args.Envelopes, args.ServiceProvider));

            ConfigureInboundProcessorBehavior(errorPolicy, settings, consumer);
        }

        /// <summary>
        ///     Publishes the received messages into the internal bus to be forwarded to the subscribers.
        /// </summary>
        /// <param name="envelopes">
        ///     The envelopes containing the messages to be published.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> scoped to the processing of the current messages.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected virtual async Task RelayMessages(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider)
        {
            if (envelopes == null)
                throw new ArgumentNullException(nameof(envelopes));

            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            await serviceProvider.GetRequiredService<IPublisher>().PublishAsync(envelopes);
        }

        private void ConfigureInboundProcessorBehavior(
            IErrorPolicy? errorPolicy,
            InboundConnectorSettings settings,
            IConsumer consumer)
        {
            var inboundProcessors = consumer.Behaviors.OfType<InboundProcessorConsumerBehavior>().ToList();

            if (inboundProcessors.Count != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly 1 InboundProcessorConsumerBehavior to be configured.");
            }

            var inboundProcessor = inboundProcessors.First();

            inboundProcessor.Batch = CreateMessageBatchIfNeeded(errorPolicy, settings, consumer);
            inboundProcessor.ErrorPolicy = errorPolicy;
        }

        private MessageBatch? CreateMessageBatchIfNeeded(
            IErrorPolicy? errorPolicy,
            InboundConnectorSettings settings,
            IConsumer consumer)
        {
            if (settings.Batch.Size <= 1)
                return null;

            return new MessageBatch(
                settings.Batch,
                errorPolicy,
                consumer,
                _serviceProvider);
        }
    }
}
