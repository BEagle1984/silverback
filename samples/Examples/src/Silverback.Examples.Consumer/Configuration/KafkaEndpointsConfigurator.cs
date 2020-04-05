// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Silverback.Examples.Common.Messages;
using Silverback.Examples.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Consumer.Configuration
{
    public class KafkaEndpointsConfigurator : IEndpointsConfigurator
    {
        private readonly ConsumerApp _app;

        public KafkaEndpointsConfigurator(ConsumerApp app)
        {
            _app = app;
        }

        public void Configure(IEndpointsConfigurationBuilder builder) => builder
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
                            Identifiers = messages.OfType<IInboundEnvelope<IIntegrationMessage>>()
                                .Select(x => x.Message?.Id ?? Guid.Empty).ToList(),
                            Source = messages.First().Endpoint.Name,
                            Destination = "silverback-examples-events"
                        })))
            .AddInbound(CreateConsumerEndpoint("silverback-examples-custom-serializer",
                GetCustomSerializer()))
            .AddInbound(CreateConsumerEndpoint("silverback-examples-avro",
                GetAvroSerializer()))
            // Special inbound (not logged)
            .AddInbound<InboundConnector>(CreateConsumerEndpoint("silverback-examples-legacy-messages",
                new JsonMessageSerializer<LegacyMessage>()
                {
                    Encoding = MessageEncoding.ASCII
                }));

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
                    GroupId = groupId ?? _app.ConsumerGroupName,
                    AutoOffsetReset = AutoOffsetReset.Earliest
                }
            };

            if (messageSerializer != null)
                endpoint.Serializer = messageSerializer;

            return endpoint;
        }

        private static IMessageSerializer GetCustomSerializer() =>
            new JsonMessageSerializer
            {
                Encoding = MessageEncoding.Unicode
            };

        private static IMessageSerializer GetAvroSerializer() =>
            new AvroMessageSerializer<AvroMessage>
            {
                SchemaRegistryConfig = new SchemaRegistryConfig
                {
                    Url = "localhost:8081"
                }
            };
    }
}