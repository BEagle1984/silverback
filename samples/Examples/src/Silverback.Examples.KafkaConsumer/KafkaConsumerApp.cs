// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Consumer;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.KafkaConsumer
{
    public class KafkaConsumerApp : ConsumerApp
    {
        protected override void ConfigureServices(IServiceCollection services) =>
            services
                .AddLogging()
                .AddSilverback()
                .AsObservable()
                .UseDbContext<ExamplesDbContext>()
                .WithConnectionToKafka(options => options
                    //.AddDbLoggedInboundConnector()
                    .AddDbOffsetStoredInboundConnector()
                    .AddInboundConnector()
                    .AddDbChunkStore())
                .AddCommonSubscribers()
                .AddScopedSubscriber<KafkaEventsSubscriber>()
                .AddScopedBehavior<LogHeadersBehavior>();

        protected override IBroker Configure(BusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator
                .Connect(endpoints => endpoints
                    .AddInbound(CreateConsumerEndpoint("silverback-examples-events",
                        "silverback-examples-events-chunked", "silverback-examples-events-sp"))
                    .AddInbound(CreateConsumerEndpoint("silverback-examples-multiple-groups", $"__group-1"))
                    .AddInbound(CreateConsumerEndpoint("silverback-examples-multiple-groups", $"__group-2"))
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
                                        BootstrapServers = "PLAINTEXT://localhost:9092"
                                    }
                                })
                                .MaxFailedAttempts(2),
                            policy
                                .Move(new KafkaProducerEndpoint("silverback-examples-events")
                                {
                                    Configuration = new KafkaProducerConfig
                                    {
                                        BootstrapServers = "PLAINTEXT://localhost:9092"
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
                                    Identifiers = messages
                                        .Select(x => ((IIntegrationMessage) x?.Message)?.Id ?? Guid.Empty).ToList(),
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

        private KafkaConsumerEndpoint CreateConsumerEndpoint(
            string name,
            IMessageSerializer messageSerializer = null) =>
            CreateConsumerEndpoint(new[] { name }, messageSerializer);

        private KafkaConsumerEndpoint CreateConsumerEndpoint(params string[] names) =>
            CreateConsumerEndpoint(names, null);

        private KafkaConsumerEndpoint CreateConsumerEndpoint(string name, string groupId) =>
            CreateConsumerEndpoint(new[] { name }, null, groupId);
        
        private KafkaConsumerEndpoint CreateConsumerEndpoint(
            string[] names,
            IMessageSerializer messageSerializer,
            string groupId = null)
        {
            var endpoint = new KafkaConsumerEndpoint(names)
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    GroupId = groupId ?? ConsumerGroupName,
                    AutoOffsetReset = AutoOffsetReset.Earliest
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