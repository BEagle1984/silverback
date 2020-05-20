// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}" />
    public class KafkaConsumer : Consumer<KafkaBroker, KafkaConsumerEndpoint, KafkaOffset>
    {
        private readonly ILogger<KafkaConsumer> _logger;

        private readonly IServiceProvider _serviceProvider;

        private KafkaInnerConsumerWrapper? _innerConsumer;

        private IKafkaMessageSerializer? _serializer;

        private int _messagesSinceCommit;

        /// <summary> Initializes a new instance of the <see cref="KafkaConsumer" /> class. </summary>
        /// <param name="broker"> The <see cref="IBroker" /> that is instantiating the consumer. </param>
        /// <param name="endpoint"> The endpoint to be consumed. </param>
        /// <param name="callback"> The delegate to be invoked when a message is received. </param>
        /// <param name="behaviors"> The behaviors to be added to the pipeline. </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed
        ///     services.
        /// </param>
        /// <param name="logger"> The <see cref="ILogger" />. </param>
        public KafkaConsumer(
            KafkaBroker broker,
            KafkaConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumer> logger)
            : base(broker, endpoint, callback, behaviors, serviceProvider, logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <inheritdoc />
        public override void Connect()
        {
            if (_innerConsumer != null)
                return;

            _innerConsumer = new KafkaInnerConsumerWrapper(
                Endpoint,
                _serviceProvider.GetRequiredService<KafkaEventsHandler>(),
                _logger);

            _serializer = Endpoint.Serializer as IKafkaMessageSerializer ??
                          new DefaultKafkaMessageSerializer(Endpoint.Serializer);

            _innerConsumer.Received += OnMessageReceived;
            _innerConsumer.StartConsuming();

            _logger.LogDebug(
                EventIds.KafkaConsumerConnected,
                "Connected consumer to topic {topic}. (BootstrapServers=\"{bootstrapServers}\")",
                Endpoint.Name,
                Endpoint.Configuration.BootstrapServers);
        }

        /// <inheritdoc />
        public override void Disconnect()
        {
            if (_innerConsumer == null)
                return;

            _innerConsumer.StopConsuming();

            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                _innerConsumer.CommitAll();

            _innerConsumer.Dispose();
            _innerConsumer = null;

            _logger.LogDebug(
                EventIds.KafkaConsumerDisconnected,
                "Disconnected consumer from topic {topic}. (BootstrapServers=\"{bootstrapServers}\")",
                Endpoint.Name,
                Endpoint.Configuration.BootstrapServers);
        }

        /// <inheritdoc />
        protected override Task Commit(IReadOnlyCollection<KafkaOffset> offsets)
        {
            if (_innerConsumer == null)
                throw new InvalidOperationException("The consumer is not connected.");

            var lastOffsets = offsets
                .GroupBy(o => o.Key)
                .Select(
                    g => g
                        .OrderByDescending(o => o.Value)
                        .First()
                        .AsTopicPartitionOffset())
                .ToList();

            _innerConsumer.StoreOffset(
                lastOffsets
                    .Select(o => new TopicPartitionOffset(o.TopicPartition, o.Offset + 1))
                    .ToArray());

            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                CommitOffsets(lastOffsets);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task Rollback(IReadOnlyCollection<KafkaOffset> offsets)
        {
            // Nothing to do here. With Kafka the uncommitted messages will be implicitly re-consumed.
            return Task.CompletedTask;
        }

        private async Task OnMessageReceived(
            Message<byte[], byte[]> message,
            TopicPartitionOffset topicPartitionOffset)
        {
            // Checking if the message was sent to the subscribed topic is necessary
            // when reusing the same consumer for multiple topics.
            if (!Endpoint.Names.Any(
                endpointName =>
                    topicPartitionOffset.Topic.Equals(endpointName, StringComparison.OrdinalIgnoreCase)))
                return;

            await TryHandleMessage(message, topicPartitionOffset);
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task TryHandleMessage(Message<byte[], byte[]> message, TopicPartitionOffset tpo)
        {
            KafkaOffset? offset = null;

            try
            {
                if (_serializer == null)
                    throw new InvalidOperationException("The consumer is not connected.");

                _messagesSinceCommit++;
                offset = new KafkaOffset(tpo);

                var headers = new MessageHeaderCollection(message.Headers.ToSilverbackHeaders());

                if (message.Key != null)
                {
                    headers.AddOrReplace(
                        KafkaMessageHeaders.KafkaMessageKey,
                        _serializer.DeserializeKey(
                            message.Key,
                            headers,
                            new MessageSerializationContext(Endpoint, tpo.Topic)));
                }

                await HandleMessage(
                    message.Value,
                    headers,
                    tpo.Topic,
                    offset);
            }
            catch (Exception ex)
            {
                const string errorMessage = "Fatal error occurred consuming the message {offset} from endpoint " +
                                            "{endpointName}. The consumer will be stopped.";

                _logger.LogCritical(
                    EventIds.KafkaConsumerFatalError,
                    ex,
                    errorMessage,
                    offset?.Value,
                    Endpoint.Name);

                Disconnect();
            }
        }

        private void CommitOffsets(IEnumerable<TopicPartitionOffset> offsets)
        {
            if (_innerConsumer == null)
                throw new InvalidOperationException("The consumer is not connected.");

            if (++_messagesSinceCommit < Endpoint.Configuration.CommitOffsetEach)
                return;

            _innerConsumer.Commit(offsets);

            _messagesSinceCommit = 0;
        }
    }
}
