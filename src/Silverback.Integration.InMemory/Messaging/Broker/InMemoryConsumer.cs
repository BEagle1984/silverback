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
    /// <inheritdoc cref="Consumer" />
    public class InMemoryConsumer : Consumer
    {
        public InMemoryConsumer(
            InMemoryBroker broker,
            IConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider,
            ILogger<InMemoryConsumer> logger)
            : base(broker, endpoint, callback, behaviors, serviceProvider, logger)
        {
        }

        public event EventHandler<IReadOnlyCollection<IOffset>> CommitCalled;

        public event EventHandler<IReadOnlyCollection<IOffset>> RollbackCalled;

        /// <inheritdoc />
        public override Task Commit(IReadOnlyCollection<IOffset> offsets)
        {
            CommitCalled?.Invoke(this, offsets);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task Rollback(IReadOnlyCollection<IOffset> offsets)
        {
            RollbackCalled?.Invoke(this, offsets);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override void Connect()
        {
        }

        /// <inheritdoc />
        public override void Disconnect()
        {
        }

        // TODO: Should pass the actual endpoint name via header (Endpoint.Name may contain a list of topics)
        public Task Receive(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset) =>
            HandleMessage(message, headers.ToList(), Endpoint.Name, offset);
    }
}
