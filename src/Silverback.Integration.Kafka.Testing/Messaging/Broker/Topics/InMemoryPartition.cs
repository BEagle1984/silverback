// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Topics
{
    public class InMemoryPartition
    {
        private readonly List<Message<byte[], byte[]>> _messages = new List<Message<byte[], byte[]>>();

        public InMemoryPartition(in int index, InMemoryTopic topic)
        {
            Partition = new Partition(index);
            Topic = topic;
        }

        public Partition Partition { get; }

        public InMemoryTopic Topic { get; }

        public Offset LastOffset => new Offset(_messages.Count - 1);

        public Offset Add(Message<byte[], byte[]> message)
        {
            // TODO: Rollover?

            lock (_messages)
            {
                _messages.Add(message);

                return new Offset(_messages.Count - 1);
            }
        }

        public bool TryPull(Offset offset, out ConsumeResult<byte[], byte[]>? result)
        {
            if (_messages.Count < offset.Value + 1)
            {
                result = null;
                return false;
            }

            // TODO: Will need to handle offset offset (if rolled over)

            result = new ConsumeResult<byte[], byte[]>
            {
                IsPartitionEOF = false,
                Message = _messages[(int)offset.Value],
                Offset = offset,
                Partition = Partition,
                Topic = Topic.Name
            };
            return true;
        }
    }
}
