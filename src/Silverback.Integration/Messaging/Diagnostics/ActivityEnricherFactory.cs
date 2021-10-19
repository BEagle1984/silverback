// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Diagnostics;

internal sealed class ActivityEnricherFactory : IActivityEnricherFactory
{
    private static readonly NullEnricher NullEnricherInstance = new();

    private readonly IServiceProvider _serviceProvider;

    private readonly ConcurrentDictionary<Type, Type> _enricherTypeCache = new();

    public ActivityEnricherFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IBrokerActivityEnricher GetActivityEnricher(EndpointConfiguration endpointConfiguration)
    {
        Type? enricherType = _enricherTypeCache.GetOrAdd(
            endpointConfiguration.GetType(),
            type => typeof(IBrokerActivityEnricher<>)
                .MakeGenericType(type));

        IBrokerActivityEnricher? activityEnricher = (IBrokerActivityEnricher?)_serviceProvider.GetService(enricherType);

        return activityEnricher ?? NullEnricherInstance;
    }

    private sealed class NullEnricher : IBrokerActivityEnricher
    {
        public void EnrichOutboundActivity(Activity activity, ProducerPipelineContext producerContext)
        {
            // Do nothing
        }

        public void EnrichInboundActivity(Activity activity, ConsumerPipelineContext consumerContext)
        {
            // Do nothing
        }
    }
}
