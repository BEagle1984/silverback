// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.ExtensibleFactories;
using Silverback.Messaging.Configuration;

namespace Silverback.Diagnostics;

/// <inheritdoc cref="IBrokerLogEnricherFactory" />
public sealed class BrokerLogEnricherFactory : TypeBasedExtensibleFactory<IBrokerLogEnricher, EndpointConfiguration>, IBrokerLogEnricherFactory
{
    /// <inheritdoc cref="IBrokerLogEnricherFactory.GetEnricher" />
    public IBrokerLogEnricher GetEnricher(EndpointConfiguration configuration, IServiceProvider serviceProvider) =>
        GetService(configuration, serviceProvider) ?? NullBrokerLogEnricher.Instance;
}
