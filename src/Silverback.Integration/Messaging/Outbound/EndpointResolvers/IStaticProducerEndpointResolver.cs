// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Outbound.EndpointResolvers;

/// <summary>
///     Statically resolves to the same target endpoint (e.g. the target topic and partition) for every message being produced.
/// </summary>
public interface IStaticProducerEndpointResolver : IProducerEndpointResolver
{
    /// <summary>
    ///     Gets the static target endpoint.
    /// </summary>
    /// <param name="configuration">
    ///     The producer configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ProducerEndpoint" />.
    /// </returns>
    ProducerEndpoint GetEndpoint(ProducerConfiguration configuration);
}
