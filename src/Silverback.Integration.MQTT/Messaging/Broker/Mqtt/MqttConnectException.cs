// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using MQTTnet;

namespace Silverback.Messaging.Broker.Mqtt;

/// <summary>
///     The exception that is thrown when the MQTT client connection fails.
/// </summary>
[ExcludeFromCodeCoverage]
public class MqttConnectException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttConnectException" /> class.
    /// </summary>
    public MqttConnectException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttConnectException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public MqttConnectException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttConnectException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="result">
    ///     The <see cref="MqttClientPublishResult" />.
    /// </param>
    public MqttConnectException(string message, MqttClientPublishResult result)
        : base(message)
    {
        Result = result;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttConnectException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public MqttConnectException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Gets the <see cref="MqttClientPublishResult" /> of the failed publish operation.
    /// </summary>
    public MqttClientPublishResult? Result { get; }
}
