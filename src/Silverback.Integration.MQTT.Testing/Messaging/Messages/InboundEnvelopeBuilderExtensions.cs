// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Adds the MQTT specific methods to the <see cref="InboundEnvelopeBuilder{TMessage}" />.
/// </summary>
public static class InboundEnvelopeBuilderExtensions
{
    /// <summary>
    ///     Sets the correlation data.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" />.
    /// </param>
    /// <param name="correlationData">
    ///     The correlation data.
    /// </param>
    /// <returns>
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public static InboundEnvelopeBuilder<TMessage> WithMqttCorrelationData<TMessage>(
        this InboundEnvelopeBuilder<TMessage> builder,
        byte[] correlationData)
        where TMessage : class =>
        Check.NotNull(builder, nameof(builder)).AddHeader(MqttMessageHeaders.CorrelationData, correlationData.ToBase64String());

    /// <summary>
    ///     Sets the topic from which the message was consumed.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" />.
    /// </param>
    /// <param name="topic">
    ///     The topic name.
    /// </param>
    /// <returns>
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public static InboundEnvelopeBuilder<TMessage> WithMqttTopic<TMessage>(
        this InboundEnvelopeBuilder<TMessage> builder,
        string topic)
        where TMessage : class =>
        Check.NotNull(builder, nameof(builder)).WithEndpoint(new MqttConsumerEndpoint(topic, new MqttConsumerEndpointConfiguration()));
}
