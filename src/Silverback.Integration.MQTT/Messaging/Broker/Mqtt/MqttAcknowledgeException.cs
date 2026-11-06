// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Broker.Mqtt;

/// <summary>
///     The exception that stops the channel reader when a consumed message cannot be acknowledged.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class MqttAcknowledgeException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttAcknowledgeException" /> class.
    /// </summary>
    public MqttAcknowledgeException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttAcknowledgeException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public MqttAcknowledgeException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttAcknowledgeException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public MqttAcknowledgeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
