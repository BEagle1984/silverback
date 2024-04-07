// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.ExtensibleFactories;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Diagnostics;

/// <inheritdoc cref="IActivityEnricherFactory" />
internal sealed class ActivityEnricherFactory : TypeBasedExtensibleFactory<IBrokerActivityEnricher, EndpointConfiguration>, IActivityEnricherFactory
{
    /// <inheritdoc cref="IActivityEnricherFactory.GetEnricher" />
    public IBrokerActivityEnricher GetEnricher(EndpointConfiguration configuration, IServiceProvider serviceProvider) =>
        GetService(configuration, serviceProvider) ?? NullBrokerActivityEnricher.Instance;
}
