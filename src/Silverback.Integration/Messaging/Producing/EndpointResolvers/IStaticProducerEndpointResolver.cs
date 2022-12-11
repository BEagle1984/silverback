// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     Statically resolves to the same target endpoint (e.g. the target topic and partition) for every message being produced.
/// </summary>
public interface IStaticProducerEndpointResolver : IProducerEndpointResolver
{
    /// <summary>
    ///     Gets the static target endpoint.
    /// </summary>
    /// <param name="configuration">
    ///     The producer endpoint configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ProducerEndpoint" />.
    /// </returns>
    ProducerEndpoint GetEndpoint(ProducerEndpointConfiguration configuration);
}
