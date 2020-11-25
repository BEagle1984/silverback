// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
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

        private readonly ISilverbackIntegrationLogger<RabbitConsumer> _logger;

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
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}"/>.
        /// </param>
        /// <param name="connectionFactory">
        ///     The <see cref="IRabbitConnectionFactory" /> to be used to create the channels to connect to the
        ///     endpoint.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        public RabbitConsumer(
            RabbitBroker broker,
            RabbitConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IRabbitConnectionFactory connectionFactory,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<RabbitConsumer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <inheritdoc cref="Consumer.ConnectCoreAsync" />
        protected override Task ConnectCoreAsync()
        {
            if (_consumer != null)
                return Task.CompletedTask;

            (_channel, _queueName) = _connectionFactory.GetChannel(Endpoint);

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.Received += TryHandleMessageAsync;

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.DisconnectCoreAsync" />
        protected override Task DisconnectCoreAsync()
        {
            if (_consumer == null)
                return Task.CompletedTask;

            CommitPendingOffset();

            _channel?.Dispose();
            _channel = null;
            _queueName = null;
            _consumer = null;

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.StopCore" />
        protected override void StartCore()
        {
            _consumerTag = _channel.BasicConsume(
                _queueName,
                false,
                _consumer);
        }

        /// <inheritdoc cref="Consumer.StopCore" />
        protected override void StopCore()
        {
            if (_consumer == null)
                return;

            _disconnecting = true;

            if (_consumerTag != null)
            {
                _channel?.BasicCancel(_consumerTag);
                _consumerTag = null;
            }

            _disconnecting = false;
        }

        /// <inheritdoc cref="Consumer.WaitUntilConsumingStoppedAsync" />
        protected override Task WaitUntilConsumingStoppedAsync(CancellationToken cancellationToken)
        {
            // TODO: How to handle this?
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}.CommitCoreAsync" />
        protected override Task CommitCoreAsync(IReadOnlyCollection<RabbitOffset> offsets)
        {
            CommitOrStoreOffset(offsets.OrderBy(offset => offset.DeliveryTag).Last());
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}.RollbackCoreAsync" />
        protected override Task RollbackCoreAsync(IReadOnlyCollection<RabbitOffset> offsets)
        {
            BasicNack(offsets.Max(offset => offset.DeliveryTag));
            return Task.CompletedTask;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task TryHandleMessageAsync(object sender, BasicDeliverEventArgs deliverEventArgs)
        {
            try
            {
                var offset = new RabbitOffset(deliverEventArgs.ConsumerTag, deliverEventArgs.DeliveryTag);

                Dictionary<string, string> logData = new Dictionary<string, string>
                {
                    ["deliveryTag"] = $"{offset.DeliveryTag.ToString(CultureInfo.InvariantCulture)}",
                    ["routingKey"] = deliverEventArgs.RoutingKey
                };

                _logger.LogDebug(
                    RabbitEventIds.ConsumingMessage,
                    "Consuming message {offset} from endpoint {endpointName}.",
                    offset.Value,
                    Endpoint.Name);

                // TODO: Test this!
                if (_disconnecting)
                    return;

                await HandleMessageAsync(
                        deliverEventArgs.Body.ToArray(),
                        deliverEventArgs.BasicProperties.Headers.ToSilverbackHeaders(),
                        Endpoint.Name,
                        offset,
                        logData)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // TODO: Prevent duplicate log (FatalExceptionLoggerConsumerBehavior)
                _logger.LogCritical(
                    IntegrationEventIds.ConsumerFatalError,
                    ex,
                    "Fatal error occurred processing the consumed message. The consumer will be stopped.");

                await DisconnectAsync().ConfigureAwait(false);
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
                    RabbitEventIds.Commit,
                    "Successfully committed (basic.ack) the delivery tag {deliveryTag}.",
                    deliveryTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    RabbitEventIds.CommitError,
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
                    RabbitEventIds.Rollback,
                    "Successfully rolled back (basic.nack) the delivery tag {deliveryTag}.",
                    deliveryTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    RabbitEventIds.RollbackError,
                    ex,
                    "Error occurred rolling back (basic.nack) the delivery tag {deliveryTag}.",
                    deliveryTag);

                throw;
            }
        }
    }
}
