// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.EnrichedMessages;

/// <summary>
///     Adds the <see cref="AddHeader{T}" />, <see cref="AddOrReplaceHeader{T}" />, <see cref="AddHeaderIfNotExists{T}" />, and
///     <see cref="WithMessageId{T}" /> methods to the message <see cref="object" />.
/// </summary>
public static class MessageWithHeadersExtensions
{
    /// <summary>
    ///     Adds a new header.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="value">
    ///     The header value.
    /// </param>
    /// <returns>
    ///     The <see cref="MessageWithHeaders{T}" /> so that additional calls can be chained.
    /// </returns>
    public static MessageWithHeaders<T> AddHeader<T>(this T message, string name, object value) =>
        new MessageWithHeaders<T>(message).AddHeader(name, value);

    /// <summary>
    ///     Adds a new header or replaces the header with the same name.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="newValue">
    ///     The new header value.
    /// </param>
    /// <returns>
    ///     The <see cref="MessageWithHeaders{T}" /> so that additional calls can be chained.
    /// </returns>
    public static MessageWithHeaders<T> AddOrReplaceHeader<T>(this T message, string name, object? newValue) =>
        new MessageWithHeaders<T>(message).AddOrReplaceHeader(name, newValue);

    /// <summary>
    ///     Adds a new header if no header with the same name is already set.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="newValue">
    ///     The new header value.
    /// </param>
    /// <returns>
    ///     The <see cref="IMessageWithHeaders" /> so that additional calls can be chained.
    /// </returns>
    public static MessageWithHeaders<T> AddHeaderIfNotExists<T>(this T message, string name, object? newValue) =>
        new MessageWithHeaders<T>(message).AddHeaderIfNotExists(name, newValue);

    /// <summary>
    ///     Sets the message id header (<see cref="DefaultMessageHeaders.MessageId" />.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="value">
    ///     The message id.
    /// </param>
    /// <returns>
    ///     The <see cref="MessageWithHeaders{T}" /> so that additional calls can be chained.
    /// </returns>
    public static MessageWithHeaders<T> WithMessageId<T>(this T message, object? value) =>
        new MessageWithHeaders<T>(message).WithMessageId(value);
}
