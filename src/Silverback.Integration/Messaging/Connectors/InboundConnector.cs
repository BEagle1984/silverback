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

        public InboundConnector(
            IBrokerCollection brokerCollection,
            IServiceProvider serviceProvider,
            ILogger<InboundConnector> logger)
        {
            _brokerCollection = brokerCollection ?? throw new ArgumentNullException(nameof(brokerCollection));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        public virtual IInboundConnector Bind(
            IConsumerEndpoint endpoint,
            IErrorPolicy errorPolicy = null,
            InboundConnectorSettings settings = null)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            settings ??= new InboundConnectorSettings();

            for (var i = 0; i < settings.Consumers; i++)
            {
                CreateAndBindConsumer(endpoint, errorPolicy, settings);
            }

            return this;
        }

        protected virtual void CreateAndBindConsumer(
            IConsumerEndpoint endpoint,
            IErrorPolicy errorPolicy,
            InboundConnectorSettings settings)
        {
            _logger.LogTrace("Connecting to inbound endpoint '{endpointName}'...", endpoint.Name);

            settings.Validate();

            var consumer = _brokerCollection.GetConsumer(endpoint);
            consumer.Received += (sender, args) => RelayMessages(args.Envelopes, args.ServiceProvider);

            InitConsumerAdditionalFeatures(errorPolicy, settings, consumer);
        }

        private void InitConsumerAdditionalFeatures(
            IErrorPolicy errorPolicy,
            InboundConnectorSettings settings,
            IConsumer consumer)
        {
            var inboundProcessors = consumer.Behaviors.OfType<InboundProcessorConsumerBehavior>().ToList();

            if (inboundProcessors.Count != 1)
                throw new SilverbackException("No InboundProcessorConsumerBehavior is configured.");

            var inboundProcessor = inboundProcessors.First();

            inboundProcessor.Batch = CreateMessageBatchIfNeeded(errorPolicy, settings, consumer);
            inboundProcessor.ErrorPolicy = errorPolicy;
        }

        private MessageBatch CreateMessageBatchIfNeeded(
            IErrorPolicy errorPolicy,
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

        protected virtual async Task RelayMessages(
            IEnumerable<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider) =>
            await serviceProvider.GetRequiredService<IPublisher>().PublishAsync(envelopes);
    }
}