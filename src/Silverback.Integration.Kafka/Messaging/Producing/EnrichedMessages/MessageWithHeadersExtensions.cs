// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Util;

namespace Silverback.Messaging.Producing.EnrichedMessages;

/// <summary>
///     Adds the <see cref="WithKafkaKey{T}(MessageWithHeaders{T}, object)" /> and <see cref="WithKafkaKey{T}(T, object)" /> method to the
///     <see cref="MessageWithHeaders{T}" /> and the message <see cref="object" />.
/// </summary>
public static class MessageWithHeadersExtensions
{
    /// <summary>
    ///     Sets the Kafka key.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="value">
    ///     The Kafka key value.
    /// </param>
    /// <returns>
    ///     The <see cref="MessageWithHeaders{T}" /> so that additional calls can be chained.
    /// </returns>
    public static MessageWithHeaders<T> WithKafkaKey<T>(this MessageWithHeaders<T> message, object? value) =>
        Check.NotNull(message, nameof(message)).WithMessageId(value);

    /// <summary>
    ///     Sets the Kafka key.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="value">
    ///     The Kafka key value.
    /// </param>
    /// <returns>
    ///     The <see cref="MessageWithHeaders{T}" /> so that additional calls can be chained.
    /// </returns>
    public static MessageWithHeaders<T> WithKafkaKey<T>(this T message, object? value) =>
        message.WithMessageId(value);
}
