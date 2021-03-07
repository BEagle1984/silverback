// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Silverback.Messaging;

namespace Silverback.Diagnostics
{
    internal class InboundLoggerFactory
    {
        private readonly BrokerLogEnricherFactory _enricherFactory;

        private readonly ConcurrentDictionary<Type, InboundLogger> _inboundLoggers = new();

        public InboundLoggerFactory(BrokerLogEnricherFactory enricherFactory)
        {
            _enricherFactory = enricherFactory;
        }

        public InboundLogger GetInboundLogger(IEndpoint endpoint) =>
            _inboundLoggers.GetOrAdd(
                endpoint.GetType(),
                _ => new InboundLogger(_enricherFactory.GetLogEnricher(endpoint)));
    }
}
