// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.Options;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Messages;
using Silverback.Examples.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Consumer.Configuration
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Called by Silverback")]
    public class KafkaEndpointsConfigurator : IEndpointsConfigurator
    {
        private readonly string _consumerGroupName;

        public KafkaEndpointsConfigurator(IOptions<ConsumerGroupConfiguration> options)
        {
            _consumerGroupName = options.Value.ConsumerGroupName;
        }

        public void Configure(IEndpointsConfigurationBuilder builder) => builder
            .AddInbound(
                new KafkaConsumerEndpoint(
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
                        GroupId = $"{_consumerGroupName}__main",
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    }
                })
            .AddInbound(
                new KafkaConsumerEndpoint("silverback-examples-multiple-groups")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = $"{_consumerGroupName}__group-1",
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    }
                })
            .AddInbound(
                new KafkaConsumerEndpoint("silverback-examples-multiple-groups")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = $"{_consumerGroupName}__group-2",
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    }
                })
            .AddInbound(
                new KafkaConsumerEndpoint("silverback-examples-batch")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = $"{_consumerGroupName}__batch",
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    }
                },
                settings: new InboundConnectorSettings
                {
                    Batch = new BatchSettings
                    {
                        Size = 5,
                        MaxWaitTime = TimeSpan.FromSeconds(5)
                    },
                    Consumers = 2
                })
            .AddInbound(
                new KafkaConsumerEndpoint("silverback-examples-error-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = $"{_consumerGroupName}__error",
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    }
                },
                policy => policy.Chain(
                    policy
                        .Retry(TimeSpan.FromMilliseconds(500))
                        .MaxFailedAttempts(2),
                    policy
                        .Move(
                            new KafkaProducerEndpoint("silverback-examples-error-events")
                            {
                                Configuration = new KafkaProducerConfig
                                {
                                    BootstrapServers = "PLAINTEXT://localhost:9092"
                                }
                            })
                        .MaxFailedAttempts(2),
                    policy
                        .Move(
                            new KafkaProducerEndpoint("silverback-examples-events")
                            {
                                Configuration = new KafkaProducerConfig
                                {
                                    BootstrapServers = "PLAINTEXT://localhost:9092"
                                }
                            })
                        .Transform(
                            (envelope, ex) =>
                            {
                                envelope.Headers.Add("exception-message", ex.Message);
                                envelope.Message = new IntegrationEventA
                                {
                                    Content = $"Transformed BadEvent (exception: {ex.Message})"
                                };
                            })
                        .Publish(
                            messages => new MessageMovedEvent
                            {
                                Source = messages.First().Endpoint.Name,
                                Destination = "silverback-examples-events"
                            })))
            .AddInbound(
                new KafkaConsumerEndpoint("silverback-examples-error-events2")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = $"{_consumerGroupName}__error2"
                    }
                },
                policy => policy.Chain(
                    policy.Retry().MaxFailedAttempts(2),
                    policy.Skip()))
            .AddInbound(
                new KafkaConsumerEndpoint("silverback-examples-error-events3")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = _consumerGroupName,
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    }
                },
                policy => policy.Retry().MaxFailedAttempts(5))
            .AddInbound(
                new KafkaConsumerEndpoint("silverback-examples-error-unhandled")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = $"{_consumerGroupName}__unhandled"
                    }
                })
            .AddInbound(
                new KafkaConsumerEndpoint("silverback-examples-avro")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = $"{_consumerGroupName}__avro",
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    },
                    Serializer = new AvroMessageSerializer<AvroMessage>
                    {
                        SchemaRegistryConfig = new SchemaRegistryConfig
                        {
                            Url = "localhost:8081"
                        }
                    }
                })
            .AddInbound(
                new KafkaConsumerEndpoint("silverback-examples-newtonsoft")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = $"{_consumerGroupName}newtonsoft",
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    },
                    Serializer = new NewtonsoftJsonMessageSerializer()
                })
            .AddInbound(
                new KafkaConsumerEndpoint("silverback-examples-encrypted")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = $"{_consumerGroupName}__encrypted",
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    },
                    Encryption = new SymmetricEncryptionSettings
                    {
                        Key = Constants.AesEncryptionKey
                    }
                })
            .AddInbound(
                new KafkaConsumerEndpoint("silverback-examples-avro-encrypted")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = $"{_consumerGroupName}__avro-encrypted",
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    },
                    Serializer = new AvroMessageSerializer<AvroMessage>
                    {
                        SchemaRegistryConfig = new SchemaRegistryConfig
                        {
                            Url = "localhost:8081"
                        }
                    },
                    Encryption = new SymmetricEncryptionSettings
                    {
                        Key = Constants.AesEncryptionKey
                    }
                })

            // Special inbound (not logged)
            .AddInbound<InboundConnector>(
                new KafkaConsumerEndpoint("silverback-examples-legacy-messages")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = $"{_consumerGroupName}__legacy",
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    },
                    Serializer = new JsonMessageSerializer<LegacyMessage>()
                });
    }
}
