// Copyright (c) 2024 Sergio Aquilini
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
public class MqttConsumerEndpointConfigurationBuilder<TMessage>
    : ConsumerEndpointConfigurationBuilder<TMessage, MqttConsumerEndpointConfiguration, MqttConsumerEndpointConfigurationBuilder<TMessage>>
{
    private string[]? _topicNames;

    private MqttQualityOfServiceLevel? _qualityOfServiceLevel;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttConsumerEndpointConfigurationBuilder{TMessage}" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="friendlyName">
    ///     An optional friendly to be shown in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </param>
    public MqttConsumerEndpointConfigurationBuilder(IServiceProvider serviceProvider, string? friendlyName = null)
        : base(serviceProvider, friendlyName)
    {
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.This" />
    protected override MqttConsumerEndpointConfigurationBuilder<TMessage> This => this;

    /// <summary>
    ///     Specifies the name of the topics or the topic filter strings.
    /// </summary>
    /// <param name="topics">
    ///     The name of the topics or the topic filter string.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(params string[] topics)
    {
        Check.HasNoEmpties(topics, nameof(topics));
        _topicNames = topics;
        return this;
    }

    /// <summary>
    ///     Specifies the desired quality of service level.
    /// </summary>
    /// <param name="qosLevel">
    ///     The <see cref="MqttQualityOfServiceLevel" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttConsumerEndpointConfigurationBuilder<TMessage> WithQualityOfServiceLevel(MqttQualityOfServiceLevel qosLevel)
    {
        _qualityOfServiceLevel = qosLevel;
        return this;
    }

    /// <summary>
    ///     Specifies that the topics have to be subscribed with the <i>at most once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttConsumerEndpointConfigurationBuilder<TMessage> WithAtMostOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);

    /// <summary>
    ///     Specifies that the topics have to be subscribed with the <i>at least once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttConsumerEndpointConfigurationBuilder<TMessage> WithAtLeastOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

    /// <summary>
    ///     Specifies that the topics have to be subscribed with the <i>exactly once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttConsumerEndpointConfigurationBuilder<TMessage> WithExactlyOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.CreateConfiguration" />
    protected override MqttConsumerEndpointConfiguration CreateConfiguration()
    {
        MqttConsumerEndpointConfiguration configuration = new();

        configuration = configuration with
        {
            Topics = _topicNames?.AsValueReadOnlyCollection() ?? configuration.Topics,
            QualityOfServiceLevel = _qualityOfServiceLevel ?? configuration.QualityOfServiceLevel
        };

        return configuration;
    }
}
