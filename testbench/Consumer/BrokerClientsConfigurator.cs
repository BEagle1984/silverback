// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.TestBench.Models;

namespace Silverback.TestBench.Consumer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder) =>
        builder
            .AddKafkaClients(
                clients => clients
                    .WithBootstrapServers("PLAINTEXT://kafka:29092")
                    //.WithBootstrapServers("PLAINTEXT://localhost:9092")
                    .AddConsumer(
                        consumer => consumer
                            .WithGroupId("testbench-consumer")
                            .AutoResetOffsetToEarliest()
                            .Consume<TestBenchMessage>(
                                endpoint => endpoint
                                    .ConsumeFrom("testbench-simple")
                                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()))
                            .Consume<TestBenchMessage>(
                                endpoint => endpoint.ConsumeFrom("testbench-batch")
                                    .EnableBatchProcessing(100, TimeSpan.FromSeconds(2))
                                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()))));
}
