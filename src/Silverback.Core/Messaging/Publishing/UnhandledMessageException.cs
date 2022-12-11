// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     The exception that is thrown when some published messages aren't handled by any registered
///     subscriber.
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class UnhandledMessageException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UnhandledMessageException" /> class.
    /// </summary>
    public UnhandledMessageException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnhandledMessageException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public UnhandledMessageException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnhandledMessageException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="unhandledMessage">
    ///     The message that wasn't handled.
    /// </param>
    public UnhandledMessageException(object unhandledMessage)
        : base(GetErrorMessage(unhandledMessage))
    {
        UnhandledMessage = unhandledMessage;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnhandledMessageException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public UnhandledMessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnhandledMessageException" /> class with the
    ///     serialized data.
    /// </summary>
    /// <param name="info">
    ///     The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being
    ///     thrown.
    /// </param>
    /// <param name="context">
    ///     The <see cref="StreamingContext" /> that contains contextual information about the source or
    ///     destination.
    /// </param>
    protected UnhandledMessageException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    /// <summary>
    ///     Gets the message that wasn't handled.
    /// </summary>
    public object? UnhandledMessage { get; }

    private static string GetErrorMessage(object unhandledMessage) =>
        $"No subscriber could be found to handle the message of type {unhandledMessage.GetType().FullName}.";
}
