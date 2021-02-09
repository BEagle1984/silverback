// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Diagnostics
{
    internal class ActivityEnricherFactory : IActivityEnricherFactory
    {
        private static readonly NullEnricher NullEnricherInstance = new();

        private readonly IServiceProvider _serviceProvider;

        private readonly ConcurrentDictionary<Type, Type> _enricherTypeCache = new();

        public ActivityEnricherFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IBrokerActivityEnricher GetActivityEnricher(Type endpointType)
        {
            Type closedEnricherType = _enricherTypeCache.GetOrAdd(
                endpointType,
                t => typeof(IBrokerActivityEnricher<>).MakeGenericType(t));

            IBrokerActivityEnricher? activityEnricher =
                (IBrokerActivityEnricher?)_serviceProvider.GetService(closedEnricherType);

            return activityEnricher ?? NullEnricherInstance;
        }

        private class NullEnricher : IBrokerActivityEnricher
        {
            public void EnrichOutboundActivity(Activity activity, ProducerPipelineContext outboundEnvelope)
            {
            }

            public void EnrichInboundActivity(Activity activity, ConsumerPipelineContext inboundEnvelope)
            {
            }
        }
    }
}
