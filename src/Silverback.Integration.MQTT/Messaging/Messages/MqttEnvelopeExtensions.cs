// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Adds some convenience extension methods to the envelope interfaces.
/// </summary>
public static class MqttEnvelopeExtensions
{
    /// <summary>
    ///     Gets response topic.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The response topic.
    /// </returns>
    public static string? GetResponseTopic(this IBrokerEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).Headers.GetValue(MqttMessageHeaders.ResponseTopic);

    /// <summary>
    ///     Sets the response topic.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <param name="responseTopic">
    ///     The response topic.
    /// </param>
    /// <returns>
    ///     The <see cref="IRawOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public static IRawOutboundEnvelope SetResponseTopic(this IRawOutboundEnvelope envelope, string responseTopic)
    {
        Check.NotNull(envelope, nameof(envelope));
        envelope.Headers.AddOrReplace(MqttMessageHeaders.ResponseTopic, responseTopic);
        return envelope;
    }

    /// <summary>
    ///     Gets the correlation data.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The correlation data.
    /// </returns>
    public static byte[]? GetCorrelationData(this IBrokerEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).Headers.GetValue(MqttMessageHeaders.CorrelationData).FromBase64String();

    /// <summary>
    ///     Gets the correlation data as UTF-8 string.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The correlation data.
    /// </returns>
    public static string? GetCorrelationDataAsString(this IBrokerEnvelope envelope) =>
        GetCorrelationData(envelope).ToUtf8String();

    /// <summary>
    ///     Sets the correlation data.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <param name="correlationData">
    ///     The correlation data.
    /// </param>
    /// <returns>
    ///     The <see cref="IRawOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public static IRawOutboundEnvelope SetCorrelationData(this IRawOutboundEnvelope envelope, byte[]? correlationData)
    {
        Check.NotNull(envelope, nameof(envelope));
        envelope.Headers.AddOrReplace(MqttMessageHeaders.CorrelationData, correlationData.ToBase64String());
        return envelope;
    }

    /// <summary>
    ///     Sets the correlation data.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <param name="correlationData">
    ///     The correlation data.
    /// </param>
    /// <returns>
    ///     The <see cref="IRawOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public static IRawOutboundEnvelope SetCorrelationData(this IRawOutboundEnvelope envelope, string? correlationData) =>
        SetCorrelationData(envelope, correlationData.ToUtf8Bytes());
}
