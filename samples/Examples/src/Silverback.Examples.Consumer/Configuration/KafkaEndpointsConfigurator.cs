// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Messages;
using Silverback.Examples.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Encryption;
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
            .AddInbound(new KafkaConsumerEndpoint(
                "silverback-examples-events",
                "silverback-examples-events-chunked",
                "silverback-examples-events-sp",
                "silverback-examples-events-2",
                "silverback-examples-events-3",
                "silverback-examples-binaries")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    GroupId = _app.ConsumerGroupName,
                    AutoOffsetReset = AutoOffsetReset.Earliest
                }
            })
            .AddInbound(new KafkaConsumerEndpoint("silverback-examples-multiple-groups")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    GroupId = "__group-1",
                    AutoOffsetReset = AutoOffsetReset.Earliest
                }
            })
            .AddInbound(new KafkaConsumerEndpoint("silverback-examples-multiple-groups")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    GroupId = "__group-2",
                    AutoOffsetReset = AutoOffsetReset.Earliest
                }
            })
            .AddInbound(new KafkaConsumerEndpoint("silverback-examples-batch")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = _app.ConsumerGroupName,
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    }
                },
                settings: new InboundConnectorSettings
                {
                    Batch = new Messaging.Batch.BatchSettings
                    {
                        Size = 5,
                        MaxWaitTime = TimeSpan.FromSeconds(5)
                    },
                    Consumers = 2
                })
            .AddInbound(new KafkaConsumerEndpoint("silverback-examples-error-events")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    GroupId = _app.ConsumerGroupName,
                    AutoOffsetReset = AutoOffsetReset.Earliest
                }
            }, policy => policy
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
                                Content = $"Transformed BadEvent (exception: {ex.Message})"
                            },
                            (headers, ex) =>
                            {
                                headers.Add("exception-message", ex.Message);
                                return headers;
                            })
                        .Publish(messages => new MessageMovedEvent
                        {
                            Source = messages.First().Endpoint.Name,
                            Destination = "silverback-examples-events"
                        })))
            .AddInbound(new KafkaConsumerEndpoint("silverback-examples-custom-serializer")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    GroupId = _app.ConsumerGroupName,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                },
                Serializer = GetCustomSerializer()
            })
            .AddInbound(new KafkaConsumerEndpoint("silverback-examples-custom-serializer")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    GroupId = _app.ConsumerGroupName,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                },
                Serializer = GetCustomSerializer()
            })
            .AddInbound(new KafkaConsumerEndpoint("silverback-examples-avro")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    GroupId = _app.ConsumerGroupName,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                },
                Serializer = GetAvroSerializer()
            })
            .AddInbound(new KafkaConsumerEndpoint("silverback-examples-encrypted")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    GroupId = _app.ConsumerGroupName,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                },
                Encryption = new SymmetricEncryptionSettings
                {
                    Key = Constants.AesEncryptionKey
                }
            })
            .AddInbound(new KafkaConsumerEndpoint("silverback-examples-avro-encrypted")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    GroupId = _app.ConsumerGroupName,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                },
                Serializer = GetAvroSerializer(),
                Encryption = new SymmetricEncryptionSettings
                {
                    Key = Constants.AesEncryptionKey
                }
            })
            // Special inbound (not logged)
            .AddInbound<InboundConnector>(new KafkaConsumerEndpoint("silverback-examples-legacy-messages")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    GroupId = _app.ConsumerGroupName,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                },
                Serializer = new JsonMessageSerializer<LegacyMessage>()
                {
                    Encoding = MessageEncoding.ASCII
                }
            });

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