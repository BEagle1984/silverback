// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class RabbitConsumer : Consumer<RabbitBroker, RabbitConsumerEndpoint, RabbitOffset>
    {
        private readonly IModel _channel;
        private readonly string _queueName;
        private readonly ILogger<RabbitConsumer> _logger;

        private AsyncEventingBasicConsumer _consumer;
        private string _consumerTag;

        public RabbitConsumer(
            RabbitBroker broker,
            RabbitConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors,
            IRabbitConnectionFactory connectionFactory,
            ILogger<RabbitConsumer> logger)
            : base(broker, endpoint, behaviors)

        {
            _logger = logger;

            (_channel, _queueName) = connectionFactory.GetChannel(endpoint);
        }

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}" />
        protected override Task Commit(IEnumerable<RabbitOffset> offsets)
        {
            BasicAck(offsets.Max(offset => offset.DeliveryTag));
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}" />
        protected override Task Rollback(IEnumerable<RabbitOffset> offsets)
        {
            BasicNack(offsets.Max(offset => offset.DeliveryTag));
            return Task.CompletedTask;
        }

        private void BasicAck(ulong deliveryTag)
        {
            try
            {
                _channel.BasicAck(deliveryTag, true);

                _logger.LogDebug("Successfully committed (basic.ack) the delivery tag {deliveryTag}.",
                    deliveryTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(
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
                _channel.BasicNack(deliveryTag, true, true);

                _logger.LogDebug("Successfully rolled back (basic.nack) the delivery tag {deliveryTag}.",
                    deliveryTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred rolled back (basic.nack) the delivery tag {deliveryTag}.",
                    deliveryTag);

                throw;
            }
        }

        /// <inheritdoc cref="Consumer" />
        public override void Connect()
        {
            if (_consumer != null)
                return;

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.Received += TryHandleMessage;

            _consumerTag = _channel.BasicConsume(queue: _queueName,
                autoAck: false,
                consumer: _consumer);
        }

        private async Task TryHandleMessage(object sender, BasicDeliverEventArgs deliverEventArgs)
        {
            RabbitOffset offset = null;

            try
            {
                offset = new RabbitOffset(deliverEventArgs.ConsumerTag, deliverEventArgs.DeliveryTag);

                _logger.LogDebug("Consuming message {offset} from endpoint {endpointName}.",
                    offset.Value, Endpoint.Name);

                await HandleMessage(
                    deliverEventArgs.Body,
                    deliverEventArgs.BasicProperties.Headers.ToSilverbackHeaders(),
                    offset);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Fatal error occurred consuming the message {offset} from endpoint {endpointName}. " +
                    "The consumer will be stopped.",
                    offset?.Value, Endpoint.Name);

                Disconnect();
            }
        }

        /// <inheritdoc cref="Consumer" />
        public override void Disconnect()
        {
            if (_consumer == null)
                return;

            _channel.BasicCancel(_consumerTag);

            _consumerTag = null;
            _consumer = null;
        }
    }
}