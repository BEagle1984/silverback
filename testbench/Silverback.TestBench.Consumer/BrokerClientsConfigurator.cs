// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Messaging.Configuration;
using Silverback.TestBench.Consumer.Models;

namespace Silverback.TestBench.Consumer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    private static readonly string ClientId = $"testbench-consumer-{Environment.GetEnvironmentVariable("CONTAINER_NAME")}";

    private static readonly Action<IErrorPolicyBuilder> DefaultErrorPolicy =
        policy => policy.Retry(5).ThenSkip();

    public void Configure(BrokerClientsConfigurationBuilder builder) =>
        builder
            .AddKafkaClients(
                clients => clients
                    .WithBootstrapServers("PLAINTEXT://kafka-1:9092,PLAINTEXT://kafka-2:9092")
                    .AddConsumer(
                        consumer => consumer
                            .WithGroupId("testbench")
                            .WithClientId(ClientId)
                            .WithPartitionAssignmentStrategy(PartitionAssignmentStrategy.CooperativeSticky)
                            .AutoResetOffsetToEarliest()
                            .Consume<SingleMessage>(
                                endpoint => endpoint
                                    .ConsumeFrom(TopicNames.Kafka.Single)
                                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                                    .OnError(DefaultErrorPolicy))
                            .Consume<BatchMessage>(
                                endpoint => endpoint.ConsumeFrom(TopicNames.Kafka.Batch)
                                    .EnableBatchProcessing(100, TimeSpan.FromSeconds(2))
                                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                                    .OnError(DefaultErrorPolicy))
                            .Consume<UnboundedMessage>(
                                endpoint => endpoint.ConsumeFrom(TopicNames.Kafka.Unbounded)
                                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                                    .OnError(DefaultErrorPolicy)))
                    .AddProducer(
                        producer => producer
                            .Produce<KafkaResponseMessage>(
                                endpoint => endpoint
                                    .ProduceTo("testbench-responses"))))
            .AddMqttClients(
                clients => clients
                    .AddClient(
                        client => client
                            .WithClientId(ClientId)
                            .ConnectViaTcp("haproxy")
                            .Consume<SingleMessage>(
                                endpoint => endpoint
                                    .ConsumeFrom($"$share/group/{TopicNames.Mqtt.Single}")
                                    .WithAtLeastOnceQoS()
                                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                                    .OnError(DefaultErrorPolicy))
                            .Consume<UnboundedMessage>(
                                endpoint => endpoint
                                    .ConsumeFrom($"$share/group/{TopicNames.Mqtt.Unbounded}")
                                    .WithAtLeastOnceQoS()
                                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                                    .OnError(DefaultErrorPolicy))
                            .Produce<MqttResponseMessage>(
                                endpoint => endpoint
                                    .ProduceTo("testbench/mqtt/responses")
                                    .WithAtMostOnceQoS())));
}
