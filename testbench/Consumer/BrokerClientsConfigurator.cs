// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.TestBench.Consumer.Models;

namespace Silverback.TestBench.Consumer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    private static readonly string ClientId = $"testbench-consumer-{Environment.GetEnvironmentVariable("CONTAINER_NAME")}";

    public void Configure(BrokerClientsConfigurationBuilder builder) =>
        builder
            .AddKafkaClients(
                clients => clients
                    .WithBootstrapServers("PLAINTEXT://kafka:29092")
                    .AddConsumer(
                        consumer => consumer
                            .WithGroupId(ClientId)
                            .WithClientId(ClientId)
                            .AutoResetOffsetToEarliest()
                            .Consume<SingleMessage>(
                                endpoint => endpoint
                                    .ConsumeFrom(Topics.Kafka.Single)
                                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                                    .OnError(policy => policy.Retry(5).ThenSkip()))
                            .Consume<BatchMessage>(
                                endpoint => endpoint.ConsumeFrom(Topics.Kafka.Batch)
                                    .EnableBatchProcessing(100, TimeSpan.FromSeconds(2))
                                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                                    .OnError(policy => policy.Retry(5).ThenSkip()))
                            .Consume<UnboundedMessage>(
                                endpoint => endpoint.ConsumeFrom(Topics.Kafka.Unbounded)
                                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                                    .OnError(policy => policy.Retry(5).ThenSkip()))))
            .AddMqttClients(
                clients => clients
                    .AddClient(
                        client => client
                            .WithClientId(ClientId)
                            .ConnectViaTcp("haproxy")
                            .Consume<SingleMessage>(
                                endpoint => endpoint
                                    .ConsumeFrom($"$share/group/{Topics.Mqtt.Single}")
                                    .WithAtLeastOnceQoS()
                                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                                    .OnError(policy => policy.Retry(5).ThenSkip()))
                            .Consume<UnboundedMessage>(
                                endpoint => endpoint
                                    .ConsumeFrom($"$share/group/{Topics.Mqtt.Unbounded}")
                                    .WithAtLeastOnceQoS()
                                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                                    .OnError(policy => policy.Retry(5).ThenSkip()))));
}
