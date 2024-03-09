// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.EnrichedMessages;

/// <inheritdoc cref="IMessageWithHeaders{T}" />
public class MessageWithHeaders<T> : IMessageWithHeaders<T>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageWithHeaders{T}" /> class.
    /// </summary>
    /// <param name="message">
    ///     The message.
    /// </param>
    public MessageWithHeaders(T message)
    {
        Message = message;
    }

    /// <inheritdoc cref="IMessageWrapper{T}.Message" />
    public T? Message { get; }

    /// <inheritdoc cref="IMessageWithHeaders.Headers" />
    public MessageHeaderCollection Headers { get; } = new();

    /// <inheritdoc cref="IMessageWithHeaders{T}.AddHeader" />
    public MessageWithHeaders<T> AddHeader(string name, object value)
    {
        Headers.Add(name, value);
        return this;
    }

    /// <inheritdoc cref="IMessageWithHeaders{T}.RemoveHeader" />
    public MessageWithHeaders<T> RemoveHeader(string name)
    {
        Headers.Remove(name);
        return this;
    }

    /// <inheritdoc cref="IMessageWithHeaders{T}.AddOrReplaceHeader" />
    public MessageWithHeaders<T> AddOrReplaceHeader(string name, object? newValue)
    {
        Headers.AddOrReplace(name, newValue);
        return this;
    }

    /// <inheritdoc cref="IMessageWithHeaders{T}.AddHeaderIfNotExists" />
    public MessageWithHeaders<T> AddHeaderIfNotExists(string name, object? newValue)
    {
        Headers.AddIfNotExists(name, newValue);
        return this;
    }

    /// <inheritdoc cref="IMessageWithHeaders{T}.WithMessageId" />
    public MessageWithHeaders<T> WithMessageId(object? value) => AddOrReplaceHeader(DefaultMessageHeaders.MessageId, value);
}
