// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// The base class for the InboundConnector checking that each message is processed only once.
    /// </summary>
    public abstract class ExactlyOnceInboundConnector : InboundConnector
    {
        protected ILogger<LoggedInboundConnector> _logger;

        protected ExactlyOnceInboundConnector(IBroker broker, IServiceProvider serviceProvider, ILogger<LoggedInboundConnector> logger)
            : base(broker, serviceProvider, logger)
        {
            _logger = logger;
        }

        protected override void RelayMessage(IMessage message, IEndpoint sourceEndpoint, IServiceProvider serviceProvider)
        {
            if (!MustProcess(message, sourceEndpoint, serviceProvider))
            {
                _logger.LogTrace("Message is being skipped since it was already processed.", message, sourceEndpoint);
                return;
            }

            base.RelayMessage(message, sourceEndpoint, serviceProvider);
        }

        protected abstract bool MustProcess(IMessage message, IEndpoint sourceEndpoint, IServiceProvider serviceProvider);
    }
}