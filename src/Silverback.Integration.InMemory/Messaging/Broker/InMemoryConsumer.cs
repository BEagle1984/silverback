// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class InMemoryConsumer : Consumer
    {
        public InMemoryConsumer(
            InMemoryBroker broker,
            IConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors,
            ILogger<InMemoryConsumer> logger)
            : base(broker, endpoint, behaviors, logger)
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

        // TODO: Should pass the actual endpoint name via header (Endpoint.Name may contain a list of topics)
        internal Task Receive(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset) =>
            HandleMessage(message, headers.ToList(), Endpoint.Name, offset); 
    }
}