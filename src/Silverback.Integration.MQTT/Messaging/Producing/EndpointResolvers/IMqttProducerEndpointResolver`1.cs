// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     A type used to resolve the target topic for the outbound message.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being produced.
/// </typeparam>
public interface IMqttProducerEndpointResolver<in TMessage>
{
    /// <summary>
    ///     Gets the target topic for the message being produced.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <returns>
    ///     The target topic.
    /// </returns>
    string GetTopic(IOutboundEnvelope<TMessage> envelope);
}
