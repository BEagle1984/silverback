// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <summary>
///     Declares the internal methods of the <see cref="MqttInboundEnvelope{TMessage,TCorrelationData}" /> not meant for public use.
/// </summary>
internal interface IInternalMqttInboundEnvelope : IMqttInboundEnvelope
{
    /// <summary>
    ///     Sets the correlation data.
    /// </summary>
    /// <param name="correlationData">
    ///     The correlation data.
    /// </param>
    /// <returns>
    ///     The <see cref="IMqttInboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IMqttInboundEnvelope SetCorrelationData(object correlationData);

    /// <summary>
    ///     Sets the correlation data.
    /// </summary>
    /// <param name="rawCorrelationData">
    ///     The serialized correlation data.
    /// </param>
    /// <returns>
    ///     The <see cref="IMqttInboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IMqttInboundEnvelope SetRawCorrelationData(byte[]? rawCorrelationData);

    /// <summary>
    ///     Sets the response topic.
    /// </summary>
    /// <param name="topic">
    ///     The response topic.
    /// </param>
    /// <returns>
    ///     The <see cref="IMqttInboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IMqttInboundEnvelope SetResponseTopic(string topic);
}
