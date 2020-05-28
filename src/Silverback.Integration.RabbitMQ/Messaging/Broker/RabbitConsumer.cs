// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}" />
    public class RabbitConsumer : Consumer<RabbitBroker, RabbitConsumerEndpoint, RabbitOffset>
    {
        private readonly IRabbitConnectionFactory _connectionFactory;

        private readonly ILogger<RabbitConsumer> _logger;

        private readonly object _pendingOffsetLock = new object();

        private IModel? _channel;

        private string? _queueName;

        private AsyncEventingBasicConsumer? _consumer;

        private string? _consumerTag;

        private bool _disconnecting;

        private int _pendingOffsetsCount;

        private RabbitOffset? _pendingOffset;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitConsumer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that is instantiating the consumer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to be consumed.
        /// </param>
        /// <param name="callback">
        ///     The delegate to be invoked when a message is received.
        /// </param>
        /// <param name="behaviors">
        ///     The behaviors to be added to the pipeline.
        /// </param>
        /// <param name="connectionFactory">
        ///     The <see cref="IRabbitConnectionFactory" /> to be used to create the channels to connect to the
        ///     endpoint.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger" />.
        /// </param>
        public RabbitConsumer(
            RabbitBroker broker,
            RabbitConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior>? behaviors,
            IRabbitConnectionFactory connectionFactory,
            IServiceProvider serviceProvider,
            ILogger<RabbitConsumer> logger)
            : base(broker, endpoint, callback, behaviors, serviceProvider, logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public override void Connect()
        {
            if (_consumer != null)
                return;

            (_channel, _queueName) = _connectionFactory.GetChannel(Endpoint);

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.Received += TryHandleMessage;

            _consumerTag = _channel.BasicConsume(
                _queueName,
                false,
                _consumer);
        }

        /// <inheritdoc />
        public override void Disconnect()
        {
            if (_consumer == null)
                return;

            _disconnecting = true;

            CommitPendingOffset();

            _channel?.BasicCancel(_consumerTag);
            _channel?.Dispose();
            _channel = null;
            _queueName = null;
            _consumerTag = null;
            _consumer = null;

            _disconnecting = false;
        }

        /// <inheritdoc />
        protected override Task CommitCore(IReadOnlyCollection<RabbitOffset> offsets)
        {
            CommitOrStoreOffset(offsets.OrderBy(offset => offset.DeliveryTag).Last());
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task RollbackCore(IReadOnlyCollection<RabbitOffset> offsets)
        {
            BasicNack(offsets.Max(offset => offset.DeliveryTag));
            return Task.CompletedTask;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task TryHandleMessage(object sender, BasicDeliverEventArgs deliverEventArgs)
        {
            RabbitOffset? offset = null;

            try
            {
                offset = new RabbitOffset(deliverEventArgs.ConsumerTag, deliverEventArgs.DeliveryTag);

                _logger.LogDebug(
                    EventIds.RabbitConsumerConsumingMessage,
                    "Consuming message {offset} from endpoint {endpointName}.",
                    offset.Value,
                    Endpoint.Name);

                if (_disconnecting)
                    return;

                await HandleMessage(
                    deliverEventArgs.Body.ToArray(),
                    deliverEventArgs.BasicProperties.Headers.ToSilverbackHeaders(),
                    Endpoint.Name,
                    offset);
            }
            catch (Exception ex)
            {
                const string errorMessage =
                    "Fatal error occurred consuming the message {offset} from endpoint {endpointName}. " +
                    "The consumer will be stopped.";
                _logger.LogCritical(EventIds.RabbitConsumerFatalError, ex, errorMessage, offset?.Value, Endpoint.Name);

                Disconnect();
            }
        }

        private void CommitOrStoreOffset(RabbitOffset offset)
        {
            lock (_pendingOffsetLock)
            {
                if (Endpoint.AcknowledgeEach == 1)
                {
                    BasicAck(offset.DeliveryTag);
                    return;
                }

                _pendingOffset = offset;
                _pendingOffsetsCount++;
            }

            if (Endpoint.AcknowledgeEach <= _pendingOffsetsCount)
                CommitPendingOffset();
        }

        private void CommitPendingOffset()
        {
            lock (_pendingOffsetLock)
            {
                if (_pendingOffset == null)
                    return;

                BasicAck(_pendingOffset.DeliveryTag);
                _pendingOffset = null;
                _pendingOffsetsCount = 0;
            }
        }

        private void BasicAck(ulong deliveryTag)
        {
            try
            {
                if (_channel == null)
                    throw new InvalidOperationException("The consumer is not connected.");

                _channel.BasicAck(deliveryTag, true);

                _logger.LogDebug(
                    EventIds.RabbitConsumerSuccessfullyCommited,
                    "Successfully committed (basic.ack) the delivery tag {deliveryTag}.",
                    deliveryTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    EventIds.RabbitConsumerErrorWhileCommitting,
                    ex,
                    "Error occurred committing (basic.ack) the delivery tag {deliveryTag}.",
                    deliveryTag);

                throw;
            }
        }

        private void BasicNack(ulong deliveryTag)
        {
            try
            {
                if (_channel == null)
                    throw new InvalidOperationException("The consumer is not connected.");

                _channel.BasicNack(deliveryTag, true, true);

                _logger.LogDebug(
                    EventIds.RabbitConsumerSuccessfullySentBasicNack,
                    "Successfully rolled back (basic.nack) the delivery tag {deliveryTag}.",
                    deliveryTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    EventIds.RabbitConsumerErrorWhileSendingBasicNack,
                    ex,
                    "Error occurred rolled back (basic.nack) the delivery tag {deliveryTag}.",
                    deliveryTag);

                throw;
            }
        }
    }
}
