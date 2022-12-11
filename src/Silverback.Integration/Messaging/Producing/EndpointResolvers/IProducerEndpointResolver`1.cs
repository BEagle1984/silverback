// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <inheritdoc cref="IProducerEndpointResolver" />
/// <typeparam name="TEndpoint">
///     The type of the endpoint being resolved.
/// </typeparam>
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Type parameter used for better constraints in ProducerEndpoint")]
public interface IProducerEndpointResolver<TEndpoint> : IProducerEndpointResolver
    where TEndpoint : ProducerEndpoint
{
}
