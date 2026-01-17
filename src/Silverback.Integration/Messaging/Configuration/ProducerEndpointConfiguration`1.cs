// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Producing.EndpointResolvers;

namespace Silverback.Messaging.Configuration;

/// <inheritdoc cref="ProducerEndpointConfiguration" />
public abstract record ProducerEndpointConfiguration<TEndpoint> : ProducerEndpointConfiguration
    where TEndpoint : ProducerEndpoint
{
    /// <inheritdoc cref="ProducerEndpointConfiguration.EndpointResolver" />
    public new IProducerEndpointResolver<TEndpoint> EndpointResolver
    {
        get;
        init => base.EndpointResolver = field = value;
    } = NullProducerEndpointResolver<TEndpoint>.Instance;
}
