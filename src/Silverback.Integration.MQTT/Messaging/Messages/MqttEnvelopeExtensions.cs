// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Adds some convenience extension methods to the envelope interfaces.
/// </summary>
// TODO: Ensure all is tested
public static class MqttEnvelopeExtensions
{
    public static IMqttInboundEnvelope AsMqttEnvelope(this IInboundEnvelope envelope) =>
        Check.IsOfType<IMqttInboundEnvelope>(envelope, nameof(envelope));

    public static IMqttInboundEnvelope<TMessage> AsMqttEnvelope<TMessage>(this IInboundEnvelope envelope) =>
        Check.IsOfType<IMqttInboundEnvelope<TMessage>>(envelope, nameof(envelope));

    public static IMqttInboundEnvelope<TMessage, TCorrelationData> AsMqttEnvelope<TMessage, TCorrelationData>(this IInboundEnvelope envelope) =>
        Check.IsOfType<IMqttInboundEnvelope<TMessage, TCorrelationData>>(envelope, nameof(envelope));

    public static IMqttOutboundEnvelope AsMqttEnvelope(this IOutboundEnvelope envelope) =>
        Check.IsOfType<IMqttOutboundEnvelope>(envelope, nameof(envelope));

    public static IMqttOutboundEnvelope<TMessage> AsMqttEnvelope<TMessage>(this IOutboundEnvelope envelope) =>
        Check.IsOfType<IMqttOutboundEnvelope<TMessage>>(envelope, nameof(envelope));

    public static IMqttOutboundEnvelope<TMessage, TCorrelationData> AsMqttEnvelope<TMessage, TCorrelationData>(this IOutboundEnvelope envelope) =>
        Check.IsOfType<IMqttOutboundEnvelope<TMessage, TCorrelationData>>(envelope, nameof(envelope));

    /// <summary>
    ///     Gets response topic.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The response topic.
    /// </returns>
    public static string? GetMqttResponseTopic(this IInboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope().ResponseTopic;

    /// <summary>
    ///     Gets response topic.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The response topic.
    /// </returns>
    public static string? GetMqttResponseTopic(this IOutboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope().ResponseTopic;

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
    ///     The <see cref="IOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public static IOutboundEnvelope SetMqttResponseTopic(this IOutboundEnvelope envelope, string responseTopic) =>
        Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope().SetResponseTopic(responseTopic);

    // /// <summary>
    // ///     Gets the correlation data.
    // /// </summary>
    // /// <param name="envelope">
    // ///     The envelope containing the message.
    // /// </param>
    // /// <returns>
    // ///     The correlation data.
    // /// </returns>
    // public static byte[]? GetMqttRawCorrelationData(this IInboundEnvelope envelope) =>
    //     Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope().RawCorrelationData;
    //
    // /// <summary>
    // ///     Gets the correlation data.
    // /// </summary>
    // /// <param name="envelope">
    // ///     The envelope containing the message.
    // /// </param>
    // /// <returns>
    // ///     The correlation data.
    // /// </returns>
    // public static byte[]? GetMqttRawCorrelationData(this IOutboundEnvelope envelope) =>
    //     Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope().RawCorrelationData;

    /// <summary>
    ///     Gets the correlation data as UTF-8 string.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The correlation data.
    /// </returns>
    public static TCorrelationData? GetMqttCorrelationData<TCorrelationData>(this IInboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope<object, TCorrelationData>().CorrelationData;

    /// <summary>
    ///     Gets the correlation data as UTF-8 string.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The correlation data.
    /// </returns>
    public static TCorrelationData? GetMqttCorrelationData<TCorrelationData>(this IOutboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope<object, TCorrelationData>().CorrelationData;
    
    // TODO: Check summaries (Kafka version too)
    /// <summary>
    ///     Gets the correlation data as UTF-8 string.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The raw correlation data.
    /// </returns>
    public static byte[]? GetMqttRawCorrelationData(this IInboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope().RawCorrelationData;

    /// <summary>
    ///     Gets the correlation data as UTF-8 string.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The correlation data.
    /// </returns>
    public static byte[]? GetMqttRawCorrelationData(this IOutboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope().RawCorrelationData;

    /// <summary>
    ///     Sets the correlation data.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <param name="rawCorrelationData">
    ///     The raw correlation data.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public static IOutboundEnvelope SetMqttRawCorrelationData(this IOutboundEnvelope envelope, byte[]? rawCorrelationData) =>
        Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope().SetRawCorrelationData(rawCorrelationData);

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
    ///     The <see cref="IOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public static IOutboundEnvelope SetMqttCorrelationData<TCorrelationData>(this IOutboundEnvelope envelope, TCorrelationData correlationData) =>
        Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope<object, TCorrelationData>().SetCorrelationData(correlationData);

    /// <summary>
    ///     Gets the destination topic.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The destination topic.
    /// </returns>
    public static string? GetMqttDestinationTopic(this IOutboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope().DynamicDestinationTopic;

    /// <summary>
    ///     Sets the destination topic.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <param name="destinationTopic">
    ///     The destination topic.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public static IOutboundEnvelope SetMqttDestinationTopic(this IOutboundEnvelope envelope, string destinationTopic) =>
        Check.NotNull(envelope, nameof(envelope)).AsMqttEnvelope().SetDestinationTopic(destinationTopic);
}
