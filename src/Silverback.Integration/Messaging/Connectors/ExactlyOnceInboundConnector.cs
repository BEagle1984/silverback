// Copyright (c) 2020 Sergio Aquilini
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
    ///     The base class for the InboundConnector checking that each message is processed only once.
    /// </summary>
    public abstract class ExactlyOnceInboundConnector : InboundConnector
    {
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;

        protected ExactlyOnceInboundConnector(
            IBrokerCollection brokerCollection,
            IServiceProvider serviceProvider,
            ILogger<ExactlyOnceInboundConnector> logger,
            MessageLogger messageLogger)
            : base(brokerCollection, serviceProvider, logger)
        {
            _logger = logger;
            _messageLogger = messageLogger;
        }

        protected override async Task RelayMessages(
            IEnumerable<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider)
        {
            envelopes = await EnsureExactlyOnce(envelopes, serviceProvider);

            await base.RelayMessages(envelopes, serviceProvider);
        }

        private async Task<IEnumerable<IRawInboundEnvelope>> EnsureExactlyOnce(
            IEnumerable<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider) =>
            await envelopes.WhereAsync(async envelope =>
            {
                if (await MustProcess(envelope, serviceProvider))
                    return true;

                _messageLogger.LogDebug(_logger, "Message is being skipped since it was already processed.", envelope);
                return false;
            });

        protected abstract Task<bool> MustProcess(IRawInboundEnvelope envelope, IServiceProvider serviceProvider);
    }
}