// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Producing.EndpointResolvers;

namespace Silverback.Messaging.Configuration;

/// <inheritdoc cref="ProducerEndpointConfiguration" />
public abstract record ProducerEndpointConfiguration<TEndpoint> : ProducerEndpointConfiguration
    where TEndpoint : ProducerEndpoint
{
    private readonly IProducerEndpointResolver<TEndpoint> _endpoint = NullProducerEndpointResolver<TEndpoint>.Instance;

    /// <inheritdoc cref="ProducerEndpointConfiguration.EndpointResolver" />
    public new IProducerEndpointResolver<TEndpoint> EndpointResolver
    {
        get => _endpoint;
        init => base.EndpointResolver = _endpoint = value;
    }
}
