// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public abstract class Consumer : IConsumer
    {
        private readonly IReadOnlyCollection<IConsumerBehavior> _behaviors;

        protected Consumer(IBroker broker, IConsumerEndpoint endpoint, IEnumerable<IConsumerBehavior> behaviors)
        {
            _behaviors = (IReadOnlyCollection<IConsumerBehavior>) behaviors?.ToList() ??
                         Array.Empty<IConsumerBehavior>();

            Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            Endpoint.Validate();
        }

        /// <summary>
        ///     Gets the <see cref="IBroker" /> instance that owns this instance.
        /// </summary>
        public IBroker Broker { get; }

        /// <summary>
        ///     Gets the <see cref="IConsumerEndpoint" /> this instance is connected to.
        /// </summary>
        public IConsumerEndpoint Endpoint { get; }

        /// <inheritdoc cref="IConsumer" />
        public event MessageReceivedHandler Received;

        /// <inheritdoc cref="IConsumer" />
        public Task Commit(IOffset offset) => Commit(new[] { offset });

        /// <inheritdoc cref="IConsumer" />
        public abstract Task Commit(IEnumerable<IOffset> offsets);

        /// <inheritdoc cref="IConsumer" />
        public Task Rollback(IOffset offset) => Rollback(new[] { offset });

        /// <inheritdoc cref="IConsumer" />
        public abstract Task Rollback(IEnumerable<IOffset> offsets);

        /// <inheritdoc cref="IConsumer" />
        public abstract void Connect();

        /// <inheritdoc cref="IConsumer" />
        public abstract void Disconnect();

        /// <summary>
        ///     Handles the consumed message invoking the <see cref="IConsumerBehavior" /> pipeline and finally
        ///     firing the <see cref="Received" /> event.
        /// </summary>
        /// <param name="message">The body of the consumed message.</param>
        /// <param name="headers">The headers of the consumed message.</param>
        /// <param name="offset">The offset of the consumed message.</param>
        /// <returns></returns>
        protected virtual async Task HandleMessage(byte[] message, IReadOnlyCollection<MessageHeader> headers, IOffset offset)
        {
            if (Received == null)
                throw new InvalidOperationException(
                    "A message was received but no handler is configured, please attach to the Received event.");

            await ExecutePipeline(
                _behaviors,
                new RawInboundEnvelope(message, headers, Endpoint, offset),
                envelope => Received.Invoke(this, new MessageReceivedEventArgs(envelope)));
        }

        private async Task ExecutePipeline(
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            RawBrokerEnvelope envelope,
            RawBrokerMessageHandler finalAction)
        {
            if (behaviors != null && behaviors.Any())
            {
                await behaviors.First()
                    .Handle(envelope, m => ExecutePipeline(behaviors.Skip(1).ToList(), m, finalAction));
            }
            else
            {
                await finalAction(envelope);
            }
        }
    }

    public abstract class Consumer<TBroker, TEndpoint, TOffset> : Consumer
        where TBroker : IBroker
        where TEndpoint : IConsumerEndpoint
        where TOffset : IOffset
    {
        protected Consumer(TBroker broker, TEndpoint endpoint, IEnumerable<IConsumerBehavior> behaviors)
            : base(broker, endpoint, behaviors)
        {
        }

        /// <summary>
        ///     Gets the <typeparamref name="TBroker" /> instance that owns this instance.
        /// </summary>
        protected new TBroker Broker => (TBroker) base.Broker;

        /// <summary>
        ///     Gets the <see cref="IProducerEndpoint" /> this instance is connected to.
        /// </summary>
        protected new TEndpoint Endpoint => (TEndpoint) base.Endpoint;

        /// <inheritdoc cref="IConsumer" />
        public override Task Commit(IEnumerable<IOffset> offsets) => Commit(offsets.Cast<TOffset>());

        /// <summary>
        ///     <param>Confirms that the messages at the specified offsets have been successfully processed.</param>
        ///     <param>
        ///         The acknowledgement will be sent to the message broker and the message will never be processed again
        ///         (by the same logical consumer / consumer group).
        ///     </param>
        /// </summary>
        /// <param name="offsets">The offsets to be committed.</param>
        protected abstract Task Commit(IEnumerable<TOffset> offsets);

        /// <inheritdoc cref="IConsumer" />
        public override Task Rollback(IEnumerable<IOffset> offsets) => Rollback(offsets.Cast<TOffset>());

        /// <summary>
        ///     <param>Notifies that an error occured while processing the messages at the specified offsets.</param>
        ///     <param>
        ///         If necessary the information will be sent to the message broker to ensure that the message will be
        ///         re-processed.
        ///     </param>
        /// </summary>
        /// <param name="offsets">The offsets to be rolled back.</param>
        protected abstract Task Rollback(IEnumerable<TOffset> offsets);
    }
}