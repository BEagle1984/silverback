// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Subscribes to a message broker and forwards the incoming integration messages to the internal bus.
    /// </summary>
    public class InboundConnector : IInboundConnector
    {
        private readonly IBroker _broker;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InboundConnector> _logger;
        private readonly Dictionary<IEndpoint, MessageBatch> _batches = new Dictionary<IEndpoint, MessageBatch>();

        public InboundConnector(IBroker broker, IServiceProvider serviceProvider, ILogger<InboundConnector> logger)
        {
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public virtual IInboundConnector Bind(IEndpoint endpoint, IErrorPolicy errorPolicy = null)
        {
            endpoint.Validate();

            _logger.LogTrace($"Connecting to inbound endpoint '{endpoint.Name}'...");

            if (endpoint.Batch.Size > 1)
            {
                _batches.Add(endpoint, new MessageBatch(
                    endpoint,
                    RelayMessage,
                    CommitBatch,
                    RollbackBatch,
                    errorPolicy,
                    _serviceProvider,
                    _serviceProvider.GetRequiredService<ILogger<MessageBatch>>()));
            }

            // TODO: Carefully test with multiple endpoints!
            // TODO: Test if consumer gets properly disposed etc.
            var consumer = _broker.GetConsumer(endpoint);
            consumer.Received += (_, message) => OnMessageReceived(message, endpoint, errorPolicy);
            return this;
        }

        private void OnMessageReceived(IMessage message, IEndpoint sourceEndpoint, IErrorPolicy errorPolicy)
        {
            if (sourceEndpoint.Batch.Size > 1)
            {
                _batches[sourceEndpoint].AddMessage(message);
            }
            else
            {
                ProcessSingleMessage(message, sourceEndpoint, errorPolicy);
            }
        }

        private void ProcessSingleMessage(IMessage message, IEndpoint sourceEndpoint, IErrorPolicy errorPolicy)
        {
            _logger.LogTrace("Processing message.", message, sourceEndpoint);

            errorPolicy.TryProcess(message, _ =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    RelayAndCommitSingleMessage(message, sourceEndpoint, scope.ServiceProvider);
                }
            });
        }

        private void RelayAndCommitSingleMessage(IMessage message, IEndpoint sourceEndpoint, IServiceProvider serviceProvider)
        {
            try
            {
                RelayMessage(message, sourceEndpoint, serviceProvider);
                CommitBatch(serviceProvider);
            }
            catch (Exception)
            {
                RollbackBatch(serviceProvider);
                throw;
            }
        }

        protected virtual void RelayMessage(IMessage message, IEndpoint sourceEndpoint, IServiceProvider serviceProvider)
        {
            try
            {
                if (message is FailedMessage failedMessage)
                    message = failedMessage.Message;

                serviceProvider.GetRequiredService<IPublisher>().Publish(message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred processing the message.", message, sourceEndpoint);
                throw;
            }
        }

        protected virtual void CommitBatch(IServiceProvider serviceProvider)
        { }

        protected virtual void RollbackBatch(IServiceProvider serviceProvider)
        { }
    }
}