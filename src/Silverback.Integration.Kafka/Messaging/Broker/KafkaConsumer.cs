// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class KafkaConsumer : Consumer<KafkaBroker, KafkaConsumerEndpoint, KafkaOffset>, IDisposable
    {
        private readonly ILogger<KafkaConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        private InnerConsumerWrapper _innerConsumer;
        private int _messagesSinceCommit;

        public KafkaConsumer(
            KafkaBroker broker,
            KafkaConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumer> logger)
            : base(broker, endpoint, behaviors)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <inheritdoc cref="Consumer" />
        public override void Connect()
        {
            if (_innerConsumer != null)
                return;

            _innerConsumer = new InnerConsumerWrapper(
                Endpoint.Configuration.ConfluentConfig,
                Endpoint.Configuration.EnableAutoRecovery,
                _serviceProvider,
                _logger);

            _innerConsumer.Subscribe(Endpoint);
            _innerConsumer.Received += OnMessageReceived;
            _innerConsumer.StartConsuming();

            _logger.LogDebug("Connected consumer to topic {topic}. (BootstrapServers=\"{bootstrapServers}\")",
                Endpoint.Name, Endpoint.Configuration.BootstrapServers);
        }

        /// <inheritdoc cref="Consumer" />
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

        public void Dispose() => Disconnect();

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
                    headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey, Encoding.UTF8.GetString(message.Key));
                
                await HandleMessage(
                    message.Value,
                    headers,
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

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}" />
        protected override Task Commit(IEnumerable<KafkaOffset> offsets)
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

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}" />
        protected override Task Rollback(IEnumerable<KafkaOffset> offsets)
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