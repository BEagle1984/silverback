// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        private KafkaInnerConsumerWrapper _innerConsumer;
        private IKafkaMessageSerializer _serializer;
        private int _messagesSinceCommit;

        public KafkaConsumer(
            KafkaBroker broker,
            KafkaConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumer> logger)
            : base(broker, endpoint, callback, behaviors, serviceProvider, logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

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

            _logger.LogDebug("Connected consumer to topic {topic}. (BootstrapServers=\"{bootstrapServers}\")",
                Endpoint.Name, Endpoint.Configuration.BootstrapServers);
        }

        public override void Disconnect()
        {
            if (_innerConsumer == null)
                return;

            _innerConsumer.StopConsuming();

            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                _innerConsumer.CommitAll();

            _innerConsumer.Dispose();
            _innerConsumer = null;

            _logger.LogDebug("Disconnected consumer from topic {topic}. (BootstrapServers=\"{bootstrapServers}\")",
                Endpoint.Name, Endpoint.Configuration.BootstrapServers);
        }

        private async Task OnMessageReceived(
            Confluent.Kafka.Message<byte[], byte[]> message,
            Confluent.Kafka.TopicPartitionOffset topicPartitionOffset)
        {
            // Checking if the message was sent to the subscribed topic is necessary
            // when reusing the same consumer for multiple topics.
            if (!Endpoint.Names.Any(endpointName =>
                topicPartitionOffset.Topic.Equals(endpointName, StringComparison.InvariantCultureIgnoreCase)))
                return;

            await TryHandleMessage(message, topicPartitionOffset);
        }

        private async Task TryHandleMessage(
            Confluent.Kafka.Message<byte[], byte[]> message,
            Confluent.Kafka.TopicPartitionOffset tpo)
        {
            KafkaOffset offset = null;

            try
            {
                _messagesSinceCommit++;
                offset = new KafkaOffset(tpo);

                var headers = new MessageHeaderCollection(message.Headers.ToSilverbackHeaders());

                if (message.Key != null)
                    headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey,
                        _serializer.DeserializeKey(
                            message.Key,
                            headers,
                            new MessageSerializationContext(Endpoint, tpo.Topic)));

                await HandleMessage(
                    message.Value,
                    headers,
                    tpo.Topic,
                    offset);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Fatal error occurred consuming the message {offset} from endpoint {endpointName}. " +
                    "The consumer will be stopped.",
                    offset?.Value, Endpoint.Name);

                Disconnect();
            }
        }

        protected override Task Commit(IReadOnlyCollection<KafkaOffset> offsets)
        {
            var lastOffsets = offsets
                .GroupBy(o => o.Key)
                .Select(g => g
                    .OrderByDescending(o => o.Value)
                    .First()
                    .AsTopicPartitionOffset())
                .ToList();

            _innerConsumer.StoreOffset(lastOffsets
                .Select(o => new Confluent.Kafka.TopicPartitionOffset(o.TopicPartition, o.Offset + 1))
                .ToArray());

            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                CommitOffsets(lastOffsets);

            return Task.CompletedTask;
        }

        protected override Task Rollback(IReadOnlyCollection<KafkaOffset> offsets)
        {
            // Nothing to do here. With Kafka the uncommitted messages will be implicitly re-consumed. 
            return Task.CompletedTask;
        }

        private void CommitOffsets(IEnumerable<Confluent.Kafka.TopicPartitionOffset> offsets)
        {
            if (++_messagesSinceCommit < Endpoint.Configuration.CommitOffsetEach) return;

            _innerConsumer.Commit(offsets);

            _messagesSinceCommit = 0;
        }
    }
}