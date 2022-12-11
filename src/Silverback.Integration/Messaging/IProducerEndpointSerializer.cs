// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
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
    ///     An array of bytes representing the serialized endpoint.
    /// </returns>
    ValueTask<byte[]> SerializeAsync(ProducerEndpoint endpoint);

    /// <summary>
    ///     Deserializes the <see cref="ProducerEndpoint" />.
    /// </summary>
    /// <param name="serializedEndpoint">
    ///     An array of bytes representing the serialized endpoint.
    /// </param>
    /// <param name="configuration">
    ///     The endpoint configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ProducerEndpoint" />.
    /// </returns>
    ValueTask<ProducerEndpoint> DeserializeAsync(byte[] serializedEndpoint, ProducerEndpointConfiguration configuration);
}
