// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     A type used to resolve the target topic and partition for the outbound message.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being produced.
/// </typeparam>
public interface IKafkaProducerEndpointResolver<in TMessage>
{
    /// <summary>
    ///     Gets the target topic and partition for the message being produced.
    /// </summary>
    /// <param name="message">
    ///     The message being produced.
    /// </param>
    /// <returns>
    ///     The target <see cref="TopicPartition" />.
    /// </returns>
    TopicPartition GetTopicPartition(TMessage? message);
}
