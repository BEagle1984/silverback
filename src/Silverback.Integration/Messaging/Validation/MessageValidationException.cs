// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Silverback.Messaging.Validation;

/// <summary>
///     The exception that is thrown when the message is not valid and the validation mode is
///     <see cref="MessageValidationMode.ThrowException" />.
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class MessageValidationException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageValidationException" /> class.
    /// </summary>
    public MessageValidationException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageValidationException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public MessageValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageValidationException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public MessageValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageValidationException" /> class with the
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
    protected MessageValidationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
