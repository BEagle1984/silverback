// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Outbound.EndpointResolvers;

/// <summary>
///     A type used to resolve the target topic for the outbound message.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being produced.
/// </typeparam>
public interface IMqttProducerEndpointResolver<TMessage>
{
    /// <summary>
    ///     Gets the target topic for the message being produced.
    /// </summary>
    /// <param name="message">
    ///     The message being produced.
    /// </param>
    /// <returns>
    ///     The target topic.
    /// </returns>
    string GetTopic(TMessage? message);
}
