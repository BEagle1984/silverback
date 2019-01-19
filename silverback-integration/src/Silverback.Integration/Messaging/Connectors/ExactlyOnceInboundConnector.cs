// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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

        protected override void RelayMessages(IEnumerable<object> messages, IEndpoint sourceEndpoint, IServiceProvider serviceProvider)
        {
            messages = EnsureExactlyOnce(messages, sourceEndpoint, serviceProvider);

            base.RelayMessages(messages, sourceEndpoint, serviceProvider);
        }

        private IEnumerable<object> EnsureExactlyOnce(IEnumerable<object> messages, IEndpoint sourceEndpoint, IServiceProvider serviceProvider)
        {
            foreach (var message in messages)
            {
                if (MustProcess(message, sourceEndpoint, serviceProvider))
                {
                    yield return message;
                }
                else
                {
                    _logger.LogMessageTrace("Message is being skipped since it was already processed.", message,
                        sourceEndpoint);
                }
            }
        }

        protected abstract bool MustProcess(object message, IEndpoint sourceEndpoint, IServiceProvider serviceProvider);
    }
}