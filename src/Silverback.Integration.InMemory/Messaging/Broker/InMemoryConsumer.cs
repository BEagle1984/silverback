// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class InMemoryConsumer : Consumer
    {
        public InMemoryConsumer(
            InMemoryBroker broker,
            IConsumerEndpoint endpoint,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider,
            ILogger<InMemoryConsumer> logger)
            : base(broker, endpoint, behaviors, serviceProvider, logger)
        {
        }

        /// <inheritdoc cref="Consumer" />
        public override Task Commit(IReadOnlyCollection<IOffset> offsets)
        {
            // Do nothing
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer" />
        public override Task Rollback(IReadOnlyCollection<IOffset> offsets)
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