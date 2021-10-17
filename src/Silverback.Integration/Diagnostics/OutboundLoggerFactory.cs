// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Silverback.Messaging;

namespace Silverback.Diagnostics
{
    internal sealed class OutboundLoggerFactory
    {
        private readonly BrokerLogEnricherFactory _enricherFactory;

        private readonly ConcurrentDictionary<Type, OutboundLogger> _outboundLoggers = new();

        public OutboundLoggerFactory(BrokerLogEnricherFactory enricherFactory)
        {
            _enricherFactory = enricherFactory;
        }

        public OutboundLogger GetOutboundLogger(IEndpoint endpoint)
        {
            return _outboundLoggers.GetOrAdd(
                endpoint.GetType(),
                _ => new OutboundLogger(_enricherFactory.GetLogEnricher(endpoint)));
        }
    }
}
