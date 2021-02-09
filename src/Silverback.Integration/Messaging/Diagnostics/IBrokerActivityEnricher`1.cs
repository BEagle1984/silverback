// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     Interface to resolve the matching <see cref="IBrokerActivityEnricher" /> for a given endpoint Type.
    /// </summary>
    /// <typeparam name="TEndpoint">The Endpoint type the this enricher can be used for.</typeparam>
    public interface IBrokerActivityEnricher<TEndpoint> : IBrokerActivityEnricher
        where TEndpoint : Endpoint
    {
    }
}
