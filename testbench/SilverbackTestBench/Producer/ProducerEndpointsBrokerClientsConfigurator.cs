// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.TestBench.Configuration;
using Silverback.TestBench.Configuration.Models;

namespace Silverback.TestBench.Producer;

public class ProducerEndpointsBrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder) =>
        builder.AddKafkaClients(
            clients => clients
                .AddProducer(
                    producer =>
                    {
                        producer.WithBootstrapServers("PLAINTEXT://localhost:9092");

                        foreach (KafkaTopicConfiguration topic in Topics.Kafka)
                        {
                            producer.Produce<RoutableTestBenchMessage>(
                                endpoint => endpoint
                                    .ProduceTo(topic.TopicName)
                                    .Filter(message => message?.TargetTopic == topic.TopicName)
                                    .SetMessageId(message => message?.MessageId));
                        }
                    }));
}
