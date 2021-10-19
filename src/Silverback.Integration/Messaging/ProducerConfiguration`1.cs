// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Outbound.EndpointResolvers;

namespace Silverback.Messaging;

/// <inheritdoc cref="ProducerConfiguration" />
public abstract record ProducerConfiguration<TEndpoint> : ProducerConfiguration
    where TEndpoint : ProducerEndpoint
{
    private readonly IProducerEndpointResolver<TEndpoint> _endpoint = NullProducerEndpointResolver<TEndpoint>.Instance;

    /// <inheritdoc cref="ProducerConfiguration.Endpoint" />
    public new IProducerEndpointResolver<TEndpoint> Endpoint
    {
        get => _endpoint;
        init => base.Endpoint = _endpoint = value;
    }
}
