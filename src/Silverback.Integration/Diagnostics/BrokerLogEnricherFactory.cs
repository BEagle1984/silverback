// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics
{
    internal sealed class BrokerLogEnricherFactory
    {
        private static readonly NullEnricher NullEnricherInstance = new();

        private readonly IServiceProvider _serviceProvider;

        private readonly ConcurrentDictionary<Type, Type> _enricherTypeCache = new();

        public BrokerLogEnricherFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IBrokerLogEnricher GetLogEnricher(IEndpoint endpoint)
        {
            var enricherType = _enricherTypeCache.GetOrAdd(
                endpoint.GetType(),
                type => typeof(IBrokerLogEnricher<>)
                    .MakeGenericType(type));

            var logEnricher = (IBrokerLogEnricher?)_serviceProvider.GetService(enricherType);

            return logEnricher ?? NullEnricherInstance;
        }

        private sealed class NullEnricher : IBrokerLogEnricher
        {
            public string AdditionalPropertyName1 => "unused1";

            public string AdditionalPropertyName2 => "unused2";

            public (string? Value1, string? Value2) GetAdditionalValues(
                IEndpoint endpoint,
                IReadOnlyCollection<MessageHeader>? headers,
                IBrokerMessageIdentifier? brokerMessageIdentifier) =>
                (null, null);
        }
    }
}
