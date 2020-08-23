// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;

namespace Silverback.Examples.Consumer.Configuration
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Called by Silverback")]
    public class RabbitEndpointsConfigurator : IEndpointsConfigurator
    {
        private readonly string _consumerGroupName;

        public RabbitEndpointsConfigurator(IOptions<ConsumerGroupConfiguration> options)
        {
            _consumerGroupName = options.Value.ConsumerGroupName;
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
                    QueueName = $"{_consumerGroupName}.silverback-examples-events-fanout",
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
                    QueueName = $"{_consumerGroupName}.silverback-examples-events-topic",
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
