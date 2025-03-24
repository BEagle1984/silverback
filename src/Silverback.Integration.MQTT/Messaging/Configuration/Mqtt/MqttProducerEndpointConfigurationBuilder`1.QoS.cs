// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet.Protocol;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <content>
///     Implements the <c>WithQualityOfServiceLevel</c> and related methods.
/// </content>
public partial class MqttProducerEndpointConfigurationBuilder<TMessage>
{
    /// <summary>
    ///     Specifies the desired quality of service level.
    /// </summary>
    /// <param name="qosLevel">
    ///     The <see cref="MqttQualityOfServiceLevel" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> WithQualityOfServiceLevel(MqttQualityOfServiceLevel qosLevel)
    {
        _qualityOfServiceLevel = qosLevel;
        return this;
    }

    /// <summary>
    ///     Specifies that the messages have to be sent with the <i>at most once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> WithAtMostOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);

    /// <summary>
    ///     Specifies that the messages have to be sent with the <i>at least once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> WithAtLeastOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

    /// <summary>
    ///     Specifies that the messages have to be sent with the <i>exactly once</i> quality of service level.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttProducerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> WithExactlyOnceQoS() =>
        WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);
}
