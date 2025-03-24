// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaProducerEndpointConfiguration" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being produced.
/// </typeparam>
public sealed class KafkaProducerEndpointConfigurationBuilder<TMessage>
    : ProducerEndpointConfigurationBuilder<TMessage, KafkaProducerEndpointConfiguration, KafkaProducerEndpoint, KafkaProducerEndpointConfigurationBuilder<TMessage>>
    where TMessage : class
{
    private IProducerEndpointResolver<KafkaProducerEndpoint>? _endpointResolver;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="friendlyName">
    ///     An optional friendly to be shown in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </param>
    public KafkaProducerEndpointConfigurationBuilder(IServiceProvider serviceProvider, string? friendlyName = null)
        : base(serviceProvider, friendlyName)
    {
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.This" />
    protected override KafkaProducerEndpointConfigurationBuilder<TMessage> This => this;

    /// <summary>
    ///     Specifies the target topic and optionally the target partition.
    /// </summary>
    /// <param name="topic">
    ///     The target topic.
    /// </param>
    /// <param name="partition">
    ///     The optional target partition index. If <c>null</c> the partition is automatically derived from the message key (use
    ///     <see cref="KafkaKeyMemberAttribute" /> to specify a message key, otherwise a random one will be generated).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> ProduceTo(string topic, int? partition = null)
    {
        Check.NotNullOrEmpty(topic, nameof(topic));

        _endpointResolver = new KafkaStaticProducerEndpointResolver(topic, partition);

        return this;
    }

    /// <summary>
    ///     Specifies the target topic and partition.
    /// </summary>
    /// <param name="topicPartition">
    ///     The target topic and partition.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> ProduceTo(TopicPartition topicPartition)
    {
        Check.NotNull(topicPartition, nameof(topicPartition));

        _endpointResolver = new KafkaStaticProducerEndpointResolver(topicPartition);

        return this;
    }

    /// <summary>
    ///     Specifies the target topic and the function returning the target partition for each message being produced.
    /// </summary>
    /// <param name="topic">
    ///     The target topic.
    /// </param>
    /// <param name="partitionFunction">
    ///     The function returning the target partition index for the message being produced.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> ProduceTo(string topic, Func<TMessage?, int> partitionFunction)
    {
        Check.NotNull(topic, nameof(topic));
        Check.NotNull(partitionFunction, nameof(partitionFunction));

        _endpointResolver = new KafkaDynamicProducerEndpointResolver<TMessage>(topic, partitionFunction);

        return this;
    }

    /// <summary>
    ///     Specifies the target topic and the function returning the target partition for each message being produced.
    /// </summary>
    /// <param name="topic">
    ///     The target topic.
    /// </param>
    /// <param name="partitionFunction">
    ///     The function returning the target partition index for the message being produced.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> ProduceTo(string topic, Func<IOutboundEnvelope<TMessage>, int> partitionFunction)
    {
        Check.NotNull(topic, nameof(topic));
        Check.NotNull(partitionFunction, nameof(partitionFunction));

        _endpointResolver = new KafkaDynamicProducerEndpointResolver<TMessage>(topic, partitionFunction);

        return this;
    }

    /// <summary>
    ///     Specifies the function returning the target topic and partition for each message being produced.
    /// </summary>
    /// <param name="topicPartitionFunction">
    ///     The function returning the target topic and partition index for the message being produced.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> ProduceTo(Func<TMessage?, TopicPartition> topicPartitionFunction)
    {
        Check.NotNull(topicPartitionFunction, nameof(topicPartitionFunction));

        _endpointResolver = new KafkaDynamicProducerEndpointResolver<TMessage>(topicPartitionFunction);

        return this;
    }

    /// <summary>
    ///     Specifies the function returning the target topic and partition for each message being produced.
    /// </summary>
    /// <param name="topicPartitionFunction">
    ///     The function returning the target topic and partition index for the message being produced.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> ProduceTo(Func<IOutboundEnvelope<TMessage>, TopicPartition> topicPartitionFunction)
    {
        Check.NotNull(topicPartitionFunction, nameof(topicPartitionFunction));

        _endpointResolver = new KafkaDynamicProducerEndpointResolver<TMessage>(topicPartitionFunction);

        return this;
    }

    /// <summary>
    ///     Specifies the functions returning the target topic and partition for each message being produced.
    /// </summary>
    /// <param name="topicFunction">
    ///     The function returning the target topic for the message being produced.
    /// </param>
    /// <param name="partitionFunction">
    ///     The optional function returning the target partition index for the message being produced. If <c>null</c> the partition is
    ///     automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" /> to specify a message key, otherwise a
    ///     random one will be generated).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> ProduceTo(
        Func<TMessage?, string> topicFunction,
        Func<TMessage?, int>? partitionFunction = null)
    {
        Check.NotNull(topicFunction, nameof(topicFunction));

        _endpointResolver = partitionFunction == null
            ? new KafkaDynamicProducerEndpointResolver<TMessage>(topicFunction)
            : new KafkaDynamicProducerEndpointResolver<TMessage>(topicFunction, partitionFunction);

        return this;
    }

    /// <summary>
    ///     Specifies the functions returning the target topic and partition for each message being produced.
    /// </summary>
    /// <param name="topicFunction">
    ///     The function returning the target topic for the message being produced.
    /// </param>
    /// <param name="partitionFunction">
    ///     The optional function returning the target partition index for the message being produced. If <c>null</c> the partition is
    ///     automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" /> to specify a message key, otherwise a
    ///     random one will be generated).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> ProduceTo(
        Func<IOutboundEnvelope<TMessage>, string> topicFunction,
        Func<IOutboundEnvelope<TMessage>, int>? partitionFunction = null)
    {
        Check.NotNull(topicFunction, nameof(topicFunction));

        _endpointResolver = partitionFunction == null
            ? new KafkaDynamicProducerEndpointResolver<TMessage>(topicFunction)
            : new KafkaDynamicProducerEndpointResolver<TMessage>(topicFunction, partitionFunction);

        return this;
    }

    /// <summary>
    ///     Specifies the target topic format and an optional function returning the target partition for each message being produced.
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
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> ProduceTo(
        string topicFormatString,
        Func<TMessage?, string[]> topicArgumentsFunction,
        Func<TMessage?, int>? partitionFunction = null)
    {
        Check.NotNullOrEmpty(topicFormatString, nameof(topicFormatString));
        Check.NotNull(topicArgumentsFunction, nameof(topicArgumentsFunction));

        _endpointResolver = new KafkaDynamicProducerEndpointResolver<TMessage>(topicFormatString, topicArgumentsFunction, partitionFunction);

        return this;
    }

    /// <summary>
    ///     Specifies the target topic format and an optional function returning the target partition for each message being produced.
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
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> ProduceTo(
        string topicFormatString,
        Func<IOutboundEnvelope<TMessage>, string[]> topicArgumentsFunction,
        Func<IOutboundEnvelope<TMessage>, int>? partitionFunction = null)
    {
        Check.NotNullOrEmpty(topicFormatString, nameof(topicFormatString));
        Check.NotNull(topicArgumentsFunction, nameof(topicArgumentsFunction));

        _endpointResolver = new KafkaDynamicProducerEndpointResolver<TMessage>(topicFormatString, topicArgumentsFunction, partitionFunction);

        return this;
    }

    /// <summary>
    ///     Specifies that the target topic and, optionally, the target partition will be specified per each message using the envelope's
    ///     <see cref="KafkaEnvelopeExtensions.SetKafkaDestinationTopic" /> extension method.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> ProduceToDynamicTopic()
    {
        _endpointResolver = new KafkaDynamicProducerEndpointResolver<TMessage>(
            envelope =>
            {
                string? destinationTopic = envelope.GetKafkaDestinationTopic();

                if (string.IsNullOrEmpty(destinationTopic))
                    throw new InvalidOperationException("The destination topic is not set.");

                return destinationTopic;
            },
            envelope => envelope.GetKafkaDestinationPartition() ?? Partition.Any);

        return this;
    }

    /// <summary>
    ///     Specifies the type of the <see cref="IKafkaProducerEndpointResolver{TMessage}" /> to be used to resolve the target topic and
    ///     partition for each message being produced.
    /// </summary>
    /// <typeparam name="TResolver">
    ///     The type of the <see cref="IKafkaProducerEndpointResolver{TMessage}" /> to be used.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> UseEndpointResolver<TResolver>()
        where TResolver : IKafkaProducerEndpointResolver<TMessage>
    {
        _endpointResolver = new KafkaDynamicProducerEndpointResolver<TMessage>(
            typeof(TResolver),
            envelope => envelope.Context?.ServiceProvider == null
                ? throw new InvalidOperationException("The service provider is not available. The endpoint resolver requires a service provider to be resolved.")
                : envelope.Context.ServiceProvider.GetRequiredService<TResolver>().GetTopicPartition(envelope));

        return this;
    }

    /// <summary>
    ///     Uses the specified value provider function to set the kafka key for each message being produced.
    /// </summary>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> SetKafkaKey(Func<TMessage?, object?> valueProvider) =>
        SetMessageId(valueProvider);

    /// <summary>
    ///     Uses the specified value provider function to set the kafka key for each message being produced.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this kafka key.
    /// </typeparam>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> SetKafkaKey<TMessageChildType>(Func<TMessageChildType?, object?> valueProvider)
        where TMessageChildType : TMessage => SetMessageId(valueProvider);

    /// <summary>
    ///     Uses the specified value provider function to set the kafka key for each message being produced.
    /// </summary>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> SetKafkaKey(Func<IOutboundEnvelope<TMessage>, object?> valueProvider) =>
        SetMessageId(valueProvider);

    /// <summary>
    ///     Uses the specified value provider function to set the kafka key for each message being produced.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this kafka key.
    /// </typeparam>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerEndpointConfigurationBuilder<TMessage> SetKafkaKey<TMessageChildType>(Func<IOutboundEnvelope<TMessageChildType>, object?> valueProvider)
        where TMessageChildType : TMessage => SetMessageId(valueProvider);

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.CreateConfiguration" />
    protected override KafkaProducerEndpointConfiguration CreateConfiguration() =>
        new()
        {
            EndpointResolver = _endpointResolver ?? NullProducerEndpointResolver<KafkaProducerEndpoint>.Instance
        };
}
