// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Topics;

namespace Silverback.Messaging.Broker.ConfluentWrappers
{
    public class MockedConfluentConsumerBuilder : IConfluentConsumerBuilder
    {
        private readonly IInMemoryTopicCollection _topics;

        private ConsumerConfig? _config;

        private Action<IConsumer<byte[]?, byte[]?>, string>? _statisticsHandler;

        private Action<IConsumer<byte[]?, byte[]?>, Error>? _errorHandler;

        private Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>?
            _partitionsAssignedHandler;

        private Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>>?
            _partitionsRevokedHandler;

        private Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets>? _offsetsCommittedHandler;

        public MockedConfluentConsumerBuilder(IInMemoryTopicCollection topics)
        {
            _topics = topics;
        }

        public IConfluentConsumerBuilder SetConfig(ConsumerConfig config)
        {
            _config = config;
            return this;
        }

        public IConfluentConsumerBuilder SetStatisticsHandler(
            Action<IConsumer<byte[]?, byte[]?>, string> statisticsHandler)
        {
            _statisticsHandler = statisticsHandler;
            return this;
        }

        public IConfluentConsumerBuilder SetErrorHandler(Action<IConsumer<byte[]?, byte[]?>, Error> errorHandler)
        {
            _errorHandler = errorHandler;
            return this;
        }

        public IConfluentConsumerBuilder SetPartitionsAssignedHandler(
            Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>
                partitionsAssignedHandler)
        {
            _partitionsAssignedHandler = partitionsAssignedHandler;
            return this;
        }

        public IConfluentConsumerBuilder SetPartitionsAssignedHandler(
            Action<IConsumer<byte[]?, byte[]?>, List<TopicPartition>> partitionsAssignedHandler)
        {
            _partitionsAssignedHandler = (consumer, partitions) =>
            {
                partitionsAssignedHandler(consumer, partitions);

                return partitions
                    .Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Unset))
                    .ToList();
            };

            return this;
        }

        public IConfluentConsumerBuilder SetPartitionsRevokedHandler(
            Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>>
                partitionsRevokedHandler)
        {
            _partitionsRevokedHandler = partitionsRevokedHandler;
            return this;
        }

        public IConfluentConsumerBuilder SetPartitionsRevokedHandler(
            Action<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>> partitionsRevokedHandler)
        {
            _partitionsRevokedHandler = (consumer, partitions) =>
            {
                partitionsRevokedHandler(consumer, partitions);

                return Enumerable.Empty<TopicPartitionOffset>();
            };

            return this;
        }

        public IConfluentConsumerBuilder SetOffsetsCommittedHandler(
            Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets> offsetsCommittedHandler)
        {
            _offsetsCommittedHandler = offsetsCommittedHandler;
            return this;
        }

        public IConsumer<byte[]?, byte[]?> Build()
        {
            if (_config == null)
                throw new InvalidOperationException("SetConfig must be called to provide the consumer configuration.");

            var consumer = new MockedKafkaConsumer(_config, _topics);

            // TODO: Use event handlers

            // if (_statisticsHandler != null)
            //     builder.SetStatisticsHandler(_statisticsHandler);
            //
            // if (_errorHandler != null)
            //     builder.SetErrorHandler(_errorHandler);
            //
            // if (_partitionsAssignedHandler != null)
            //     builder.SetPartitionsAssignedHandler(_partitionsAssignedHandler);
            //
            // if (_partitionsRevokedHandler != null)
            //     builder.SetPartitionsRevokedHandler(_partitionsRevokedHandler);
            //
            // if (_offsetsCommittedHandler != null)
            //     builder.SetOffsetsCommittedHandler(_offsetsCommittedHandler);

            return consumer;
        }
    }
}
