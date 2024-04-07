// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Messaging;

/// <summary>
///     Serializes and deserializes the producer endpoints when they need to be stored.
/// </summary>
public interface IProducerEndpointSerializer
{
    /// <summary>
    ///     Serializes the <see cref="ProducerEndpoint" />.
    /// </summary>
    /// <param name="endpoint">
    ///     The endpoint to be serialized.
    /// </param>
    /// <returns>
    ///     The serialized endpoint.
    /// </returns>
    string Serialize(ProducerEndpoint endpoint);

    /// <summary>
    ///     Deserializes the <see cref="ProducerEndpoint" />.
    /// </summary>
    /// <param name="serializedEndpoint">
    ///     The serialized endpoint.
    /// </param>
    /// <param name="configuration">
    ///     The endpoint configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ProducerEndpoint" />.
    /// </returns>
    ProducerEndpoint Deserialize(string serializedEndpoint, ProducerEndpointConfiguration configuration);
}
