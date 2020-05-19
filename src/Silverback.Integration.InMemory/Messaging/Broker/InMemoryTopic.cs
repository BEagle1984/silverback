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
        private readonly List<InMemoryConsumer> _consumers = new List<InMemoryConsumer>();

        public InMemoryTopic(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public int NextOffset { get; private set; }

        [SuppressMessage("ReSharper", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public IOffset Publish(byte[]? message, IEnumerable<MessageHeader> headers) =>
            AsyncHelper.RunSynchronously(() => PublishAsync(message, headers));

        [SuppressMessage("ReSharper", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task<IOffset> PublishAsync(byte[]? message, IEnumerable<MessageHeader> headers)
        {
            var offset = new InMemoryOffset(Name, NextOffset);
            await _consumers.ForEachAsync(c => c.Receive(message, headers, offset));

            NextOffset++;

            return offset;
        }

        public InMemoryConsumer Subscribe(InMemoryConsumer consumer)
        {
            _consumers.Add(consumer);
            return consumer;
        }
    }
}