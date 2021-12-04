// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Collections;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaProducerConfiguration" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being produced.
/// </typeparam>
public sealed class KafkaProducerConfigurationBuilder<TMessage>
    : ProducerConfigurationBuilder<TMessage, KafkaProducerConfiguration, KafkaProducerEndpoint, KafkaProducerConfigurationBuilder<TMessage>>
{
    private readonly KafkaClientConfiguration? _clientConfiguration;

    private readonly List<Action<KafkaClientProducerConfiguration>> _clientConfigurationActions = new();

    private IProducerEndpointResolver<KafkaProducerEndpoint>? _endpointResolver;

    private IOutboundMessageEnricher? _kafkaKeyEnricher;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> class.
    /// </summary>
    /// <param name="clientConfiguration">
    ///     The <see cref="KafkaClientConfiguration" /> to be used to initialize the <see cref="KafkaClientProducerConfiguration" />.
    /// </param>
    /// <param name="endpointsConfigurationBuilder">
    ///     The optional <see cref="EndpointsConfigurationBuilder" /> that instantiated the builder.
    /// </param>
    public KafkaProducerConfigurationBuilder(
        KafkaClientConfiguration? clientConfiguration = null,
        KafkaEndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
        : base(endpointsConfigurationBuilder)
    {
        _clientConfiguration = clientConfiguration;
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.EndpointRawName" />
    // TODO: Test
    public override string? EndpointRawName => _endpointResolver?.RawName;

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TEndpoint,TBuilder}.This" />
    protected override KafkaProducerConfigurationBuilder<TMessage> This => this;

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
    ///     The <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder<TMessage> ProduceTo(string topic, int? partition = null)
    {
        Check.NotEmpty(topic, nameof(topic));
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
    ///     The <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder<TMessage> ProduceTo(TopicPartition topicPartition)
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
    ///     The <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder<TMessage> ProduceTo(string topic, Func<TMessage?, int> partitionFunction)
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
    ///     The <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder<TMessage> ProduceTo(
        Func<TMessage?, string> topicFunction,
        Func<TMessage?, int>? partitionFunction = null)
    {
        Check.NotNull(topicFunction, nameof(topicFunction));

        if (partitionFunction == null)
        {
            _endpointResolver = new KafkaDynamicProducerEndpointResolver(message => topicFunction.Invoke((TMessage?)message));
        }
        else
        {
            _endpointResolver = new KafkaDynamicProducerEndpointResolver(
                message => topicFunction.Invoke((TMessage?)message),
                message => partitionFunction.Invoke((TMessage?)message));
        }

        return this;
    }

    /// <summary>
    ///     Specifies the function returning the target topic and partition for each message being produced.
    /// </summary>
    /// <param name="topicPartitionFunction">
    ///     The function returning the target topic and partition index for the message being produced.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder<TMessage> ProduceTo(Func<TMessage?, TopicPartition> topicPartitionFunction)
    {
        Check.NotNull(topicPartitionFunction, nameof(topicPartitionFunction));
        _endpointResolver = new KafkaDynamicProducerEndpointResolver(
            message =>
                topicPartitionFunction.Invoke((TMessage?)message));
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
    ///     The <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder<TMessage> ProduceTo(
        string topicFormatString,
        Func<TMessage?, string[]> topicArgumentsFunction,
        Func<TMessage?, int>? partitionFunction = null)
    {
        Check.NotEmpty(topicFormatString, nameof(topicFormatString));
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
    ///     The <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder<TMessage> UseEndpointResolver<TResolver>()
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
    ///     The <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder<TMessage> WithKafkaKey(Func<TMessage?, object?> valueProvider)
    {
        Check.NotNull(valueProvider, nameof(valueProvider));
        _kafkaKeyEnricher = new OutboundMessageKafkaKeyEnricher<TMessage>(valueProvider);
        return this;
    }

    /// <summary>
    ///     Uses the specified value provider function to set the kafka key for each message being produced.
    /// </summary>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder<TMessage> WithKafkaKey(Func<IOutboundEnvelope<TMessage>, object?> valueProvider)
    {
        Check.NotNull(valueProvider, nameof(valueProvider));
        _kafkaKeyEnricher = new OutboundMessageKafkaKeyEnricher<TMessage>(valueProvider);
        return this;
    }

    /// <summary>
    ///     Configures the Kafka client settings.
    /// </summary>
    /// <param name="clientConfigurationAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaClientProducerConfiguration" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder<TMessage> ConfigureClient(Action<KafkaClientProducerConfiguration> clientConfigurationAction)
    {
        Check.NotNull(clientConfigurationAction, nameof(clientConfigurationAction));
        _clientConfigurationActions.Add(clientConfigurationAction);
        return this;
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TEndpoint,TBuilder}.CreateConfiguration" />
    protected override KafkaProducerConfiguration CreateConfiguration()
    {
        if (_endpointResolver == null)
            throw new EndpointConfigurationException("Endpoint not set. Use ProduceTo or UseEndpointResolver to set it.");

        KafkaProducerConfiguration configuration = new()
        {
            Endpoint = _endpointResolver,
            Client = GetClientConfiguration(),
            MessageEnrichers = _kafkaKeyEnricher == null
                ? ValueReadOnlyCollection<IOutboundMessageEnricher>.Empty
                : new ValueReadOnlyCollection<IOutboundMessageEnricher>(
                    new[]
                    {
                        _kafkaKeyEnricher
                    })
        };

        _clientConfigurationActions.ForEach(action => action.Invoke(configuration.Client));

        return configuration;
    }

    private KafkaClientProducerConfiguration GetClientConfiguration()
    {
        KafkaClientProducerConfiguration config = new(_clientConfiguration);
        _clientConfigurationActions.ForEach(action => action.Invoke(config));
        return config.AsReadOnly();
    }
}
