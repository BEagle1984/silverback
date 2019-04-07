// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

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

        public void Publish(byte[] message, IEnumerable<MessageHeader> headers)
        {
            var offset = new InMemoryOffset(Name, NextOffset);
            _consumers.ForEach(c => c.Receive(message, headers, offset));

            NextOffset++;
        }

        public InMemoryConsumer Subscribe(InMemoryConsumer consumer)
        {
            _consumers.Add(consumer);
            return consumer;
        }
    }
}