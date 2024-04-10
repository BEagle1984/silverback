// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Validation;

/// <summary>
///     The exception that is thrown when the message is not valid and the validation mode is
///     <see cref="MessageValidationMode.ThrowException" />.
/// </summary>
[SuppressMessage("Usage", "CA2237:Mark ISerializable types with serializable", Justification = "Not required anymore")]
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
}
