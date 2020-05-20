// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        /// <summary> Initializes a new instance of the <see cref="InMemoryConsumer" /> class. </summary>
        /// <param name="broker"> The <see cref="IBroker" /> that is instantiating the consumer. </param>
        /// <param name="endpoint"> The endpoint to be consumed. </param>
        /// <param name="receivedCallback"> The delegate to be invoked when a message is received. </param>
        /// <param name="behaviors"> The behaviors to be added to the pipeline. </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed
        ///     services.
        /// </param>
        /// <param name="logger"> The <see cref="ILogger" />. </param>
        public InMemoryConsumer(
            InMemoryBroker broker,
            IConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback receivedCallback,
            IReadOnlyCollection<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider,
            ILogger<InMemoryConsumer> logger)
            : base(broker, endpoint, receivedCallback, behaviors, serviceProvider, logger)
        {
        }

        /// <summary>
        ///     The event fired whenever the <c> Commit </c> method is called to acknowledge the successful
        ///     processing of a message.
        /// </summary>
        public event EventHandler<OffsetsEventArgs>? CommitCalled;

        /// <summary>
        ///     The event fired whenever the <c> Rollback </c> method is called to notify that the message couldn't
        ///     be processed.
        /// </summary>
        public event EventHandler<OffsetsEventArgs>? RollbackCalled;

        /// <inheritdoc />
        public override Task Commit(IReadOnlyCollection<IOffset> offsets)
        {
            CommitCalled?.Invoke(this, new OffsetsEventArgs(offsets));

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task Rollback(IReadOnlyCollection<IOffset> offsets)
        {
            RollbackCalled?.Invoke(this, new OffsetsEventArgs(offsets));

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

        /// <summary> Simulates that a message has been received. </summary>
        /// <param name="message"> The raw message body. </param>
        /// <param name="headers"> The message headers. </param>
        /// <param name="offset"> The message offset. </param>
        /// <returns> A <see cref="Task" /> representing the asynchronous operation. </returns>
        // TODO: Should pass the actual endpoint name via header (Endpoint.Name may contain a list of topics)
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public Task Receive(byte[]? message, IEnumerable<MessageHeader> headers, IOffset offset) =>
            HandleMessage(message, headers.ToList(), Endpoint.Name, offset);
    }
}
