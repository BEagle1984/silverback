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
        protected ILogger Logger;
        private readonly MessageLogger _messageLogger;

        protected ExactlyOnceInboundConnector(IBroker broker, IServiceProvider serviceProvider, ILogger<ExactlyOnceInboundConnector> logger, MessageLogger messageLogger)
            : base(broker, serviceProvider, logger)
        {
            Logger = logger;
            _messageLogger = messageLogger;
        }

        protected override void RelayMessages(IEnumerable<IInboundMessage> messages, IServiceProvider serviceProvider)
        {
            messages = EnsureExactlyOnce(messages, serviceProvider);

            base.RelayMessages(messages, serviceProvider);
        }

        private IEnumerable<IInboundMessage> EnsureExactlyOnce(IEnumerable<IInboundMessage> messages, IServiceProvider serviceProvider)
        {
            foreach (var message in messages)
            {
                if (MustProcess(message, serviceProvider))
                {
                    yield return message;
                }
                else
                {
                    _messageLogger.LogTrace(Logger, "Message is being skipped since it was already processed.", message);
                }
            }
        }

        protected abstract bool MustProcess(IInboundMessage message, IServiceProvider serviceProvider);
    }
}