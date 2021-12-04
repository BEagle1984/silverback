// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Outbound.Enrichers;

/// <summary>
///     Enriches the outbound message being moved.
/// </summary>
/// <typeparam name="TEndpoint">
///     The type of the endpoint that this enricher can be used for.
/// </typeparam>
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used for DI")]
public interface IMovePolicyMessageEnricher<TEndpoint> : IMovePolicyMessageEnricher
    where TEndpoint : Endpoint
{
}
