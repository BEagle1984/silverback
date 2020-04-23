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
    /// <inheritdoc cref="IConsumer" />
    public abstract class Consumer : IConsumer, IDisposable
    {
        private readonly MessagesReceivedAsyncCallback _receivedCallback;

        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<Consumer> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Consumer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that is instantiating the consumer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to be consumed.
        /// </param>
        /// <param name="receivedCallback"> The delegate to be invoked when a message is received. </param>
        /// <param name="behaviors"> The behaviors to be added to the pipeline. </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger"> The <see cref="ILogger" />. </param>
        protected Consumer(
            IBroker broker,
            IConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback receivedCallback,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider,
            ILogger<Consumer> logger)
        {
            if (broker == null)
                throw new ArgumentNullException(nameof(broker));
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            Behaviors = behaviors ?? Array.Empty<IConsumerBehavior>();

            Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            _receivedCallback = receivedCallback ?? throw new ArgumentNullException(nameof(receivedCallback));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Endpoint.Validate();
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IConsumerBehavior> Behaviors { get; }

        /// <summary>
        ///     Gets the <see cref="IBroker" /> that owns this consumer.
        /// </summary>
        public IBroker Broker { get; }

        /// <summary>
        ///     Gets the <see cref="IConsumerEndpoint" /> representing the endpoint that is being consumed.
        /// </summary>
        public IConsumerEndpoint Endpoint { get; }

        /// <inheritdoc />
        public Task Commit(IOffset offset) => Commit(new[] { offset });

        /// <inheritdoc />
        public abstract Task Commit(IReadOnlyCollection<IOffset> offsets);

        /// <inheritdoc />
        public Task Rollback(IOffset offset) => Rollback(new[] { offset });

        /// <inheritdoc />
        public abstract Task Rollback(IReadOnlyCollection<IOffset> offsets);

        /// <inheritdoc />
        public abstract void Connect();

        /// <inheritdoc />
        public abstract void Disconnect();

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Handles the consumed message invoking each <see cref="IConsumerBehavior" /> in the pipeline and finally
        ///     invoking the callback method.
        /// </summary>
        /// <param name="message"> The body of the consumed message. </param>
        /// <param name="headers"> The headers of the consumed message. </param>
        /// <param name="sourceEndpointName">
        ///     The name of the actual endpoint (topic) where the message has been delivered.
        /// </param>
        /// <param name="offset"> The offset of the consumed message. </param>a
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected virtual async Task HandleMessage(
            byte[] message,
            IReadOnlyCollection<MessageHeader> headers,
            string sourceEndpointName,
            IOffset offset) =>
            await ExecutePipeline(
                Behaviors,
                new ConsumerPipelineContext(
                    new[]
                    {
                        new RawInboundEnvelope(message, headers, Endpoint, sourceEndpointName, offset)
                    },
                    this),
                _serviceProvider);

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        ///     resources.
        /// </summary>
        /// <param name="disposing">
        ///     A value indicating whether the method has been called by the <c> Dispose </c> method and not
        ///     from the finalizer.
        /// </param>
        [SuppressMessage("ReSharper", "CA1031", Justification = Justifications.ExceptionLogged)]
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            try
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error occurred while disposing consumer from endpoint {endpoint}.",
                    Endpoint.Name);
            }
        }

        private async Task ExecutePipeline(
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider)
        {
            if (behaviors != null && behaviors.Any())
            {
                await behaviors.First()
                    .Handle(
                        context,
                        serviceProvider,
                        (nextContext, nextServiceProvider) =>
                            ExecutePipeline(
                                behaviors.Skip(1).ToList(),
                                nextContext,
                                nextServiceProvider));
            }
            else
            {
                await _receivedCallback.Invoke(
                    new MessagesReceivedCallbackArgs(context.Envelopes, serviceProvider, this));
            }
        }
    }
}
