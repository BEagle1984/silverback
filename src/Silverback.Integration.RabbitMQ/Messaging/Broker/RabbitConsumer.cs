// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Rabbit;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}" />
    public class RabbitConsumer : Consumer<RabbitBroker, RabbitConsumerEndpoint, RabbitDeliveryTag>
    {
        private readonly IRabbitConnectionFactory _connectionFactory;

        private readonly IInboundLogger<RabbitConsumer> _logger;

        private readonly object _pendingOffsetLock = new();

        private IModel? _channel;

        private string? _queueName;

        private AsyncEventingBasicConsumer? _consumer;

        private string? _consumerTag;

        private bool _disconnecting;

        private int _pendingOffsetsCount;

        private RabbitDeliveryTag? _pendingDeliveryTag;

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
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="connectionFactory">
        ///     The <see cref="IRabbitConnectionFactory" /> to be used to create the channels to connect to the
        ///     endpoint.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="IInboundLogger{TCategoryName}" />.
        /// </param>
        public RabbitConsumer(
            RabbitBroker broker,
            RabbitConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IRabbitConnectionFactory connectionFactory,
            IServiceProvider serviceProvider,
            IInboundLogger<RabbitConsumer> logger)
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

        /// <inheritdoc cref="Consumer.StopCoreAsync" />
        protected override Task StartCoreAsync()
        {
            _consumerTag = _channel.BasicConsume(
                _queueName,
                false,
                _consumer);

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.StopCoreAsync" />
        protected override Task StopCoreAsync()
        {
            if (_consumer == null)
                return Task.CompletedTask;

            _disconnecting = true;

            if (_consumerTag != null)
            {
                _channel?.BasicCancel(_consumerTag);
                _consumerTag = null;
            }

            _disconnecting = false;

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.WaitUntilConsumingStoppedAsync" />
        protected override Task WaitUntilConsumingStoppedAsync()
        {
            // TODO: How to handle this?
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.CommitCoreAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
        protected override Task CommitCoreAsync(
            IReadOnlyCollection<RabbitDeliveryTag> brokerMessageIdentifiers)
        {
            CommitOrStoreDeliveryTag(
                brokerMessageIdentifiers.OrderBy(deliveryTag => deliveryTag.DeliveryTag).Last());
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.RollbackCoreAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
        protected override Task RollbackCoreAsync(
            IReadOnlyCollection<RabbitDeliveryTag> brokerMessageIdentifiers)
        {
            BasicNack(brokerMessageIdentifiers.Max(deliveryTag => deliveryTag.DeliveryTag));
            return Task.CompletedTask;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task TryHandleMessageAsync(object sender, BasicDeliverEventArgs deliverEventArgs)
        {
            try
            {
                var deliveryTag = new RabbitDeliveryTag(
                    deliverEventArgs.ConsumerTag,
                    deliverEventArgs.DeliveryTag);
                var headers = new MessageHeaderCollection(
                    deliverEventArgs.BasicProperties.Headers.ToSilverbackHeaders());

                headers.AddIfNotExists(DefaultMessageHeaders.MessageId, deliveryTag.Value);

                _logger.LogConsuming(deliveryTag.Value, this);

                // TODO: Test this!
                if (_disconnecting)
                    return;

                await HandleMessageAsync(
                        deliverEventArgs.Body.ToArray(),
                        headers,
                        Endpoint.Name,
                        deliveryTag)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is not ConsumerPipelineFatalException)
                    _logger.LogConsumerFatalError(this, ex);

                await DisconnectAsync().ConfigureAwait(false);
            }
        }

        private void CommitOrStoreDeliveryTag(RabbitDeliveryTag deliveryTag)
        {
            lock (_pendingOffsetLock)
            {
                if (Endpoint.AcknowledgeEach == 1)
                {
                    BasicAck(deliveryTag.DeliveryTag);
                    return;
                }

                _pendingDeliveryTag = deliveryTag;
                _pendingOffsetsCount++;
            }

            if (Endpoint.AcknowledgeEach <= _pendingOffsetsCount)
                CommitPendingOffset();
        }

        private void CommitPendingOffset()
        {
            lock (_pendingOffsetLock)
            {
                if (_pendingDeliveryTag == null)
                    return;

                BasicAck(_pendingDeliveryTag.DeliveryTag);
                _pendingDeliveryTag = null;
                _pendingOffsetsCount = 0;
            }
        }

        private void BasicAck(ulong deliveryTag)
        {
            _channel!.BasicAck(deliveryTag, true);
            _logger.LogCommit(deliveryTag.ToString(CultureInfo.InvariantCulture), this);
        }

        private void BasicNack(ulong deliveryTag)
        {
            _channel!.BasicNack(deliveryTag, true, true);
            _logger.LogRollback(deliveryTag.ToString(CultureInfo.InvariantCulture), this);
        }
    }
}
