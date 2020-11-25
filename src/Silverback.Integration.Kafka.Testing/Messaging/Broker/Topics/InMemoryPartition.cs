// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Topics
{
    /// <summary>
    ///     A partition in the <see cref="InMemoryTopic" />.
    /// </summary>
    public class InMemoryPartition
    {
        private const int MaxRetainedMessages = 100;

        private readonly List<Message<byte[]?, byte[]?>> _messages = new List<Message<byte[]?, byte[]?>>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryPartition" /> class.
        /// </summary>
        /// <param name="index">
        ///     The partition index.
        /// </param>
        /// <param name="topic">
        ///     The related <see cref="InMemoryTopic" />.
        /// </param>
        public InMemoryPartition(in int index, InMemoryTopic topic)
        {
            Partition = new Partition(index);
            Topic = topic;
        }

        /// <summary>
        ///     Gets the <see cref="Partition" /> structure describing this partition.
        /// </summary>
        public Partition Partition { get; }

        /// <summary>
        ///     Gets the <see cref="InMemoryTopic" /> this partition belongs to.
        /// </summary>
        public InMemoryTopic Topic { get; }

        /// <summary>
        ///     Gets the <see cref="Offset" /> of the first message in the partition.
        /// </summary>
        public Offset FirstOffset { get; private set; } = Offset.Unset;

        /// <summary>
        ///     Gets the <see cref="Offset" /> of the latest written message.
        /// </summary>
        public Offset LastOffset { get; private set; } = Offset.Unset;

        /// <summary>
        ///     Writes the specified message to the partition.
        /// </summary>
        /// <param name="message">
        ///     The message to be written.
        /// </param>
        /// <returns>
        ///     The <see cref="Offset" /> at which the message was written.
        /// </returns>
        public Offset Add(Message<byte[]?, byte[]?> message)
        {
            lock (_messages)
            {
                if (_messages.Count == 0)
                    LastOffset = FirstOffset = new Offset(0);
                else
                    LastOffset++;

                _messages.Add(message);

                if (_messages.Count > MaxRetainedMessages)
                {
                    _messages.RemoveAt(0);
                    FirstOffset++;
                }

                return new Offset(LastOffset);
            }
        }

        /// <summary>
        ///     Pulls the message at the specified offset.
        /// </summary>
        /// <param name="offset">
        ///     The offset of the message to be pulled.
        /// </param>
        /// <param name="result">
        ///     The pulled message, or <c>null</c> if no message is found at the specified offset.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the message was found at the specified offset; otherwise, <c>false</c>.
        /// </returns>
        public bool TryPull(Offset offset, out ConsumeResult<byte[]?, byte[]?>? result)
        {
            lock (_messages)
            {
                if (offset.Value > LastOffset || _messages.Count == 0)
                {
                    result = null;
                    return false;
                }

                result = new ConsumeResult<byte[]?, byte[]?>
                {
                    IsPartitionEOF = false,
                    Message = _messages[(int)(offset.Value - Math.Max(0, FirstOffset.Value))],
                    Offset = offset,
                    Partition = Partition,
                    Topic = Topic.Name
                };
            }

            return true;
        }
    }
}
