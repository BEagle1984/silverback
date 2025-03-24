// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     Resolves the destination endpoint (e.g. the target topic and partition) for a message being produced.
/// </summary>
public interface IProducerEndpointResolver
{
    /// <summary>
    ///     Gets the raw endpoint name that can be used as <see cref="EndpointConfiguration.RawName" />.
    /// </summary>
    string RawName { get; }

    /// <summary>
    ///     Gets the computed actual destination endpoint for the message being produced.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <returns>
    ///     The <see cref="ProducerEndpoint" /> for the specified message.
    /// </returns>
    ProducerEndpoint GetEndpoint(IOutboundEnvelope envelope);
}
