// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Protocol;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Builds the <see cref="MqttProducerEndpointConfiguration" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being produced.
/// </typeparam>
public partial class MqttProducerEndpointConfigurationBuilder<TMessage>
    : ProducerEndpointConfigurationBuilder<TMessage, MqttProducerEndpointConfiguration, MqttProducerEndpoint, MqttProducerEndpointConfigurationBuilder<TMessage>>
    where TMessage : class
{
    private IProducerEndpointResolver<MqttProducerEndpoint>? _endpointResolver;

    private MqttQualityOfServiceLevel? _qualityOfServiceLevel;

    private bool? _retain;

    private uint? _messageExpiryInterval;

    private NoMatchingSubscribersBehavior? _noMatchingSubscribersBehavior;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="friendlyName">
    ///     An optional friendly to be shown in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </param>
    public MqttProducerEndpointConfigurationBuilder(IServiceProvider serviceProvider, string? friendlyName = null)
        : base(serviceProvider, friendlyName)
    {
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.This" />
    protected override MqttProducerEndpointConfigurationBuilder<TMessage> This => this;

    /// <summary>
    ///     Specifies the target topic and optionally the target partition.
    /// </summary>
    /// <param name="topic">
    ///     The target topic.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> ProduceTo(string topic)
    {
        Check.NotNullOrEmpty(topic, nameof(topic));

        _endpointResolver = new MqttStaticProducerEndpointResolver(topic);

        return this;
    }

    /// <summary>
    ///     Specifies the functions returning the target topic for each message being produced.
    /// </summary>
    /// <param name="topicFunction">
    ///     The function returning the target topic for the message being produced.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> ProduceTo(Func<TMessage?, string> topicFunction)
    {
        Check.NotNull(topicFunction, nameof(topicFunction));

        _endpointResolver = new MqttDynamicProducerEndpointResolver<TMessage>(topicFunction);

        return this;
    }

    /// <summary>
    ///     Specifies the target topic format.
    /// </summary>
    /// <param name="topicFormatString">
    ///     The topic format string that will be combined with the arguments returned by the <paramref name="topicArgumentsFunction" />
    ///     using a <see cref="string.Format(string,object[])" />.
    /// </param>
    /// <param name="topicArgumentsFunction">
    ///     The function returning the arguments to be used to format the string.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> ProduceTo(
        string topicFormatString,
        Func<TMessage?, string[]> topicArgumentsFunction)
    {
        Check.NotNullOrEmpty(topicFormatString, nameof(topicFormatString));
        Check.NotNull(topicArgumentsFunction, nameof(topicArgumentsFunction));

        _endpointResolver = new MqttDynamicProducerEndpointResolver<TMessage>(topicFormatString, topicArgumentsFunction);

        return this;
    }

    /// <summary>
    ///     Specifies that the target topic and, optionally, the target partition will be specified per each message using the envelope's
    ///     <see cref="MqttEnvelopeExtensions.SetMqttDestinationTopic" /> extension method.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> ProduceToDynamicTopic()
    {
        _endpointResolver = new MqttDynamicProducerEndpointResolver<TMessage>(envelope =>
        {
            string? destinationTopic = envelope.GetMqttDestinationTopic();

            if (string.IsNullOrEmpty(destinationTopic))
                throw new InvalidOperationException("The destination topic is not set.");

            return destinationTopic;
        });

        return this;
    }

    /// <summary>
    ///     Specifies the type of the <see cref="IMqttProducerEndpointResolver{TMessage}" /> to be used to resolve the target topic for
    ///     each message being produced.
    /// </summary>
    /// <typeparam name="TResolver">
    ///     The type of the <see cref="IMqttProducerEndpointResolver{TMessage}" /> to be used.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> UseEndpointResolver<TResolver>()
        where TResolver : IMqttProducerEndpointResolver<TMessage>
    {
        _endpointResolver = new MqttDynamicProducerEndpointResolver<TMessage>(
            typeof(TResolver),
            envelope => envelope.Context?.ServiceProvider == null
                ? throw new InvalidOperationException("The service provider is not available. The endpoint resolver requires a service provider to be resolved.")
                : envelope.Context.ServiceProvider.GetRequiredService<TResolver>().GetTopic(envelope));

        return this;
    }

    /// <summary>
    ///     Specifies that the messages have to be sent with the retain flag, causing them to be persisted on the  broker.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> Retain()
    {
        _retain = true;
        return this;
    }

    /// <summary>
    ///     Sets the message expiry interval. This interval defines the period of time that the broker stores
    ///     the <i>PUBLISH</i> message for any matching subscribers that are not currently connected. When no
    ///     message expiry interval is set, the broker must store the message for matching subscribers indefinitely.
    /// </summary>
    /// <param name="messageExpiryInterval">
    ///     The <see cref="TimeSpan" /> representing the message expiry interval.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> WithMessageExpiration(TimeSpan messageExpiryInterval)
    {
        Check.Range(
            messageExpiryInterval,
            nameof(messageExpiryInterval),
            TimeSpan.Zero,
            TimeSpan.FromSeconds(uint.MaxValue));

        _messageExpiryInterval = (uint)messageExpiryInterval.TotalSeconds;
        return this;
    }

    /// <summary>
    ///     Specifies that no exception should be thrown when no matching subscribers are found for the produced message.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> IgnoreNoMatchingSubscribers()
    {
        _noMatchingSubscribersBehavior = NoMatchingSubscribersBehavior.Ignore;
        return this;
    }

    /// <summary>
    ///     Specifies that a warning should be logged when no matching subscribers are found for the produced message.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> LogNoMatchingSubscribersWarning()
    {
        _noMatchingSubscribersBehavior = NoMatchingSubscribersBehavior.LogWarning;
        return this;
    }

    /// <summary>
    ///     Specifies that an exception should be thrown when no matching subscribers are found for the produced message.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> ThrowNoMatchingSubscribersError()
    {
        _noMatchingSubscribersBehavior = NoMatchingSubscribersBehavior.Throw;
        return this;
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.CreateConfiguration" />
    protected override MqttProducerEndpointConfiguration CreateConfiguration()
    {
        MqttProducerEndpointConfiguration configuration = new();

        return configuration with
        {
            EndpointResolver = _endpointResolver ?? NullProducerEndpointResolver<MqttProducerEndpoint>.Instance,
            QualityOfServiceLevel = _qualityOfServiceLevel ?? configuration.QualityOfServiceLevel,
            Retain = _retain ?? configuration.Retain,
            MessageExpiryInterval = _messageExpiryInterval ?? configuration.MessageExpiryInterval,
            NoMatchingSubscribersBehavior = _noMatchingSubscribersBehavior ?? configuration.NoMatchingSubscribersBehavior
        };
    }
}
