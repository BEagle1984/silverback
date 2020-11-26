// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet.Protocol;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="MqttProducerEndpoint" />.
    /// </summary>
    public interface IMqttProducerEndpointBuilder : IProducerEndpointBuilder<IMqttProducerEndpointBuilder>
    {
        /// <summary>
        ///     Specifies the name of the topic.
        /// </summary>
        /// <param name="topicName">
        ///     The name of the topic.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder ProduceTo(string topicName);

        /// <summary>
        ///     Specifies the desired quality of service level.
        /// </summary>
        /// t
        /// <param name="qosLevel">
        ///     The <see cref="MqttQualityOfServiceLevel" />.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder WithQualityOfServiceLevel(MqttQualityOfServiceLevel qosLevel);

        /// <summary>
        ///     Specifies that the messages have to be sent with the <i>at most once</i> quality of service level.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder WithAtMostOnceQoS();

        /// <summary>
        ///     Specifies that the messages have to be sent with the <i>at least once</i> quality of service level.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder WithAtLeastOnceQoS();

        /// <summary>
        ///     Specifies that the messages have to be sent with the <i>exactly once</i> quality of service level.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder WithExactlyOnceQoS();

        /// <summary>
        ///     Specifies that the messages have to be sent with the retain flag, causing them to be persisted on the
        ///     broker.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder Retain();

        /// <summary>
        ///     Sets the message expiry interval. This interval defines the period of time that the broker stores
        ///     the <i>PUBLISH</i> message for any matching subscribers that are not currently connected. When no
        ///     message expiry interval is set, the broker must store the message for matching subscribers indefinitely.
        /// </summary>
        /// <param name="messageExpiryInterval">
        ///     The <see cref="TimeSpan" /> representing the message expiry interval.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder WithMessageExpiration(TimeSpan messageExpiryInterval);
    }
}
