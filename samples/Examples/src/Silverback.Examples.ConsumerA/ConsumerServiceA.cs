// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Consumer;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.ConsumerA
{
    public class ConsumerServiceA : ConsumerService
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddLogging();

            services
                .AddSilverback()
                .AsObservable()
                .UseDbContext<ExamplesDbContext>()
                .WithConnectionTo<KafkaBroker>(options => options
                    //.AddDbLoggedInboundConnector()
                    .AddDbOffsetStoredInboundConnector()
                    .AddInboundConnector()
                    .AddDbChunkStore())
                .AddScopedSubscriber<SubscriberService>()
                .AddScopedBehavior<LogHeadersBehavior>();
        }

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider)
        {
            Configuration.SetupSerilog();

            var broker = configurator
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
                                        BootstrapServers = "PLAINTEXT://localhost:9092",
                                        ClientId = "consumer-service-a"
                                    }
                                })
                                .MaxFailedAttempts(2),
                            policy
                                .Move(new KafkaProducerEndpoint("silverback-examples-events")
                                {
                                    Configuration = new KafkaProducerConfig
                                    {
                                        BootstrapServers = "PLAINTEXT://localhost:9092",
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
                                .Publish(messages => new MessageMovedEvent
                                {
                                    Identifiers = messages.Select(x => ((IIntegrationMessage)x?.Content)?.Id ?? Guid.Empty).ToList(),
                                    Source = messages.First().Endpoint.Name,
                                    Destination = "silverback-examples-events"
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
                broker.Disconnect();
            };
        }

        private static KafkaConsumerEndpoint CreateConsumerEndpoint(string name, IMessageSerializer messageSerializer = null)
        {
            var endpoint = new KafkaConsumerEndpoint(name)
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
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
    }
}