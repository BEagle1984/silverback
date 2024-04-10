// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     The exception that is thrown by the <see cref="IMessageSerializer" /> implementations when the
///     message cannot be serialized or deserialized. This exception is thrown only when a Silverback
///     specific error occurs, other exceptions related to reflection or the underlying serializer are not
///     wrapped.
/// </summary>
[SuppressMessage("Usage", "CA2237:Mark ISerializable types with serializable", Justification = "Not required anymore")]
[ExcludeFromCodeCoverage]
public class MessageSerializerException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageSerializerException" /> class.
    /// </summary>
    public MessageSerializerException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageSerializerException" /> class with the specified
    ///     message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public MessageSerializerException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageSerializerException" /> class with the specified
    ///     message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public MessageSerializerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
