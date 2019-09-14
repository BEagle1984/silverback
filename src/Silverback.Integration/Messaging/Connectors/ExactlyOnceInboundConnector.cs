// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

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
            : base(broker, serviceProvider)
        {
            Logger = logger;
            _messageLogger = messageLogger;
        }

        protected override async Task RelayMessages(IEnumerable<IInboundMessage> messages, IServiceProvider serviceProvider)
        {
            messages = await EnsureExactlyOnce(messages, serviceProvider);

            await base.RelayMessages(messages, serviceProvider);
        }

        private async Task<IEnumerable<IInboundMessage>> EnsureExactlyOnce(IEnumerable<IInboundMessage> messages, IServiceProvider serviceProvider) =>
            await messages.WhereAsync(async message =>
            {
                if (await MustProcess(message, serviceProvider))
                    return true;

                _messageLogger.LogTrace(Logger, "Message is being skipped since it was already processed.", message);
                return false;
            });

        protected abstract Task<bool> MustProcess(IInboundMessage message, IServiceProvider serviceProvider);
    }
}