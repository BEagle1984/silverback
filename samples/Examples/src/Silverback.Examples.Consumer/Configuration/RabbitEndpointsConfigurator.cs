// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using RabbitMQ.Client;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;

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
            .AddInbound<LoggedInboundConnector>(
                new RabbitQueueConsumerEndpoint("silverback-examples-events-queue")
                {
                    Connection = GetConnectionConfig(),
                    Queue = new RabbitQueueConfig
                    {
                        IsDurable = true,
                        IsExclusive = false,
                        IsAutoDeleteEnabled = false
                    },
                    AcknowledgeEach = 2
                })
            .AddInbound<LoggedInboundConnector>(
                new RabbitExchangeConsumerEndpoint("silverback-examples-events-fanout")
                {
                    Connection = GetConnectionConfig(),
                    QueueName = $"{_app.ConsumerGroupName}.silverback-examples-events-fanout",
                    Queue = new RabbitQueueConfig
                    {
                        IsDurable = true,
                        IsExclusive = false,
                        IsAutoDeleteEnabled = false
                    },
                    Exchange = new RabbitExchangeConfig
                    {
                        IsDurable = true,
                        IsAutoDeleteEnabled = false,
                        ExchangeType = ExchangeType.Fanout
                    }
                })
            .AddInbound<LoggedInboundConnector>(
                new RabbitExchangeConsumerEndpoint("silverback-examples-events-topic")
                {
                    Connection = GetConnectionConfig(),
                    QueueName = $"{_app.ConsumerGroupName}.silverback-examples-events-topic",
                    RoutingKey = "interesting.*.event",
                    Queue = new RabbitQueueConfig
                    {
                        IsDurable = true,
                        IsExclusive = false,
                        IsAutoDeleteEnabled = false
                    },
                    Exchange = new RabbitExchangeConfig
                    {
                        IsDurable = true,
                        IsAutoDeleteEnabled = false,
                        ExchangeType = ExchangeType.Topic
                    }
                });

        private static RabbitConnectionConfig GetConnectionConfig() => new RabbitConnectionConfig
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest",
        };
    }
}
