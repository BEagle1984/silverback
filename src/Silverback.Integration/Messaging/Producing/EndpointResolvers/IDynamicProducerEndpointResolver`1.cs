// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <inheritdoc cref="IStaticProducerEndpointResolver" />
/// <typeparam name="TEndpoint">
///     The type of the endpoint being resolved.
/// </typeparam>
public interface IDynamicProducerEndpointResolver<TEndpoint> : IDynamicProducerEndpointResolver, IProducerEndpointResolver<TEndpoint>
    where TEndpoint : ProducerEndpoint;
