// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.ConfluentWrappers
{
    /// <summary>
    ///     Wraps the <see cref="Confluent.Kafka.ConsumerBuilder{TKey,TValue}" />.
    /// </summary>
    [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
    public class ConfluentConsumerBuilder : IConfluentConsumerBuilder
    {
        private ConsumerConfig? _config;

        private Action<IConsumer<byte[]?, byte[]?>, string>? _statisticsHandler;

        private Action<IConsumer<byte[]?, byte[]?>, Error>? _errorHandler;

        private Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>?
            _partitionsAssignedHandler;

        private Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>>?
            _partitionsRevokedHandler;

        private Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets>? _offsetsCommittedHandler;

        private Action<IConsumer<byte[]?, byte[]?>, LogMessage>? _logHandler;

        /// <inheritdoc cref="IConfluentConsumerBuilder.SetConfig" />
        public IConfluentConsumerBuilder SetConfig(ConsumerConfig config)
        {
            _config = config;
            return this;
        }

        /// <inheritdoc cref="IConfluentConsumerBuilder.SetStatisticsHandler" />
        public IConfluentConsumerBuilder SetStatisticsHandler(
            Action<IConsumer<byte[]?, byte[]?>, string> statisticsHandler)
        {
            _statisticsHandler = statisticsHandler;
            return this;
        }

        /// <inheritdoc cref="IConfluentConsumerBuilder.SetErrorHandler" />
        public IConfluentConsumerBuilder SetErrorHandler(Action<IConsumer<byte[]?, byte[]?>, Error> errorHandler)
        {
            _errorHandler = errorHandler;
            return this;
        }

        /// <inheritdoc cref="IConfluentConsumerBuilder.SetPartitionsAssignedHandler(Func{IConsumer{byte[],byte[]},List{TopicPartition},IEnumerable{TopicPartitionOffset}})" />
        public IConfluentConsumerBuilder SetPartitionsAssignedHandler(
            Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>
                partitionsAssignedHandler)
        {
            _partitionsAssignedHandler = partitionsAssignedHandler;
            return this;
        }

        /// <inheritdoc cref="IConfluentConsumerBuilder.SetPartitionsAssignedHandler(Action{IConsumer{byte[],byte[]},List{TopicPartition}})" />
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

        /// <inheritdoc cref="IConfluentConsumerBuilder.SetPartitionsRevokedHandler(Func{IConsumer{byte[],byte[]},List{TopicPartitionOffset},IEnumerable{TopicPartitionOffset}})" />
        public IConfluentConsumerBuilder SetPartitionsRevokedHandler(
            Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>>
                partitionsRevokedHandler)
        {
            _partitionsRevokedHandler = partitionsRevokedHandler;
            return this;
        }

        /// <inheritdoc cref="IConfluentConsumerBuilder.SetPartitionsRevokedHandler(Action{IConsumer{byte[],byte[]},List{TopicPartitionOffset}})" />
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

        /// <inheritdoc cref="IConfluentConsumerBuilder.SetOffsetsCommittedHandler" />
        public IConfluentConsumerBuilder SetOffsetsCommittedHandler(
            Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets> offsetsCommittedHandler)
        {
            _offsetsCommittedHandler = offsetsCommittedHandler;
            return this;
        }

        /// <inheritdoc cref="IConfluentConsumerBuilder.SetLogHandler" />
        public IConfluentConsumerBuilder SetLogHandler(Action<IConsumer<byte[]?, byte[]?>, LogMessage> logHandler)
        {
            _logHandler = logHandler;
            return this;
        }

        /// <inheritdoc cref="IConfluentConsumerBuilder.Build" />
        public IConsumer<byte[]?, byte[]?> Build()
        {
            if (_config == null)
                throw new InvalidOperationException("SetConfig must be called to provide the consumer configuration.");

            var builder = new ConsumerBuilder<byte[]?, byte[]?>(_config);

            if (_statisticsHandler != null)
                builder.SetStatisticsHandler(_statisticsHandler);

            if (_errorHandler != null)
                builder.SetErrorHandler(_errorHandler);

            if (_partitionsAssignedHandler != null)
                builder.SetPartitionsAssignedHandler(_partitionsAssignedHandler);

            if (_partitionsRevokedHandler != null)
                builder.SetPartitionsRevokedHandler(_partitionsRevokedHandler);

            if (_offsetsCommittedHandler != null)
                builder.SetOffsetsCommittedHandler(_offsetsCommittedHandler);

            if (_logHandler != null)
                builder.SetLogHandler(_logHandler);

            return builder.Build();
        }
    }
}
