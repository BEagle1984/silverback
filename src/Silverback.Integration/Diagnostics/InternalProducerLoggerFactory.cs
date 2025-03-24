// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Diagnostics;

internal sealed class InternalProducerLoggerFactory
{
    private readonly IBrokerLogEnricherFactory _enricherFactory;

    private readonly IServiceProvider _serviceProvider;

    private readonly ConcurrentDictionary<Type, ProducerLogger> _cache = new();

    public InternalProducerLoggerFactory(IBrokerLogEnricherFactory enricherFactory, IServiceProvider serviceProvider)
    {
        _enricherFactory = Check.NotNull(enricherFactory, nameof(enricherFactory));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
    }

    public ProducerLogger GetProducerLogger(ProducerEndpointConfiguration endpointConfiguration)
    {
        Type configurationType = Check.NotNull(endpointConfiguration, nameof(endpointConfiguration)).GetType();

        return _cache.GetOrAdd(
            configurationType,
            static (_, args) => new ProducerLogger(
                args.Factory.GetEnricher(
                    args.Configuration,
                    args.ServiceProvider)),
            (Configuration: endpointConfiguration, Factory: _enricherFactory, ServiceProvider: _serviceProvider));
    }
}
