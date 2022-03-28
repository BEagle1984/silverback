// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using MQTTnet;
using MQTTnet.Protocol;
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
    private string? _topic;

    private object? _message;

    private MqttQualityOfServiceLevel _qosLevel;

    private IMessageSerializer? _serializer;

    private bool _retain;

    /// <summary>
    ///     Gets the desired delay in seconds.
    /// </summary>
    public uint? Delay { get; private set; }

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
    ///     Specifies the LWT message to be published.
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
    ///     Specifies the LWT message delay.
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

        Delay = (uint)delay.TotalSeconds;
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
            payloadStream = AsyncHelper
                .RunSynchronously(() => _serializer.SerializeAsync(_message, new MessageHeaderCollection(), endpoint).AsTask());
        }

        return new MqttLastWillMessageConfiguration
        {
            Topic = _topic ?? string.Empty,
            Payload = payloadStream.ReadAll(),
            QualityOfServiceLevel = _qosLevel,
            Retain = _retain
        };
    }
}
