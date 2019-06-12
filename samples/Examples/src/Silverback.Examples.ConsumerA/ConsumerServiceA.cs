// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Silverback.Examples.Common.Consumer;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.ConsumerA
{
    public class ConsumerServiceA : ConsumerService
    {
        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus(options => options.Observable())
            .AddBroker<KafkaBroker>(options => options
                //.AddDbLoggedInboundConnector<ExamplesDbContext>()
                .AddDbOffsetStoredInboundConnector<ExamplesDbContext>()
                .AddInboundConnector()
                .AddDbChunkStore<ExamplesDbContext>())
            .AddScoped<ISubscriber, SubscriberService>()
            .AddScoped<IBehavior, LogHeadersBehavior>();

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider)
        {
            ConfigureNLog(serviceProvider);

            configurator
                .Connect(endpoints => endpoints
                    .AddInbound(CreateConsumerEndpoint("silverback-examples-events"))
                    .AddInbound(CreateConsumerEndpoint("silverback-examples-batch"),
                        settings: new InboundConnectorSettings
                        {
                            Batch = new Messaging.Batch.BatchSettings
                            {
                                Size = 5,
                                MaxWaitTime = TimeSpan.FromSeconds(5)
                            },
                            Consumers = 2
                        })
                    .AddInbound(CreateConsumerEndpoint("silverback-examples-error-events"), policy => policy
                        .Chain(
                            policy
                                .Retry(TimeSpan.FromMilliseconds(500))
                                .MaxFailedAttempts(2),
                            policy
                                .Move(new KafkaProducerEndpoint("silverback-examples-error-events")
                                {
                                    Configuration = new KafkaProducerConfig
                                    {
                                        BootstrapServers = "PLAINTEXT://kafka:9092",
                                        ClientId = "consumer-service-a"
                                    }
                                })
                                .MaxFailedAttempts(4),
                            policy
                                .Move(new KafkaProducerEndpoint("silverback-examples-events")
                                {
                                    Configuration = new KafkaProducerConfig
                                    {
                                        BootstrapServers = "PLAINTEXT://kafka:9092",
                                        ClientId = "consumer-service-a"
                                    }
                                })
                                .Transform(
                                    (msg, ex) => new IntegrationEventA
                                    {
                                        Id = Guid.NewGuid(),
                                        Content = $"Transformed BadEvent (exception: {ex.Message})"
                                    },
                                    (headers, ex) =>
                                    {
                                        headers.Add("exception-message", ex.Message);
                                        return headers;
                                    })
                                .Publish(msg => new MessageMovedEvent
                                {
                                    Id = (msg.Message as IntegrationEvent)?.Id ?? Guid.Empty,
                                    Destination = msg.Endpoint.Name
                                })))
                    .AddInbound(CreateConsumerEndpoint("silverback-examples-custom-serializer",
                        GetCustomSerializer()))
                    // Special inbound (not logged)
                    .AddInbound<InboundConnector>(CreateConsumerEndpoint("silverback-examples-legacy-messages",
                        new JsonMessageSerializer<LegacyMessage>()
                        {
                            Encoding = MessageEncoding.ASCII
                        })));

            Console.CancelKeyPress += (_, __) =>
            {
                serviceProvider.GetService<IBroker>().Disconnect();
            };
        }

        private static KafkaConsumerEndpoint CreateConsumerEndpoint(string name, IMessageSerializer messageSerializer = null)
        {
            var endpoint = new KafkaConsumerEndpoint(name)
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092",
                    ClientId = "consumer-service-a",
                    GroupId = "silverback-examples"
                }
            };

            if (messageSerializer != null)
                endpoint.Serializer = messageSerializer;

            return endpoint;
        }

        private static JsonMessageSerializer GetCustomSerializer()
        {
            var serializer = new JsonMessageSerializer
            {
                Encoding = MessageEncoding.Unicode
            };

            return serializer;
        }

        private static void ConfigureNLog(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<ILoggerFactory>()
                .AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            NLog.LogManager.LoadConfiguration("nlog.config");
        }
    }
}