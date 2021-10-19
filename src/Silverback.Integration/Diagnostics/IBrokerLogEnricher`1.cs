// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Provides enrichment for the logs written in the context of the consumers and producers, for the specified endpoint type.
    /// </summary>
    /// <typeparam name="TEndpoint">
    ///     The type of the endpoint that this enricher can be used for.
    /// </typeparam>
    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used for DI")]
    public interface IBrokerLogEnricher<TEndpoint> : IBrokerLogEnricher
        where TEndpoint : EndpointConfiguration
    {
    }
}
