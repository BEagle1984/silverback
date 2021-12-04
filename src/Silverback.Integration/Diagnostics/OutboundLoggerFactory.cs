// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Util;

namespace Silverback.Diagnostics;

internal sealed class OutboundLoggerFactory
{
    private readonly BrokerLogEnricherFactory _enricherFactory;

    private readonly Dictionary<Type, OutboundLogger> _outboundLoggers = new();

    public OutboundLoggerFactory(BrokerLogEnricherFactory enricherFactory)
    {
        _enricherFactory = enricherFactory;
    }

    public OutboundLogger GetOutboundLogger(ProducerConfiguration producerConfiguration)
    {
        Type type = producerConfiguration.GetType();

        if (_outboundLoggers.TryGetValue(type, out OutboundLogger? logger))
            return logger;

        lock (_outboundLoggers)
        {
            return _outboundLoggers.GetOrAdd(
                type,
                _ => new OutboundLogger(_enricherFactory.GetLogEnricher(producerConfiguration)));
        }
    }
}
