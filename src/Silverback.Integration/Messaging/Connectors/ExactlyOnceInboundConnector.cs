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

        protected override void RelayMessages(IEnumerable<MessageReceivedEventArgs> messagesArgs, IEndpoint sourceEndpoint, IServiceProvider serviceProvider)
        {
            messagesArgs = EnsureExactlyOnce(messagesArgs, sourceEndpoint, serviceProvider);

            base.RelayMessages(messagesArgs, sourceEndpoint, serviceProvider);
        }

        private IEnumerable<MessageReceivedEventArgs> EnsureExactlyOnce(IEnumerable<MessageReceivedEventArgs> messagesArgs, IEndpoint sourceEndpoint, IServiceProvider serviceProvider)
        {
            foreach (var messageArgs in messagesArgs)
            {
                if (MustProcess(messageArgs, sourceEndpoint, serviceProvider))
                {
                    yield return messageArgs;
                }
                else
                {
                    _messageLogger.LogTrace(Logger, "Message is being skipped since it was already processed.", messageArgs.Message,
                        sourceEndpoint, offset: messageArgs.Offset);
                }
            }
        }

        protected abstract bool MustProcess(MessageReceivedEventArgs messageArgs, IEndpoint sourceEndpoint, IServiceProvider serviceProvider);
    }
}