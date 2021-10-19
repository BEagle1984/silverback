// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Protocol;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Builds the <see cref="MqttProducerConfiguration" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being produced.
/// </typeparam>
public class MqttProducerConfigurationBuilder<TMessage>
    : ProducerConfigurationBuilder<TMessage, MqttProducerConfiguration, MqttProducerEndpoint, MqttProducerConfigurationBuilder<TMessage>>
{
    private MqttClientConfiguration _clientConfiguration;

    private IProducerEndpointResolver<MqttProducerEndpoint>? _endpointResolver;

    private MqttQualityOfServiceLevel? _qualityOfServiceLevel;

    private bool? _retain;

    private uint? _messageExpiryInterval;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttProducerConfigurationBuilder{TMessage}" /> class.
    /// </summary>
    /// <param name="clientConfig">
    ///     The <see cref="MqttClientConfiguration" />.
    /// </param>
    /// <param name="endpointsConfigurationBuilder">
    ///     The optional <see cref="EndpointsConfigurationBuilder" /> that instantiated the builder.
    /// </param>
    public MqttProducerConfigurationBuilder(
        MqttClientConfiguration? clientConfig = null,
        EndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
        : base(endpointsConfigurationBuilder)
    {
        _clientConfiguration = clientConfig ?? new MqttClientConfiguration();
    }

    // TODO: Test
    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.EndpointRawName" />
    public override string? EndpointRawName => _endpointResolver?.RawName;

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TEndpoint,TBuilder}.This" />
    protected override MqttProducerConfigurationBuilder<TMessage> This => this;

    /// <summary>
    ///     Specifies the target topic and optionally the target partition.
    /// </summary>
    /// <param name="topic">
    ///     The target topic.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerConfigurationBuilder<TMessage> ProduceTo(string topic)
    {
        Check.NotEmpty(topic, nameof(topic));
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
    ///     The <see cref="MqttProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerConfigurationBuilder<TMessage> ProduceTo(Func<TMessage?, string> topicFunction)
    {
        Check.NotNull(topicFunction, nameof(topicFunction));
        _endpointResolver = new MqttDynamicProducerEndpointResolver(message => topicFunction.Invoke((TMessage?)message));
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
    ///     The <see cref="MqttProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerConfigurationBuilder<TMessage> ProduceTo(
        string topicFormatString,
        Func<TMessage?, string[]> topicArgumentsFunction)
    {
        Check.NotEmpty(topicFormatString, nameof(topicFormatString));
        Check.NotNull(topicArgumentsFunction, nameof(topicArgumentsFunction));

        _endpointResolver = new MqttDynamicProducerEndpointResolver(
            topicFormatString,
            message => topicArgumentsFunction.Invoke((TMessage?)message));

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
    ///     The <see cref="MqttProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerConfigurationBuilder<TMessage> UseEndpointResolver<TResolver>()
        where TResolver : IMqttProducerEndpointResolver<TMessage>
    {
        _endpointResolver = new MqttDynamicProducerEndpointResolver(
            typeof(TResolver),
            (message, serviceProvider) =>
                serviceProvider.GetRequiredService<TResolver>().GetTopic((TMessage?)message));

        return this;
    }

    /// <summary>
    ///     Configures the MQTT client settings.
    /// </summary>
    /// <param name="clientConfigurationAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientConfiguration" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerConfigurationBuilder<TMessage> ConfigureClient(Action<MqttClientConfiguration> clientConfigurationAction)
    {
        Check.NotNull(clientConfigurationAction, nameof(clientConfigurationAction));

        clientConfigurationAction.Invoke(_clientConfiguration);

        return this;
    }

    /// <summary>
    ///     Configures the MQTT client settings.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerConfigurationBuilder<TMessage> ConfigureClient(Action<MqttClientConfigurationBuilder> configurationBuilderAction)
    {
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        MqttClientConfigurationBuilder configBuilder = new(_clientConfiguration, EndpointsConfigurationBuilder?.ServiceProvider);
        configurationBuilderAction.Invoke(configBuilder);
        _clientConfiguration = configBuilder.Build();

        return this;
    }

    /// <summary>
    ///     Specifies the desired quality of service level.
    /// </summary>
    /// <param name="qosLevel">
    ///     The <see cref="MqttQualityOfServiceLevel" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerConfigurationBuilder<TMessage> WithQualityOfServiceLevel(MqttQualityOfServiceLevel qosLevel)
    {
        _qualityOfServiceLevel = qosLevel;
        return this;
    }

    /// <summary>
    ///     Specifies that the messages have to be sent with the <i>at most once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerConfigurationBuilder<TMessage> WithAtMostOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);

    /// <summary>
    ///     Specifies that the messages have to be sent with the <i>at least once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerConfigurationBuilder<TMessage> WithAtLeastOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

    /// <summary>
    ///     Specifies that the messages have to be sent with the <i>exactly once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerConfigurationBuilder<TMessage> WithExactlyOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);

    /// <summary>
    ///     Specifies that the messages have to be sent with the retain flag, causing them to be persisted on the  broker.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerConfigurationBuilder<TMessage> Retain()
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
    ///     The <see cref="MqttProducerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerConfigurationBuilder<TMessage> WithMessageExpiration(TimeSpan messageExpiryInterval)
    {
        Check.Range(
            messageExpiryInterval,
            nameof(messageExpiryInterval),
            TimeSpan.Zero,
            TimeSpan.FromSeconds(uint.MaxValue));

        _messageExpiryInterval = (uint)messageExpiryInterval.TotalSeconds;
        return this;
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TEndpoint,TBuilder}.CreateConfiguration" />
    protected override MqttProducerConfiguration CreateConfiguration()
    {
        if (_endpointResolver == null)
            throw new EndpointConfigurationException("Endpoint not set. Use ProduceTo or UseEndpointResolver to set it.");

        MqttProducerConfiguration configuration = new();

        return configuration with
        {
            Endpoint = _endpointResolver,
            Client = _clientConfiguration,
            QualityOfServiceLevel = _qualityOfServiceLevel ?? configuration.QualityOfServiceLevel,
            Retain = _retain ?? configuration.Retain,
            MessageExpiryInterval = _messageExpiryInterval ?? configuration.MessageExpiryInterval
        };
    }
}
