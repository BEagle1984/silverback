// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public class OutboundQueueProducer : Producer<TransactionalOutboxBroker, IProducerEndpoint>
    {
        private readonly IOutboxWriter _queueWriter;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundQueueProducer" /> class.
        /// </summary>
        /// <param name="queueWriter">
        ///     The <see cref="IOutboxWriter" /> to be used to write to the queue.
        /// </param>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="IOutboundLogger{TCategoryName}" />.
        /// </param>
        public OutboundQueueProducer(
            IOutboxWriter queueWriter,
            TransactionalOutboxBroker broker,
            IProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            IOutboundLogger<Producer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            _queueWriter = queueWriter;
        }

        /// <inheritdoc cref="Producer.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},string)" />
        protected override IBrokerMessageIdentifier ProduceCore(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName) =>
            throw new InvalidOperationException("Only asynchronous operations are supported.");

        /// <inheritdoc cref="Producer.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},string)" />
        protected override IBrokerMessageIdentifier ProduceCore(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName) =>
            throw new InvalidOperationException("Only asynchronous operations are supported.");

        /// <inheritdoc cref="Producer.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},string,Action,Action{Exception})" />
        protected override void ProduceCore(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action onSuccess,
            Action<Exception> onError) =>
            throw new InvalidOperationException("Only asynchronous operations are supported.");

        /// <inheritdoc cref="Producer.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},string,Action,Action{Exception})" />
        protected override void ProduceCore(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action onSuccess,
            Action<Exception> onError) =>
            throw new InvalidOperationException("Only asynchronous operations are supported.");

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},string)" />
        protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName) =>
            await ProduceCoreAsync(
                    message,
                    await messageStream.ReadAllAsync().ConfigureAwait(false),
                    headers,
                    actualEndpointName)
                .ConfigureAwait(false);

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},string)" />
        protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName)
        {
            await _queueWriter.WriteAsync(
                    message,
                    messageBytes,
                    headers,
                    Endpoint.Name,
                    actualEndpointName)
                .ConfigureAwait(false);

            return null;
        }

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},string,Action,Action{Exception})" />
        protected override async Task ProduceCoreAsync(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action onSuccess,
            Action<Exception> onError) =>
            await ProduceCoreAsync(
                    message,
                    await messageStream.ReadAllAsync().ConfigureAwait(false),
                    headers,
                    actualEndpointName,
                    onSuccess,
                    onError)
                .ConfigureAwait(false);

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},string,Action,Action{Exception})" />
        protected override async Task ProduceCoreAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action onSuccess,
            Action<Exception> onError)
        {
            Check.NotNull(onSuccess, nameof(onSuccess));
            Check.NotNull(onError, nameof(onError));

            await _queueWriter.WriteAsync(
                    message,
                    messageBytes,
                    headers,
                    Endpoint.Name,
                    actualEndpointName)
                .ConfigureAwait(false);

            onSuccess.Invoke();
        }
    }
}
