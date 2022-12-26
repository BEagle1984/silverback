// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using MQTTnet;
using MQTTnet.Protocol;
using Silverback.Collections;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Builds the last will and testament (LWT) message related part of the <see cref="MqttClientConfiguration" />.
/// </summary>
/// <typeparam name="TMessage">
///     The LWT message type.
/// </typeparam>
public class MqttLastWillMessageConfigurationBuilder<TMessage>
{
    private readonly List<MqttUserProperty> _userProperties = new();

    private string? _topic;

    private object? _message;

    private MqttQualityOfServiceLevel _qosLevel;

    private IMessageSerializer? _serializer;

    private bool _retain;

    private uint? _delay;

    private string? _contentType;

    private byte[]? _correlationData;

    private MqttPayloadFormatIndicator? _payloadFormatIndicator;

    private string? _responseTopic;

    /// <summary>
    ///     Specifies the name of the topic to produce the LWT message to.
    /// </summary>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> ProduceTo(string topic)
    {
        _topic = Check.NotNullOrEmpty(topic, nameof(topic));
        return this;
    }

    /// <summary>
    ///     Sets the payload of the last will message to be published.
    /// </summary>
    /// <param name="message">
    ///     The actual LWT message to be published.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> Message(TMessage message)
    {
        _message = Check.NotNull(message, nameof(message));
        return this;
    }

    /// <summary>
    ///     Sets the last will message delay.
    /// </summary>
    /// t
    /// <param name="delay">
    ///     The <see cref="TimeSpan" /> representing the delay.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> WithDelay(TimeSpan delay)
    {
        Check.Range(delay, nameof(delay), TimeSpan.Zero, TimeSpan.FromSeconds(uint.MaxValue));

        _delay = (uint)delay.TotalSeconds;
        return this;
    }

    /// <summary>
    ///     Specifies the desired quality of service level.
    /// </summary>
    /// <param name="qosLevel">
    ///     The <see cref="MqttQualityOfServiceLevel" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> WithQualityOfServiceLevel(MqttQualityOfServiceLevel qosLevel)
    {
        _qosLevel = qosLevel;
        return this;
    }

    /// <summary>
    ///     Specifies that the LWT message has to be sent with the <i>at most once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> WithAtMostOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);

    /// <summary>
    ///     Specifies that the LWT message has to be sent with the <i>at least once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> WithAtLeastOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

    /// <summary>
    ///     Specifies that the LWT message has to be sent with the <i>exactly once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> WithExactlyOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);

    /// <summary>
    ///     Specifies that the LWT message will be sent with the retain flag, causing it to be persisted on the broker.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> Retain()
    {
        _retain = true;
        return this;
    }

    /// <summary>
    ///     Specifies the <see cref="IMessageSerializer" /> to be used to serialize the LWT message.
    /// </summary>
    /// <param name="serializer">
    ///     The <see cref="IMessageSerializer" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> SerializeUsing(IMessageSerializer serializer)
    {
        _serializer = serializer;
        return this;
    }

    /// <summary>
    ///     Sets the serializer to an instance of <see cref="JsonMessageSerializer{TMessage}" /> (or
    ///     <see cref="JsonMessageSerializer{TMessage}" />) to serialize the produced messages as JSON.
    /// </summary>
    /// <param name="serializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="JsonMessageSerializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> SerializeAsJson(Action<JsonMessageSerializerBuilder>? serializerBuilderAction = null)
    {
        JsonMessageSerializerBuilder serializerBuilder = new();

        if (typeof(TMessage) != typeof(object))
            serializerBuilder.UseFixedType<TMessage>();

        serializerBuilderAction?.Invoke(serializerBuilder);
        return SerializeUsing(serializerBuilder.Build());
    }

    /// <summary>
    ///     Sets the content type.
    /// </summary>
    /// <param name="contentType">
    ///     The content type.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> WithContentType(string? contentType)
    {
        _contentType = contentType;
        return this;
    }

    /// <summary>
    ///     Sets the correlation data.
    /// </summary>
    /// <param name="correlationData">
    ///     The correlation data.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> WithCorrelationData(byte[]? correlationData)
    {
        _correlationData = correlationData;
        return this;
    }

    /// <summary>
    ///     Sets the payload format indicator.
    /// </summary>
    /// <param name="payloadFormatIndicator">
    ///     The payload format indicator.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> WithPayloadFormatIndicator(MqttPayloadFormatIndicator? payloadFormatIndicator)
    {
        _payloadFormatIndicator = payloadFormatIndicator;
        return this;
    }

    /// <summary>
    ///     Sets the response topic.
    /// </summary>
    /// <param name="topic">
    ///     The response topic.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> WithPayloadFormatIndicator(string? topic)
    {
        _responseTopic = topic;
        return this;
    }

    /// <summary>
    ///     Adds a user property to the last will message.
    /// </summary>
    /// <param name="name">
    ///     The property name.
    /// </param>
    /// <param name="value">
    ///     The property value.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttLastWillMessageConfigurationBuilder<TMessage> AddUserProperty(string name, string? value)
    {
        Check.NotNull(name, nameof(name));

        _userProperties.Add(new MqttUserProperty(name, value));
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="MqttApplicationMessage" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttApplicationMessage" />.
    /// </returns>
    public MqttLastWillMessageConfiguration Build()
    {
        _serializer ??= DefaultSerializers.Json;
        Stream? payloadStream = null;

        if (!string.IsNullOrEmpty(_topic))
        {
            MqttProducerEndpoint endpoint = new MqttStaticProducerEndpointResolver(_topic).GetEndpoint(new MqttProducerEndpointConfiguration());
            payloadStream = AsyncHelper.RunSynchronously(
                () => _serializer.SerializeAsync(_message, new MessageHeaderCollection(), endpoint));
        }

        MqttLastWillMessageConfiguration configuration = new()
        {
            Topic = _topic ?? string.Empty,
            Payload = payloadStream.ReadAll(),
            QualityOfServiceLevel = _qosLevel,
            Retain = _retain,
            ContentType = _contentType,
            CorrelationData = _correlationData,
            ResponseTopic = _responseTopic,
            UserProperties = _userProperties.AsValueReadOnlyCollection()
        };

        if (_delay.HasValue)
            configuration = configuration with { Delay = _delay.Value };

        if (_payloadFormatIndicator.HasValue)
            configuration = configuration with { PayloadFormatIndicator = _payloadFormatIndicator.Value };

        return configuration;
    }
}
