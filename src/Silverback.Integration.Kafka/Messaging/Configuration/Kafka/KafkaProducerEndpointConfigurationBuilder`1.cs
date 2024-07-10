// Copyright (c) 2024 Sergio Aquilini
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
{
    private IProducerEndpointResolver<KafkaProducerEndpoint>? _endpointResolver;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" /> class.
    /// </summary>
    /// <param name="friendlyName">
    ///     An optional friendly to be shown in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </param>
    public KafkaProducerEndpointConfigurationBuilder(string? friendlyName = null)
        : base(friendlyName)
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

        _endpointResolver = new KafkaDynamicProducerEndpointResolver(
            topic,
            message => partitionFunction.Invoke((TMessage?)message));

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
            ? new KafkaDynamicProducerEndpointResolver(message => topicFunction.Invoke((TMessage?)message))
            : (IProducerEndpointResolver<KafkaProducerEndpoint>)new KafkaDynamicProducerEndpointResolver(
                message => topicFunction.Invoke((TMessage?)message),
                message => partitionFunction.Invoke((TMessage?)message));

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

        _endpointResolver = new KafkaDynamicProducerEndpointResolver(message => topicPartitionFunction.Invoke((TMessage?)message));

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

        _endpointResolver = new KafkaDynamicProducerEndpointResolver(
            topicFormatString,
            message => topicArgumentsFunction.Invoke((TMessage?)message),
            partitionFunction == null ? null : message => partitionFunction.Invoke((TMessage?)message));

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
        _endpointResolver = new KafkaDynamicProducerEndpointResolver(
            typeof(TResolver),
            (message, serviceProvider) =>
                serviceProvider.GetRequiredService<TResolver>().GetTopicPartition((TMessage?)message));

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
            Endpoint = _endpointResolver ?? NullProducerEndpointResolver<KafkaProducerEndpoint>.Instance
        };
}
