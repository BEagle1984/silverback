// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.ConfluentWrappers
{
    public interface IConfluentConsumerBuilder
    {
        IConfluentConsumerBuilder SetConfig(ConsumerConfig config);

        IConfluentConsumerBuilder SetStatisticsHandler(Action<IConsumer<byte[]?, byte[]?>, string> statisticsHandler);

        IConfluentConsumerBuilder SetErrorHandler(Action<IConsumer<byte[]?, byte[]?>, Error> errorHandler);

        IConfluentConsumerBuilder SetPartitionsAssignedHandler(
            Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>> partitionsAssignedHandler);

        IConfluentConsumerBuilder SetPartitionsAssignedHandler(
            Action<IConsumer<byte[]?, byte[]?>, List<TopicPartition>> partitionsAssignedHandler);

        IConfluentConsumerBuilder SetPartitionsRevokedHandler(
            Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>> partitionsRevokedHandler);

        IConfluentConsumerBuilder SetPartitionsRevokedHandler(
            Action<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>> partitionsRevokedHandler);

        IConfluentConsumerBuilder SetOffsetsCommittedHandler(
            Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets> offsetsCommittedHandler);

        IConsumer<byte[]?, byte[]?> Build();
    }
}
