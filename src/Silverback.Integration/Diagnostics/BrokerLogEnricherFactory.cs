// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.ExtensibleFactories;
using Silverback.Messaging.Configuration;

namespace Silverback.Diagnostics;

/// <inheritdoc cref="IBrokerLogEnricherFactory.GetEnricher" />
public class BrokerLogEnricherFactory : TypeBasedExtensibleFactory<IBrokerLogEnricher, EndpointConfiguration>, IBrokerLogEnricherFactory
{
    /// <inheritdoc cref="IBrokerLogEnricherFactory.GetEnricher" />
    public IBrokerLogEnricher GetEnricher(EndpointConfiguration configuration) =>
        GetService(configuration) ?? NullBrokerLogEnricher.Instance;
}
