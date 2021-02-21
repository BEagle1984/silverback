// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;

namespace Silverback.Messaging.Configuration.Kafka
{
    /// <summary>
    ///     Builds the <see cref="KafkaProducerEndpoint" />.
    /// </summary>
    public interface IKafkaProducerEndpointBuilder : IProducerEndpointBuilder<IKafkaProducerEndpointBuilder>
    {
        /// <summary>
        ///     Specifies the name of the topic and optionally the target partition.
        /// </summary>
        /// <param name="topicName">
        ///     The name of the topic.
        /// </param>
        /// <param name="partition">
        ///     The optional partition index. If <c>null</c> the partition is automatically derived from the message
        ///     key (use <see cref="KafkaKeyMemberAttribute" /> to specify a message key, otherwise a random one will be
        ///     generated).
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaProducerEndpointBuilder ProduceTo(string topicName, int? partition = null);

        /// <summary>
        ///     Specifies the name of the topic and optionally the target partition.
        /// </summary>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced.
        /// </param>
        /// <param name="partitionFunction">
        ///     The optional function returning the target partition index for the message being produced. If <c>null</c>
        ///     the partition is automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" />
        ///     to specify a message key, otherwise a random one will be generated).
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaProducerEndpointBuilder ProduceTo(
            Func<IOutboundEnvelope, string> topicNameFunction,
            Func<IOutboundEnvelope, int>? partitionFunction = null);

        /// <summary>
        ///     Specifies the name of the topic and optionally the target partition.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages being produced.
        /// </typeparam>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced.
        /// </param>
        /// <param name="partitionFunction">
        ///     The optional function returning the target partition index for the message being produced. If <c>null</c>
        ///     the partition is automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" />
        ///     to specify a message key, otherwise a random one will be generated).
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaProducerEndpointBuilder ProduceTo<TMessage>(
            Func<IOutboundEnvelope<TMessage>, string> topicNameFunction,
            Func<IOutboundEnvelope<TMessage>, int>? partitionFunction = null)
            where TMessage : class;

        /// <summary>
        ///     Specifies the name of the topic and optionally the target partition.
        /// </summary>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced.
        /// </param>
        /// <param name="partitionFunction">
        ///     The optional function returning the target partition index for the message being produced. If <c>null</c>
        ///     the partition is automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" />
        ///     to specify a message key, otherwise a random one will be generated).
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaProducerEndpointBuilder ProduceTo(
            Func<IOutboundEnvelope, IServiceProvider, string> topicNameFunction,
            Func<IOutboundEnvelope, IServiceProvider, int>? partitionFunction = null);

        /// <summary>
        ///     Specifies the name of the topic and optionally the target partition.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages being produced.
        /// </typeparam>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced.
        /// </param>
        /// <param name="partitionFunction">
        ///     The optional function returning the target partition index for the message being produced. If <c>null</c>
        ///     the partition is automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" />
        ///     to specify a message key, otherwise a random one will be generated).
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaProducerEndpointBuilder ProduceTo<TMessage>(
            Func<IOutboundEnvelope<TMessage>, IServiceProvider, string> topicNameFunction,
            Func<IOutboundEnvelope<TMessage>, IServiceProvider, int>? partitionFunction = null)
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
        /// <param name="partitionFunction">
        ///     The optional function returning the target partition index for the message being produced. If <c>null</c>
        ///     the partition is automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" />
        ///     to specify a message key, otherwise a random one will be generated).
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaProducerEndpointBuilder ProduceTo(
            string topicNameFormatString,
            Func<IOutboundEnvelope, string[]> topicNameArgumentsFunction,
            Func<IOutboundEnvelope, int>? partitionFunction = null);

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
        /// <param name="partitionFunction">
        ///     The optional function returning the target partition index for the message being produced. If <c>null</c>
        ///     the partition is automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" />
        ///     to specify a message key, otherwise a random one will be generated).
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaProducerEndpointBuilder ProduceTo<TMessage>(
            string topicNameFormatString,
            Func<IOutboundEnvelope<TMessage>, string[]> topicNameArgumentsFunction,
            Func<IOutboundEnvelope<TMessage>, int>? partitionFunction = null)
            where TMessage : class;

        /// <summary>
        ///     Specifies the type of the <see cref="IKafkaProducerEndpointNameResolver" /> to be used to resolve the
        ///     actual endpoint name and partition.
        /// </summary>
        /// <typeparam name="TResolver">
        ///     The type of the <see cref="IKafkaProducerEndpointNameResolver" /> to be used.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IKafkaProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaProducerEndpointBuilder UseEndpointNameResolver<TResolver>()
            where TResolver : IKafkaProducerEndpointNameResolver;

        /// <summary>
        ///     Configures the Kafka client properties.
        /// </summary>
        /// <param name="configAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaProducerConfig" /> and configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaProducerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaProducerEndpointBuilder Configure(Action<KafkaProducerConfig> configAction);
    }
}
