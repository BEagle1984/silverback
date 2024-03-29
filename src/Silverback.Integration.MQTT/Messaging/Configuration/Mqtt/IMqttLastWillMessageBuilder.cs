﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet.Protocol;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration.Mqtt
{
    /// <summary>
    ///     Builds the last will and testament (LWT) message related part of the <see cref="MqttClientConfig" />.
    /// </summary>
    public interface IMqttLastWillMessageBuilder
    {
        /// <summary>
        ///     Specifies the name of the topic to produce the LWT message to.
        /// </summary>
        /// <param name="topicName">
        ///     The name of the topic.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder ProduceTo(string topicName);

        /// <summary>
        ///     Specifies the LWT message to be published.
        /// </summary>
        /// t
        /// <param name="message">
        ///     The actual LWT message to be published.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder Message(object message);

        /// <summary>
        ///     Specifies the LWT message delay.
        /// </summary>
        /// t
        /// <param name="delay">
        ///     The <see cref="TimeSpan" /> representing the delay.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder WithDelay(TimeSpan delay);

        /// <summary>
        ///     Specifies the desired quality of service level.
        /// </summary>
        /// t
        /// <param name="qosLevel">
        ///     The <see cref="MqttQualityOfServiceLevel" />.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder WithQualityOfServiceLevel(MqttQualityOfServiceLevel qosLevel);

        /// <summary>
        ///     Specifies that the LWT message has to be sent with the <i>at most once</i> quality of service level.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder WithAtMostOnceQoS();

        /// <summary>
        ///     Specifies that the LWT message has to be sent with the <i>at least once</i> quality of service level.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder WithAtLeastOnceQoS();

        /// <summary>
        ///     Specifies that the LWT message has to be sent with the <i>exactly once</i> quality of service level.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder WithExactlyOnceQoS();

        /// <summary>
        ///     Specifies that the LWT message will be sent with the retain flag, causing it to be persisted on the broker.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder"/> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder Retain();

        /// <summary>
        ///     Specifies the <see cref="IMessageSerializer" /> to be used to serialize the LWT message.
        /// </summary>
        /// <param name="serializer">
        ///     The <see cref="IMessageSerializer" />.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder"/> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder SerializeUsing(IMessageSerializer serializer);

        /// <summary>
        ///     <para>
        ///         Sets the serializer to an instance of <see cref="JsonMessageSerializer" /> (or
        ///         <see cref="JsonMessageSerializer{TMessage}" />) to serialize the produced messages as JSON.
        ///     </para>
        /// </summary>
        /// <param name="serializerBuilderAction">
        ///     An optional <see cref="Action{T}" /> that takes the <see cref="IJsonMessageSerializerBuilder" /> and
        ///     configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder"/> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder SerializeAsJson(Action<IJsonMessageSerializerBuilder>? serializerBuilderAction = null);

        /// <summary>
        ///     Specifies the content type.
        /// </summary>
        /// <param name="contentType">
        ///     The content type.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder"/> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder WithContentType(string? contentType);

        /// <summary>
        ///     Specifies the correlation data.
        /// </summary>
        /// <param name="correlationData">
        ///     The correlation data.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder"/> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder WithCorrelationData(byte[]? correlationData);

        /// <summary>
        ///     Specifies the response topic.
        /// </summary>
        /// <param name="topic">
        ///     The response topic.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder"/> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder WithResponseTopic(string? topic);

        /// <summary>
        ///     Specifies the payload format indicator.
        /// </summary>
        /// <param name="formatIndicator">
        ///     The format indicator.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder"/> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder WithPayloadFormatIndicator(MqttPayloadFormatIndicator formatIndicator);

        /// <summary>
        ///     Adds a user property to be sent with the will message.
        /// </summary>
        /// <param name="name">
        ///     The property name.
        /// </param>
        /// <param name="value">
        ///     The property value.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttLastWillMessageBuilder"/> so that additional calls can be chained.
        /// </returns>
        IMqttLastWillMessageBuilder AddUserProperty(string name, string value);
    }
}
