// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Outbound.EndpointResolvers;

/// <inheritdoc cref="IProducerEndpointResolver" />
/// <typeparam name="TEndpoint">
///     The type of the endpoint being resolved.
/// </typeparam>
public interface IProducerEndpointResolver<TEndpoint> : IProducerEndpointResolver
    where TEndpoint : ProducerEndpoint
{
}
