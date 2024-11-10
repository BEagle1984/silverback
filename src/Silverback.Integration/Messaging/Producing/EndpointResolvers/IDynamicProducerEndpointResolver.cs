// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     Dynamically resolves the destination endpoint (e.g. the target topic and partition) for each message being produced.
/// </summary>
public interface IDynamicProducerEndpointResolver : IProducerEndpointResolver
{
    /// <summary>
    ///     Gets the string representation of the computed actual destination endpoint for the message being produced.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <returns>
    ///     The <see cref="ProducerEndpoint" /> for the specified message.
    /// </returns>
    string GetSerializedEndpoint(IOutboundEnvelope envelope);
}
