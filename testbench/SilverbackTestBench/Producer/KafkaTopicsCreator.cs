// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Silverback.TestBench.Configuration;
using Silverback.TestBench.UI;

namespace Silverback.TestBench.Producer;

public static class KafkaTopicsCreator
{
    public static async Task CreateAllTopicsAsync()
    {
        IAdminClient adminClient = new AdminClientBuilder(
            new ClientConfig
            {
                BootstrapServers = "PLAINTEXT://localhost:9092"
            }).Build();

        Console.Write("Deleting Kafka topics...");
        await TryDeleteTopicsAsync(adminClient);
        ConsoleHelper.WriteDone();

        Console.Write("Creating Kafka topics...");
        await TryCreateTopicsAsync(adminClient, 1);
        ConsoleHelper.WriteDone();
    }

    private static async Task TryCreateTopicsAsync(IAdminClient adminClient, int tryCount)
    {
        try
        {
            await adminClient.CreateTopicsAsync(
                Topics.Kafka.Select(
                    topic =>
                        new TopicSpecification
                        {
                            Name = topic.TopicName,
                            NumPartitions = topic.PartitionsCount
                        }));
        }
        catch (CreateTopicsException)
        {
            Console.Write(".");

            if (tryCount > 10)
                throw;

            await Task.Delay(TimeSpan.FromSeconds(1 + tryCount));
            await TryCreateTopicsAsync(adminClient, tryCount + 1);
        }
    }

    private static async Task TryDeleteTopicsAsync(IAdminClient adminClient)
    {
        try
        {
            await adminClient.DeleteTopicsAsync(Topics.Kafka.Select(topic => topic.TopicName));
        }
        catch (DeleteTopicsException)
        {
            // Ignore
        }
    }
}
