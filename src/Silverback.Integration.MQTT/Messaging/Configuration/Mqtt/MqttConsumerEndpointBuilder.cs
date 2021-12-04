// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet.Protocol;
using Silverback.Collections;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Builds the <see cref="MqttConsumerEndpoint" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being consumed.
/// </typeparam>
public class MqttConsumerConfigurationBuilder<TMessage>
    : ConsumerConfigurationBuilder<TMessage, MqttConsumerConfiguration, MqttConsumerConfigurationBuilder<TMessage>>
{
    private MqttClientConfiguration _clientConfiguration;

    private string[]? _topicNames;

    private MqttQualityOfServiceLevel? _qualityOfServiceLevel;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttConsumerConfigurationBuilder{TMessage}" /> class.
    /// </summary>
    /// <param name="clientConfig">
    ///     The <see cref="MqttClientConfiguration" />.
    /// </param>
    /// <param name="endpointsConfigurationBuilder">
    ///     The optional <see cref="EndpointsConfigurationBuilder" /> that instantiated the builder.
    /// </param>
    public MqttConsumerConfigurationBuilder(
        MqttClientConfiguration? clientConfig = null,
        EndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
        : base(endpointsConfigurationBuilder)
    {
        _clientConfiguration = clientConfig ?? new MqttClientConfiguration();
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.EndpointRawName" />
    // TODO: Test
    public override string? EndpointRawName => _topicNames == null ? null : string.Join(',', _topicNames);

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TEndpoint,TBuilder}.This" />
    protected override MqttConsumerConfigurationBuilder<TMessage> This => this;

    /// <summary>
    ///     Specifies the name of the topics or the topic filter strings.
    /// </summary>
    /// <param name="topics">
    ///     The name of the topics or the topic filter string.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttConsumerConfigurationBuilder<TMessage> ConsumeFrom(params string[] topics)
    {
        Check.HasNoEmpties(topics, nameof(topics));
        _topicNames = topics;
        return this;
    }

    /// <summary>
    ///     Configures the MQTT client settings.
    /// </summary>
    /// <param name="clientConfigurationAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientConfiguration" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttConsumerConfigurationBuilder<TMessage> ConfigureClient(Action<MqttClientConfiguration> clientConfigurationAction)
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
    ///     The <see cref="MqttConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttConsumerConfigurationBuilder<TMessage> ConfigureClient(Action<MqttClientConfigurationBuilder> configurationBuilderAction)
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
    ///     The <see cref="MqttConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttConsumerConfigurationBuilder<TMessage> WithQualityOfServiceLevel(MqttQualityOfServiceLevel qosLevel)
    {
        _qualityOfServiceLevel = qosLevel;
        return this;
    }

    /// <summary>
    ///     Specifies that the topics have to be subscribed with the <i>at most once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttConsumerConfigurationBuilder<TMessage> WithAtMostOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);

    /// <summary>
    ///     Specifies that the topics have to be subscribed with the <i>at least once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttConsumerConfigurationBuilder<TMessage> WithAtLeastOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

    /// <summary>
    ///     Specifies that the topics have to be subscribed with the <i>exactly once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttConsumerConfigurationBuilder<TMessage> WithExactlyOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TEndpoint,TBuilder}.CreateConfiguration" />
    protected override MqttConsumerConfiguration CreateConfiguration()
    {
        MqttConsumerConfiguration configuration = new();

        configuration = configuration with
        {
            Topics = _topicNames?.AsValueReadOnlyCollection() ?? configuration.Topics,
            Client = _clientConfiguration
        };

        if (_qualityOfServiceLevel != null)
            configuration = configuration with { QualityOfServiceLevel = _qualityOfServiceLevel.Value };

        return configuration;
    }
}
