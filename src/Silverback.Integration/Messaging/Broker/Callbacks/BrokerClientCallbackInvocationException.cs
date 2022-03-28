// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     The exception that wraps the exception thrown by a <see cref="IBrokerClientCallback" />.
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class BrokerClientCallbackInvocationException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrokerClientCallbackInvocationException" /> class.
    /// </summary>
    public BrokerClientCallbackInvocationException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrokerClientCallbackInvocationException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public BrokerClientCallbackInvocationException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrokerClientCallbackInvocationException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public BrokerClientCallbackInvocationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrokerClientCallbackInvocationException" /> class with the
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
    protected BrokerClientCallbackInvocationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
