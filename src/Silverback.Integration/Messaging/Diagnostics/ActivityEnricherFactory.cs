// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.ExtensibleFactories;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Diagnostics;

/// <inheritdoc cref="IActivityEnricherFactory" />
internal sealed class ActivityEnricherFactory : TypeBasedExtensibleFactory<IBrokerActivityEnricher, EndpointConfiguration>, IActivityEnricherFactory
{
    /// <inheritdoc cref="IActivityEnricherFactory.GetEnricher" />
    public IBrokerActivityEnricher GetEnricher(EndpointConfiguration configuration) =>
        GetService(configuration) ?? NullBrokerActivityEnricher.Instance;
}
