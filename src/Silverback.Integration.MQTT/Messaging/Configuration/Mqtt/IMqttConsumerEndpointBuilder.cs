// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet.Protocol;

namespace Silverback.Messaging.Configuration.Mqtt
{
    /// <summary>
    ///     Builds the <see cref="MqttConsumerEndpoint" />.
    /// </summary>
    public interface IMqttConsumerEndpointBuilder : IConsumerEndpointBuilder<IMqttConsumerEndpointBuilder>
    {
        /// <summary>
        ///     Specifies the name of the topics or the topic filter strings.
        /// </summary>
        /// <param name="topics">
        ///     The name of the topics or the topic filter string.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttConsumerEndpointBuilder ConsumeFrom(params string[] topics);

        /// <summary>
        ///     Specifies the desired quality of service level.
        /// </summary>
        /// <param name="qosLevel">
        ///     The <see cref="MqttQualityOfServiceLevel" />.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttConsumerEndpointBuilder WithQualityOfServiceLevel(MqttQualityOfServiceLevel qosLevel);

        /// <summary>
        ///     Specifies that the topics have to be subscribed with the <i>at most once</i> quality of service level.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttConsumerEndpointBuilder WithAtMostOnceQoS();

        /// <summary>
        ///     Specifies that the topics have to be subscribed with the <i>at least once</i> quality of service
        ///     level.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttConsumerEndpointBuilder WithAtLeastOnceQoS();

        /// <summary>
        ///     Specifies that the topics have to be subscribed with the <i>exactly once</i> quality of service level.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttConsumerEndpointBuilder WithExactlyOnceQoS();
    }
}
