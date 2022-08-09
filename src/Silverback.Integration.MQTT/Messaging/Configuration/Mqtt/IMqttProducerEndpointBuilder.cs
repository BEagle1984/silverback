// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet.Protocol;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;

namespace Silverback.Messaging.Configuration.Mqtt
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
        ///     Specifies the name of the topic and optionally the target partition.
        /// </summary>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced. If the function returns
        ///     <c>null</c> the message will not be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder ProduceTo(Func<IOutboundEnvelope, string?> topicNameFunction);

        /// <summary>
        ///     Specifies the name of the topic and optionally the target partition.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages being produced.
        /// </typeparam>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced. If the function returns
        ///     <c>null</c> the message will not be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder ProduceTo<TMessage>(
            Func<IOutboundEnvelope<TMessage>, string?> topicNameFunction)
            where TMessage : class;

        /// <summary>
        ///     Specifies the name of the topic and optionally the target partition.
        /// </summary>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced. If the function returns
        ///     <c>null</c> the message will not be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder ProduceTo(
            Func<IOutboundEnvelope, IServiceProvider, string?> topicNameFunction);

        /// <summary>
        ///     Specifies the name of the topic and optionally the target partition.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages being produced.
        /// </typeparam>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced. If the function returns
        ///     <c>null</c> the message will not be produced.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder ProduceTo<TMessage>(
            Func<IOutboundEnvelope<TMessage>, IServiceProvider, string?> topicNameFunction)
            where TMessage : class;

        /// <summary>
        ///     Specifies the name of the topic and optionally the target partition.
        /// </summary>
        /// <param name="topicNameFormatString">
        ///     The endpoint name format string that will be combined with the arguments returned by the
        ///     <paramref name="topicNameArgumentsFunction" /> using a <c>string.Format</c>.
        /// </param>
        /// <param name="topicNameArgumentsFunction">
        ///     The function returning the arguments to be used to format the string.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder ProduceTo(
            string topicNameFormatString,
            Func<IOutboundEnvelope, string[]> topicNameArgumentsFunction);

        /// <summary>
        ///     Specifies the name of the topic and optionally the target partition.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages being produced.
        /// </typeparam>
        /// <param name="topicNameFormatString">
        ///     The endpoint name format string that will be combined with the arguments returned by the
        ///     <paramref name="topicNameArgumentsFunction" /> using a <c>string.Format</c>.
        /// </param>
        /// <param name="topicNameArgumentsFunction">
        ///     The function returning the arguments to be used to format the string.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder ProduceTo<TMessage>(
            string topicNameFormatString,
            Func<IOutboundEnvelope<TMessage>, string[]> topicNameArgumentsFunction)
            where TMessage : class;

        /// <summary>
        ///     Specifies the type of the <see cref="IProducerEndpointNameResolver" /> to be used to resolve the
        ///     actual endpoint name and partition.
        /// </summary>
        /// <typeparam name="TResolver">
        ///     The type of the <see cref="IProducerEndpointNameResolver" /> to be used.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder UseEndpointNameResolver<TResolver>()
            where TResolver : IProducerEndpointNameResolver;

        /// <summary>
        ///     Configures the MQTT client properties.
        /// </summary>
        /// <param name="configAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientConfig" /> and configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder Configure(Action<MqttClientConfig> configAction);

        /// <summary>
        ///     Configures the MQTT client properties.
        /// </summary>
        /// <param name="configBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IMqttClientConfigBuilder" /> and configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttProducerEndpointBuilder Configure(Action<IMqttClientConfigBuilder> configBuilderAction);

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
