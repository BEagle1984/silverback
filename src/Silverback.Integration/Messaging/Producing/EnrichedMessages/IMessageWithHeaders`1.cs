// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.EnrichedMessages;

/// <summary>
///     Represents a message enriched with a collection of headers.
/// </summary>
/// <typeparam name="T">
///     The type of the message.
/// </typeparam>
public interface IMessageWithHeaders<T> : IMessageWithHeaders
{
    /// <inheritdoc cref="IMessageWithHeaders.Message" />
    new T? Message { get; }

    /// <inheritdoc cref="IMessageWithHeaders.Headers" />
    new MessageHeaderCollection Headers { get; }

    /// <summary>
    ///     Adds a new header.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="value">
    ///     The header value.
    /// </param>
    /// <returns>
    ///     The <see cref="MessageWithHeaders{T}" /> so that additional calls can be chained.
    /// </returns>
    MessageWithHeaders<T> AddHeader(string name, object value);

    /// <summary>
    ///     Removes all headers with the specified name.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <returns>
    ///     The <see cref="MessageWithHeaders{T}" /> so that additional calls can be chained.
    /// </returns>
    MessageWithHeaders<T> RemoveHeader(string name);

    /// <summary>
    ///     Adds a new header or replaces the header with the same name.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="newValue">
    ///     The new header value.
    /// </param>
    /// <returns>
    ///     The <see cref="MessageWithHeaders{T}" /> so that additional calls can be chained.
    /// </returns>
    MessageWithHeaders<T> AddOrReplaceHeader(string name, object? newValue);

    /// <summary>
    ///     Adds a new header if no header with the same name is already set.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="newValue">
    ///     The new header value.
    /// </param>
    /// <returns>
    ///     The <see cref="MessageWithHeaders{T}" /> so that additional calls can be chained.
    /// </returns>
    MessageWithHeaders<T> AddHeaderIfNotExists(string name, object? newValue);

    /// <summary>
    ///     Sets the message id header (<see cref="DefaultMessageHeaders.MessageId" />.
    /// </summary>
    /// <param name="value">
    ///     The message id.
    /// </param>
    /// <returns>
    ///     The <see cref="MessageWithHeaders{T}" /> so that additional calls can be chained.
    /// </returns>
    MessageWithHeaders<T> WithMessageId(object? value);
}
