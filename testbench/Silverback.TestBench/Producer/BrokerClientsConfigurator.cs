// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.TestBench.ViewModel;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.Producer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    private readonly MainViewModel _mainViewModel;

    public BrokerClientsConfigurator(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public void Configure(BrokerClientsConfigurationBuilder builder) =>
        builder
            .AddKafkaClients(
                clients => clients
                    .AddProducer(
                        producer =>
                        {
                            producer
                                .WithBootstrapServers("PLAINTEXT://localhost:19092,PLAINTEXT://localhost:29092")
                                .WithClientId("testbench-producer");

                            foreach (KafkaTopicViewModel topic in _mainViewModel.KafkaTopics)
                            {
                                producer.Produce<RoutableTestBenchMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(topic.TopicName)
                                        .Filter(message => message?.TargetTopicViewModel.TopicName == topic.TopicName)
                                        .SetMessageId(message => message?.MessageId));
                            }
                        }))
            .AddMqttClients(
                clients => clients
                    .AddClient(
                        client =>
                        {
                            client.ConnectViaTcp("localhost").WithClientId("testbench-producer");

                            foreach (MqttTopicViewModel topic in _mainViewModel.MqttTopics)
                            {
                                client.Produce<RoutableTestBenchMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(topic.TopicName)
                                        .WithAtLeastOnceQoS()
                                        .Filter(message => message?.TargetTopicViewModel.TopicName == topic.TopicName)
                                        .SetMessageId(message => message?.MessageId));
                            }
                        }));
}
