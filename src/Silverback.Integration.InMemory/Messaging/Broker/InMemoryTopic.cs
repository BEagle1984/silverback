// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    internal class InMemoryTopic
    {
        // TODO:
        // * Convert into pull mechanism
        // * Forward the topic instance to the consumer
        // * Use offset to retrieve the message at the specified position
        // * Can support partitions???

        private readonly List<InMemoryConsumer> _consumers = new List<InMemoryConsumer>();

        public InMemoryTopic(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public int NextOffset { get; private set; }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public IOffset? Publish(byte[]? message, IEnumerable<MessageHeader> headers) =>
            AsyncHelper.RunSynchronously(() => PublishAsync(message, headers));

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task<IOffset?> PublishAsync(byte[]? message, IEnumerable<MessageHeader> headers)
        {
            var offset = new InMemoryOffset(Name, NextOffset);
            await _consumers.ForEachAsync(consumer => consumer.ReceiveAsync(message, headers, offset))
                .ConfigureAwait(false);

            NextOffset++;

            return offset;
        }

        public InMemoryConsumer Subscribe(InMemoryConsumer consumer)
        {
            _consumers.Add(consumer);
            return consumer;
        }

        public void ResetOffset() => NextOffset = 0;
    }
}
