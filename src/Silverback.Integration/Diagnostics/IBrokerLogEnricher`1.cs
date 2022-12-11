// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration;

namespace Silverback.Diagnostics;

/// <inheritdoc cref="IBrokerLogEnricher" />
/// <typeparam name="TEndpoint">
///     The type of the endpoint that this enricher can be used for.
/// </typeparam>
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used for DI")]
public interface IBrokerLogEnricher<TEndpoint> : IBrokerLogEnricher
    where TEndpoint : EndpointConfiguration
{
}
