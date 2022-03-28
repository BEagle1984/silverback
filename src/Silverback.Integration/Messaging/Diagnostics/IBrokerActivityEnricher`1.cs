// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Diagnostics;

/// <summary>
///     Provides enrichment for activities produced by the <see cref="ActivityProducerBehavior" /> and
///     <see cref="ActivityConsumerBehavior" /> for the specified endpoint type.
/// </summary>
/// <typeparam name="TConfiguration">
///     The type of the endpoint configuration that this enricher can be used with.
/// </typeparam>
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used for DI")]
public interface IBrokerActivityEnricher<TConfiguration> : IBrokerActivityEnricher
    where TConfiguration : EndpointConfiguration
{
}
