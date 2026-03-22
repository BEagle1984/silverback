// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.TestBench.Producer.Messages;

namespace Silverback.TestBench.Producer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder) =>
        builder
            .AddKafkaClients(clients => clients
                .AddProducer(producer => producer
                    .WithBootstrapServers("PLAINTEXT://localhost:19092,PLAINTEXT://localhost:29092")
                    .WithClientId("testbench-producer")
                    .Produce<KafkaRoutableTestBenchMessage>(
                        $"kafka-dynamic-topic",
                        endpoint => endpoint
                            .ProduceToDynamicTopic()
                            //.StoreToOutbox(outbox => outbox.UsePostgreSql(App.PostgreSqlConnectionString))
                            .SetKafkaKey(message => message?.MessageId))))
            .AddMqttClients(clients => clients
                .AddClient(client => client
                    .ConnectViaTcp("localhost").WithClientId("testbench-producer")
                    .Produce<MqttRoutableTestBenchMessage>(
                        $"mqtt-dynamic-topic",
                        endpoint => endpoint
                            .ProduceToDynamicTopic()
                            //.StoreToOutbox(outbox => outbox.UsePostgreSql(App.PostgreSqlConnectionString))
                            .WithAtLeastOnceQoS())));
}
