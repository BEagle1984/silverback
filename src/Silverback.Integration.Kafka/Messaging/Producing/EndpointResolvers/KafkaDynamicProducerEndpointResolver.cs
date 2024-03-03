// Copyright (c) 2023 Sergio Aquilini
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
public sealed record KafkaDynamicProducerEndpointResolver
    : DynamicProducerEndpointResolver<KafkaProducerEndpoint, KafkaProducerEndpointConfiguration>
{
    private readonly Func<object?, IServiceProvider, TopicPartition> _topicPartitionFunction;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver" /> class.
    /// </summary>
    /// <param name="topic">
    ///     The target topic.
    /// </param>
    /// <param name="partitionFunction">
    ///     The function returning the target partition index for the message being produced.
    /// </param>
    public KafkaDynamicProducerEndpointResolver(string topic, Func<object?, int> partitionFunction)
        : base(Check.NotNullOrEmpty(topic, nameof(topic)))
    {
        Check.NotNull(partitionFunction, nameof(partitionFunction));

        _topicPartitionFunction = (message, _) => new TopicPartition(topic, partitionFunction.Invoke(message));
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver" /> class.
    /// </summary>
    /// <param name="topicFunction">
    ///     The function returning the target topic for the message being produced.
    /// </param>
    public KafkaDynamicProducerEndpointResolver(Func<object?, string> topicFunction)
        : base($"dynamic-{Guid.NewGuid():N}")
    {
        Check.NotNull(topicFunction, nameof(topicFunction));

        _topicPartitionFunction = (message, _) =>
            new TopicPartition(
                topicFunction.Invoke(message),
                Partition.Any);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver" /> class.
    /// </summary>
    /// <param name="topicFunction">
    ///     The function returning the target topic for the message being produced.
    /// </param>
    /// <param name="partitionFunction">
    ///     The function returning the target partition index for the message being produced.
    /// </param>
    public KafkaDynamicProducerEndpointResolver(Func<object?, string> topicFunction, Func<object?, int> partitionFunction)
        : base($"dynamic-{Guid.NewGuid():N}")
    {
        Check.NotNull(topicFunction, nameof(topicFunction));
        Check.NotNull(partitionFunction, nameof(partitionFunction));

        _topicPartitionFunction = (message, _) =>
            new TopicPartition(
                topicFunction.Invoke(message),
                partitionFunction.Invoke(message));
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver" /> class.
    /// </summary>
    /// <param name="topicPartitionFunction">
    ///     The function returning the target topic and partition index for the message being produced.
    /// </param>
    public KafkaDynamicProducerEndpointResolver(Func<object?, TopicPartition> topicPartitionFunction)
        : base($"dynamic-{Guid.NewGuid():N}")
    {
        Check.NotNull(topicPartitionFunction, nameof(topicPartitionFunction));

        _topicPartitionFunction = (message, _) => topicPartitionFunction.Invoke(message);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaDynamicProducerEndpointResolver" /> class.
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
        Func<object?, string[]> topicArgumentsFunction,
        Func<object?, int>? partitionFunction = null)
        : base(Check.NotNullOrEmpty(topicFormatString, nameof(topicFormatString)))
    {
        Check.NotNullOrEmpty(topicFormatString, nameof(topicFormatString));
        Check.NotNull(topicArgumentsFunction, nameof(topicArgumentsFunction));

        string FormatTopic(object? message) =>
            string.Format(CultureInfo.InvariantCulture, topicFormatString, topicArgumentsFunction.Invoke(message));

        _topicPartitionFunction = partitionFunction == null
            ? (message, _) => new TopicPartition(FormatTopic(message), Partition.Any)
            : (message, _) => new TopicPartition(FormatTopic(message), partitionFunction.Invoke(message));
    }

    internal KafkaDynamicProducerEndpointResolver(
        Type resolverType,
        Func<object?, IServiceProvider, TopicPartition> topicPartitionFunction)
        : base($"dynamic-{Check.NotNull(resolverType, nameof(resolverType)).Name}-{Guid.NewGuid():N}")
    {
        Check.NotNull(resolverType, nameof(resolverType));
        Check.NotNull(topicPartitionFunction, nameof(topicPartitionFunction));

        _topicPartitionFunction = topicPartitionFunction;
    }

    /// <inheritdoc cref="DynamicProducerEndpointResolver{TEndpoint,TConfiguration}.Serialize(TEndpoint)" />
    public override string Serialize(KafkaProducerEndpoint endpoint)
    {
        Check.NotNull(endpoint, nameof(endpoint));

        return $"{endpoint.TopicPartition.Topic}|{endpoint.TopicPartition.Partition.Value}";
    }

    /// <inheritdoc cref="DynamicProducerEndpointResolver{TEndpoint,TConfiguration}.Deserialize(string,TConfiguration)" />
    public override KafkaProducerEndpoint Deserialize(
        string serializedEndpoint,
        KafkaProducerEndpointConfiguration configuration)
    {
        Check.NotNull(serializedEndpoint, nameof(serializedEndpoint));
        Check.NotNull(configuration, nameof(configuration));

        string[] parts = serializedEndpoint.Split('|');

        return new KafkaProducerEndpoint(
            new TopicPartition(parts[0], int.Parse(parts[1], CultureInfo.InvariantCulture)),
            configuration);
    }

    /// <inheritdoc cref="DynamicProducerEndpointResolver{TEndpoint,TConfiguration}.GetEndpointCore" />
    protected override KafkaProducerEndpoint GetEndpointCore(
        object? message,
        KafkaProducerEndpointConfiguration configuration,
        IServiceProvider serviceProvider) =>
        new(_topicPartitionFunction.Invoke(message, serviceProvider), configuration);
}
