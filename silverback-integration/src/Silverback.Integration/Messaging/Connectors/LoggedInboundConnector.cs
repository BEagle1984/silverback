// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Subscribes to a message broker and forwards the incoming integration messages to the internal bus.
    /// This implementation logs the incoming messages and prevents duplicated processing of the same message.
    /// </summary>
    public class LoggedInboundConnector : InboundConnector
    {
        private readonly ILogger<LoggedInboundConnector> _logger;

        public LoggedInboundConnector(IBroker broker, IServiceProvider serviceProvider, ILogger<LoggedInboundConnector> logger)
            : base(broker, serviceProvider, logger)
        {
            _logger = logger;
        }

        protected override void RelayMessage(IMessage message, IEndpoint sourceEndpoint, IPublisher publisher, IServiceProvider serviceProvider)
        {
            if (!(message is IIntegrationMessage integrationMessage))
            {
                throw new NotSupportedException("The LoggedInboundConnector currently supports only instances of IIntegrationMessage.");
            }

            RelayIntegrationMessage(integrationMessage, sourceEndpoint, publisher, serviceProvider);
        }

        protected void RelayIntegrationMessage(IIntegrationMessage message, IEndpoint sourceEndpoint, IPublisher publisher, IServiceProvider serviceProvider)
        {
            var inboundLog = serviceProvider.GetRequiredService<IInboundLog>();

            if (inboundLog.Exists(message, sourceEndpoint))
            {
                _logger.LogTrace($"Message is being skipped since it was already processed.", message, sourceEndpoint);
                return;
            }

            inboundLog.Add(message, sourceEndpoint);

            try
            {
                base.RelayMessage(message, sourceEndpoint, publisher, serviceProvider);
                inboundLog.Commit();
            }
            catch (Exception)
            {
                // TODO: Test exception case
                inboundLog.Rollback();
                throw;
            }
        }
    }
}