// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Diagnostics;

internal sealed class InternalProducerLoggerFactory
{
    private readonly IBrokerLogEnricherFactory _enricherFactory;

    private readonly ConcurrentDictionary<Type, ProducerLogger> _cache = new();

    public InternalProducerLoggerFactory(IBrokerLogEnricherFactory enricherFactory)
    {
        _enricherFactory = Check.NotNull(enricherFactory, nameof(enricherFactory));
    }

    public ProducerLogger GetProducerLogger(ProducerEndpointConfiguration endpointConfiguration)
    {
        Type configurationType = Check.NotNull(endpointConfiguration, nameof(endpointConfiguration)).GetType();

        return _cache.GetOrAdd(
            configurationType,
            static (_, args) => new ProducerLogger(args.Factory.GetEnricher(args.Configuration)),
            (Configuration: endpointConfiguration, Factory: _enricherFactory));
    }
}
