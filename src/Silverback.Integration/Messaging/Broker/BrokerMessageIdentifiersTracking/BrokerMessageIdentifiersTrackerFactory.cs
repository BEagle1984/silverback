// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.ExtensibleFactories;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Broker.BrokerMessageIdentifiersTracking;

/// <inheritdoc cref="IBrokerMessageIdentifiersTrackerFactory" />
public sealed class BrokerMessageIdentifiersTrackerFactory : TypeBasedExtensibleFactory<IBrokerMessageIdentifiersTracker, EndpointConfiguration>, IBrokerMessageIdentifiersTrackerFactory
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrokerMessageIdentifiersTrackerFactory" /> class.
    /// </summary>
    public BrokerMessageIdentifiersTrackerFactory()
        : base(false)
    {
    }

    /// <inheritdoc cref="IBrokerMessageIdentifiersTrackerFactory.GetTracker" />
    public IBrokerMessageIdentifiersTracker GetTracker(EndpointConfiguration configuration, IServiceProvider serviceProvider) =>
        GetService(configuration, serviceProvider) ?? new SimpleMessageIdentifiersTracker();
}
