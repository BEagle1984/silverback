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
    /// <inheritdoc cref="IConsumer" />
    public abstract class Consumer : IConsumer, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Consumer> _logger;

        protected Consumer(
            IBroker broker,
            IConsumerEndpoint endpoint,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider,
            ILogger<Consumer> logger)
        {
            Behaviors = behaviors ?? Array.Empty<IConsumerBehavior>();

            Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _serviceProvider = serviceProvider;
            _logger = logger;

            Endpoint.Validate();
        }

        public IReadOnlyCollection<IConsumerBehavior> Behaviors { get; }

        /// <summary>
        ///     Gets the <see cref="IBroker" /> instance that owns this instance.
        /// </summary>
        public IBroker Broker { get; }

        /// <summary>
        ///     Gets the <see cref="IConsumerEndpoint" /> this instance is connected to.
        /// </summary>
        public IConsumerEndpoint Endpoint { get; }

        public event MessageReceivedHandler Received;

        public Task Commit(IOffset offset) => Commit(new[] { offset });

        public abstract Task Commit(IReadOnlyCollection<IOffset> offsets);

        public Task Rollback(IOffset offset) => Rollback(new[] { offset });

        public abstract Task Rollback(IReadOnlyCollection<IOffset> offsets);

        public abstract void Connect();

        public abstract void Disconnect();

        public void Dispose()
        {
            try
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error occurred while disposing consumer from endpoint {endpoint}.",
                    Endpoint.Name);
            }
        }

        /// <summary>
        ///     Handles the consumed message invoking the <see cref="IConsumerBehavior" /> pipeline and finally
        ///     firing the <see cref="Received" /> event.
        /// </summary>
        /// <param name="message">The body of the consumed message.</param>
        /// <param name="headers">The headers of the consumed message.</param>
        /// <param name="sourceEndpointName">The name of the actual endpoint (topic) where the message has been delivered.</param>
        /// <param name="offset">The offset of the consumed message.</param>
        /// <returns></returns>
        protected virtual async Task HandleMessage(
            byte[] message,
            IReadOnlyCollection<MessageHeader> headers,
            string sourceEndpointName,
            IOffset offset)
        {
            if (Received == null)
                throw new InvalidOperationException(
                    "A message was received but no handler is configured, please attach to the Received event.");

            await ExecutePipeline(
                Behaviors,
                new ConsumerPipelineContext(
                    new[]
                    {
                        new RawInboundEnvelope(message, headers, Endpoint, sourceEndpointName, offset)
                    },
                    this),
                _serviceProvider,
                (context, serviceProvider) => Received.Invoke(this,
                    new MessagesReceivedEventArgs(context.Envelopes, serviceProvider)));
        }

        private async Task ExecutePipeline(
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler finalAction)
        {
            if (behaviors != null && behaviors.Any())
            {
                await behaviors.First()
                    .Handle(context,
                        serviceProvider,
                        (nextContext, nextServiceProvider) =>
                            ExecutePipeline(
                                behaviors.Skip(1).ToList(),
                                nextContext,
                                nextServiceProvider,
                                finalAction));
            }
            else
            {
                await finalAction(context, serviceProvider);
            }
        }
    }

    /// <inheritdoc cref="Consumer" />
    public abstract class Consumer<TBroker, TEndpoint, TOffset> : Consumer
        where TBroker : IBroker
        where TEndpoint : IConsumerEndpoint
        where TOffset : IOffset
    {
        protected Consumer(
            TBroker broker,
            TEndpoint endpoint,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider,
            ILogger<Consumer<TBroker, TEndpoint, TOffset>> logger)
            : base(broker, endpoint, behaviors, serviceProvider, logger)
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

        public override Task Commit(IReadOnlyCollection<IOffset> offsets) => Commit(offsets.Cast<TOffset>().ToList());

        /// <summary>
        ///     <param>Confirms that the messages at the specified offsets have been successfully processed.</param>
        ///     <param>
        ///         The acknowledgement will be sent to the message broker and the message will never be processed again
        ///         (by the same logical consumer / consumer group).
        ///     </param>
        /// </summary>
        /// <param name="offsets">The offsets to be committed.</param>
        protected abstract Task Commit(IReadOnlyCollection<TOffset> offsets);

        public override Task Rollback(IReadOnlyCollection<IOffset> offsets) =>
            Rollback(offsets.Cast<TOffset>().ToList());

        /// <summary>
        ///     <param>Notifies that an error occured while processing the messages at the specified offsets.</param>
        ///     <param>
        ///         If necessary the information will be sent to the message broker to ensure that the message will be
        ///         re-processed.
        ///     </param>
        /// </summary>
        /// <param name="offsets">The offsets to be rolled back.</param>
        protected abstract Task Rollback(IReadOnlyCollection<TOffset> offsets);
    }
}