﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using MQTTnet.Client.Publishing;

namespace Silverback.Messaging.Broker.Mqtt;

/// <summary>
///     The exception that is thrown when the result of a publish to the MQTT broker doesn't indicate success.
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class MqttProduceException : ProduceException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttProduceException" /> class.
    /// </summary>
    public MqttProduceException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttProduceException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public MqttProduceException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttProduceException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="result">
    ///     The <see cref="MqttClientPublishResult" />.
    /// </param>
    public MqttProduceException(string message, MqttClientPublishResult result)
        : base(message)
    {
        Result = result;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttProduceException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public MqttProduceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttProduceException" /> class with the
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
    protected MqttProduceException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    /// <summary>
    ///     Gets the <see cref="MqttClientPublishResult" /> of the failed publish operation.
    /// </summary>
    public MqttClientPublishResult? Result { get; }
}
