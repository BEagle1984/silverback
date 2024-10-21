// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.TestBench.Configuration;
using Silverback.TestBench.Configuration.Models;

namespace Silverback.TestBench.Producer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder) =>
        builder
            .AddKafkaClients(
            clients => clients
                .AddProducer(
                    producer =>
                    {
                        producer.WithBootstrapServers("PLAINTEXT://localhost:9092").WithClientId("testbench-producer");

                        foreach (KafkaTopicConfiguration topic in TopicsConfiguration.Kafka)
                        {
                            producer.Produce<RoutableTestBenchMessage>(
                                endpoint => endpoint
                                    .ProduceTo(topic.TopicName)
                                    .Filter(message => message?.TargetTopicConfiguration.TopicName == topic.TopicName)
                                    .SetMessageId(message => message?.MessageId));
                        }
                    }))
            .AddMqttClients(clients => clients
                .AddClient(
                    client =>
                    {
                        client.ConnectViaTcp("localhost").WithClientId("testbench-producer");

                        foreach (MqttTopicConfiguration topic in TopicsConfiguration.Mqtt)
                        {
                            client.Produce<RoutableTestBenchMessage>(
                                endpoint => endpoint
                                    .ProduceTo(topic.TopicName)
                                    .WithAtLeastOnceQoS()
                                    .Filter(message => message?.TargetTopicConfiguration.TopicName == topic.TopicName)
                                    .SetMessageId(message => message?.MessageId));
                        }
                    }));
}
