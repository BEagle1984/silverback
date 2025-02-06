// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The exception that is thrown when an error occurs while processing the outbox messages.
/// </summary>
public class OutboxProcessingException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxProcessingException" /> class.
    /// </summary>
    public OutboxProcessingException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxProcessingException" /> class with the specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public OutboxProcessingException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxProcessingException" /> class with the specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public OutboxProcessingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
