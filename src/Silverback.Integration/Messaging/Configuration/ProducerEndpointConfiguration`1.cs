// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Producing.EndpointResolvers;

namespace Silverback.Messaging.Configuration;

/// <inheritdoc cref="ProducerEndpointConfiguration" />
public abstract record ProducerEndpointConfiguration<TEndpoint> : ProducerEndpointConfiguration
    where TEndpoint : ProducerEndpoint
{
    private readonly IProducerEndpointResolver<TEndpoint> _endpoint = NullProducerEndpointResolver<TEndpoint>.Instance;

    /// <inheritdoc cref="ProducerEndpointConfiguration.Endpoint" />
    public new IProducerEndpointResolver<TEndpoint> Endpoint
    {
        get => _endpoint;
        init => base.Endpoint = _endpoint = value;
    }
}
