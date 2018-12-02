// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        public InboundConnector(IBroker broker, IServiceProvider serviceProvider, ILogger<InboundConnector> logger)
        {
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public virtual IInboundConnector Bind(IEndpoint endpoint, IErrorPolicy errorPolicy = null)
        {
            _logger.LogTrace($"Connecting to inbound endpoint '{endpoint.Name}'...");

            // TODO: Carefully test with multiple endpoints!
            // TODO: Test if consumer gets properly disposed etc.
            var consumer = _broker.GetConsumer(endpoint);
            consumer.Received += (_, message) => OnMessageReceived(message, endpoint);
            consumer.Error += (_, args) => OnError(args, errorPolicy);
            return this;
        }

        private void OnMessageReceived(IMessage message, IEndpoint sourceEndpoint)
        {
            _logger.LogTrace("Processing message.", message, sourceEndpoint);

            using (var scope = _serviceProvider.CreateScope())
            {
                RelayMessage(message, sourceEndpoint, scope.ServiceProvider.GetRequiredService<IPublisher>(), scope.ServiceProvider);
            }
        }

        protected virtual void RelayMessage(IMessage message, IEndpoint sourceEndpoint, IPublisher publisher,
            IServiceProvider serviceProvider) => publisher.Publish(message);

        private void OnError(ErrorHandlerEventArgs args, IErrorPolicy errorPolicy)
        {
            if (errorPolicy == null)
                return;

            args.Action = errorPolicy.HandleError(args.Message, args.RetryCount, args.Exception);
        }
    }
}