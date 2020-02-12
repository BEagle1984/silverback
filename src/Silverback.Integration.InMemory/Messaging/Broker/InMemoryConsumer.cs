// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class InMemoryConsumer : Consumer
    {
        public InMemoryConsumer(
            InMemoryBroker broker,
            IConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors)
            : base(broker, endpoint, behaviors)
        {
        }

        /// <inheritdoc cref="Consumer" />
        public override Task Commit(IEnumerable<IOffset> offsets)
        {
            // Do nothing
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer" />
        public override Task Rollback(IEnumerable<IOffset> offsets)
        {
            // Do nothing
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer" />
        public override void Connect()
        {
        }

        /// <inheritdoc cref="Consumer" />
        public override void Disconnect()
        {
        }

        internal Task Receive(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset) =>
            HandleMessage(message, headers.ToList(), offset);
    }
}