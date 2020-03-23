// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using RabbitMQ.Client;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Consumer.Configuration
{
    public class RabbitEndpointsConfigurator : IEndpointsConfigurator
    {
        private readonly ConsumerApp _app;

        public RabbitEndpointsConfigurator(ConsumerApp app)
        {
            _app = app;
        }

        public void Configure(IEndpointsConfigurationBuilder builder) => builder
            .AddInbound(
                CreateQueueEndpoint("silverback-examples-events-queue"),
                typeof(LoggedInboundConnector))
            .AddInbound(
                CreateExchangeEndpoint("silverback-examples-events-fanout", ExchangeType.Fanout),
                typeof(LoggedInboundConnector))
            .AddInbound(
                CreateExchangeEndpoint("silverback-examples-events-topic",
                    ExchangeType.Topic,
                    routingKey: "interesting.*.event"),
                typeof(LoggedInboundConnector));

        private RabbitQueueConsumerEndpoint CreateQueueEndpoint(
            string name,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            IMessageSerializer messageSerializer = null)
        {
            var endpoint = new RabbitQueueConsumerEndpoint(name)
            {
                Connection = GetConnectionConfig(),
                Queue = new RabbitQueueConfig
                {
                    IsDurable = durable,
                    IsExclusive = exclusive,
                    IsAutoDeleteEnabled = autoDelete
                },
                AcknowledgeEach = 2
            };

            if (messageSerializer != null)
                endpoint.Serializer = messageSerializer;

            return endpoint;
        }

        private IConsumerEndpoint CreateExchangeEndpoint(
            string name,
            string exchangeType,
            bool durable = true,
            bool autoDelete = false,
            string routingKey = null,
            IMessageSerializer messageSerializer = null)
        {
            var endpoint = new RabbitExchangeConsumerEndpoint(name)
            {
                Connection = GetConnectionConfig(),
                QueueName = $"{_app.ConsumerGroupName}.{name}",
                RoutingKey = routingKey,
                Queue = new RabbitQueueConfig
                {
                    IsDurable = durable,
                    IsExclusive = false,
                    IsAutoDeleteEnabled = autoDelete
                },
                Exchange = new RabbitExchangeConfig
                {
                    IsDurable = durable,
                    IsAutoDeleteEnabled = autoDelete,
                    ExchangeType = exchangeType
                }
            };

            if (messageSerializer != null)
                endpoint.Serializer = messageSerializer;

            return endpoint;
        }

        private static RabbitConnectionConfig GetConnectionConfig() => new RabbitConnectionConfig
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest",
        };
    }
}