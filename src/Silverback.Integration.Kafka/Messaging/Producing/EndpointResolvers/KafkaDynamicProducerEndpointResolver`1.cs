// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     Dynamically resolves the target topic and partition for each message being produced.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the message being produced.
/// </typeparam>
public sealed record KafkaDynamicProducerEndpointResolver<TMessage>
    : DynamicProducerEndpointResolver<TMessage, KafkaProducerEndpoint, KafkaProducerEndpointConfiguration>
    where TMessage : class
{
    private readonly Func<IOutboundEnvelope<TMessage>, TopicPartition> _topicPartitionFunction;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver{TMessage}" /> class.
    /// </summary>
    /// <param name="topic">
    ///     The target topic.
    /// </param>
    /// <param name="partitionFunction">
    ///     The function returning the target partition index for the message being produced.
    /// </param>
    public KafkaDynamicProducerEndpointResolver(string topic, Func<TMessage?, int> partitionFunction)
        : this(topic, envelope => partitionFunction.Invoke(envelope.Message))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver{TMessage}" /> class.
    /// </summary>
    /// <param name="topic">
    ///     The target topic.
    /// </param>
    /// <param name="partitionFunction">
    ///     The function returning the target partition index for the message being produced.
    /// </param>
    public KafkaDynamicProducerEndpointResolver(string topic, Func<IOutboundEnvelope<TMessage>, int> partitionFunction)
        : base(Check.NotNullOrEmpty(topic))
    {
        Check.NotNull(partitionFunction);

        _topicPartitionFunction = envelope => new TopicPartition(topic, partitionFunction.Invoke(envelope));
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver{TMessage}" /> class.
    /// </summary>
    /// <param name="topicFunction">
    ///     The function returning the target topic for the message being produced.
    /// </param>
    public KafkaDynamicProducerEndpointResolver(Func<TMessage?, string> topicFunction)
        : this(envelope => topicFunction.Invoke(envelope.Message))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver{TMessage}" /> class.
    /// </summary>
    /// <param name="topicFunction">
    ///     The function returning the target topic for the message being produced.
    /// </param>
    public KafkaDynamicProducerEndpointResolver(Func<IOutboundEnvelope<TMessage>, string> topicFunction)
        : base($"dynamic-{Guid.NewGuid():N}")
    {
        Check.NotNull(topicFunction);

        _topicPartitionFunction = envelope => new TopicPartition(topicFunction.Invoke(envelope), Partition.Any);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver{TMessage}" /> class.
    /// </summary>
    /// <param name="topicFunction">
    ///     The function returning the target topic for the message being produced.
    /// </param>
    /// <param name="partitionFunction">
    ///     The function returning the target partition index for the message being produced.
    /// </param>
    public KafkaDynamicProducerEndpointResolver(
        Func<TMessage?, string> topicFunction,
        Func<TMessage?, int> partitionFunction)
        : this(envelope => new TopicPartition(topicFunction.Invoke(envelope.Message), partitionFunction.Invoke(envelope.Message)))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver{TMessage}" /> class.
    /// </summary>
    /// <param name="topicFunction">
    ///     The function returning the target topic for the message being produced.
    /// </param>
    /// <param name="partitionFunction">
    ///     The function returning the target partition index for the message being produced.
    /// </param>
    public KafkaDynamicProducerEndpointResolver(
        Func<IOutboundEnvelope<TMessage>, string> topicFunction,
        Func<IOutboundEnvelope<TMessage>, int> partitionFunction)
        : base($"dynamic-{Guid.NewGuid():N}")
    {
        Check.NotNull(topicFunction);
        Check.NotNull(partitionFunction);

        _topicPartitionFunction = envelope => new TopicPartition(topicFunction.Invoke(envelope), partitionFunction.Invoke(envelope));
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver{TMessage}" /> class.
    /// </summary>
    /// <param name="topicPartitionFunction">
    ///     The function returning the target topic and partition index for the message being produced.
    /// </param>
    public KafkaDynamicProducerEndpointResolver(Func<TMessage?, TopicPartition> topicPartitionFunction)
        : this(envelope => topicPartitionFunction.Invoke(envelope.Message))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver{TMessage}" /> class.
    /// </summary>
    /// <param name="topicPartitionFunction">
    ///     The function returning the target topic and partition index for the message being produced.
    /// </param>
    public KafkaDynamicProducerEndpointResolver(Func<IOutboundEnvelope<TMessage>, TopicPartition> topicPartitionFunction)
        : base($"dynamic-{Guid.NewGuid():N}")
    {
        Check.NotNull(topicPartitionFunction);

        _topicPartitionFunction = topicPartitionFunction;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver{TMessage}" /> class.
    /// </summary>
    /// <param name="topicFormatString">
    ///     The topic format string that will be combined with the arguments returned by the <paramref name="topicArgumentsFunction" />
    ///     using a <see cref="string.Format(string,object[])" />.
    /// </param>
    /// <param name="topicArgumentsFunction">
    ///     The function returning the arguments to be used to format the string.
    /// </param>
    /// <param name="partitionFunction">
    ///     The optional function returning the target partition index for the message being produced. If <c>null</c> the partition is
    ///     automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" /> to specify a message key, otherwise a
    ///     random one will be generated).
    /// </param>
    [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Not an issue, the array is not modified")]
    public KafkaDynamicProducerEndpointResolver(
        string topicFormatString,
        Func<TMessage?, string[]> topicArgumentsFunction,
        Func<TMessage?, int>? partitionFunction = null)
        : this(
            topicFormatString,
            envelope => topicArgumentsFunction.Invoke(envelope.Message),
            partitionFunction == null ? null : envelope => partitionFunction.Invoke(envelope.Message))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver{TMessage}" /> class.
    /// </summary>
    /// <param name="topicFormatString">
    ///     The topic format string that will be combined with the arguments returned by the <paramref name="topicArgumentsFunction" />
    ///     using a <see cref="string.Format(string,object[])" />.
    /// </param>
    /// <param name="topicArgumentsFunction">
    ///     The function returning the arguments to be used to format the string.
    /// </param>
    /// <param name="partitionFunction">
    ///     The optional function returning the target partition index for the message being produced. If <c>null</c> the partition is
    ///     automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" /> to specify a message key, otherwise a
    ///     random one will be generated).
    /// </param>
    [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Not an issue, the array is not modified")]
    public KafkaDynamicProducerEndpointResolver(
        string topicFormatString,
        Func<IOutboundEnvelope<TMessage>, string[]> topicArgumentsFunction,
        Func<IOutboundEnvelope<TMessage>, int>? partitionFunction = null)
        : base(Check.NotNullOrEmpty(topicFormatString))
    {
        Check.NotNullOrEmpty(topicFormatString);
        Check.NotNull(topicArgumentsFunction);

        string FormatTopic(IOutboundEnvelope<TMessage> envelope) =>
            string.Format(CultureInfo.InvariantCulture, topicFormatString, topicArgumentsFunction.Invoke(envelope));

        _topicPartitionFunction = partitionFunction == null
            ? envelope => new TopicPartition(FormatTopic(envelope), Partition.Any)
            : envelope => new TopicPartition(FormatTopic(envelope), partitionFunction.Invoke(envelope));
    }

    internal KafkaDynamicProducerEndpointResolver(
        Type resolverType,
        Func<IOutboundEnvelope<TMessage>, TopicPartition> topicPartitionFunction)
        : base($"dynamic-{Check.NotNull(resolverType).Name}-{Guid.NewGuid():N}")
    {
        Check.NotNull(resolverType);
        Check.NotNull(topicPartitionFunction);

        _topicPartitionFunction = topicPartitionFunction;
    }

    /// <inheritdoc cref="DynamicProducerEndpointResolver{TMessage,TEndpoint,TConfiguration}.SerializeEndpoint" />
    protected override string SerializeEndpoint(KafkaProducerEndpoint endpoint)
    {
        Check.NotNull(endpoint);

        return $"{endpoint.TopicPartition.Topic}|{endpoint.TopicPartition.Partition.Value}";
    }

    /// <inheritdoc cref="DynamicProducerEndpointResolver{TMessage,TEndpoint,TConfiguration}.DeserializeEndpoint" />
    protected override KafkaProducerEndpoint DeserializeEndpoint(
        string serializedEndpoint,
        KafkaProducerEndpointConfiguration configuration)
    {
        Check.NotNull(serializedEndpoint);

        int separatorIndex = serializedEndpoint.IndexOf('|', StringComparison.Ordinal);

        if (separatorIndex < 0)
            throw new FormatException($"The serialized endpoint '{serializedEndpoint}' is not in the expected 'topic|partition' format.");

        // Anything after a further separator is ignored
        ReadOnlySpan<char> partition = serializedEndpoint.AsSpan(separatorIndex + 1);
        int nextSeparatorIndex = partition.IndexOf('|');

        if (nextSeparatorIndex >= 0)
            partition = partition[..nextSeparatorIndex];

        return new KafkaProducerEndpoint(
            new TopicPartition(serializedEndpoint[..separatorIndex], int.Parse(partition, CultureInfo.InvariantCulture)),
            configuration);
    }

    /// <inheritdoc cref="DynamicProducerEndpointResolver{TMessage,TEndpoint,TConfiguration}.GetEndpointCore" />
    protected override KafkaProducerEndpoint GetEndpointCore(
        IOutboundEnvelope<TMessage> envelope,
        KafkaProducerEndpointConfiguration configuration) =>
        new(_topicPartitionFunction.Invoke(envelope), configuration);
}
