// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Extended.Shared;
using Silverback.Tests.Extended.Shared.Messages;
using Silverback.Tests.Extended.Stress.Worker.Diagnostics;

namespace Silverback.Tests.Extended.Stress.Worker.Kafka;

internal static class MixedWorkload
{
    public static async Task RunAsync(string[] args)
    {
        string role = Environment.GetEnvironmentVariable("ROLE") ?? "consumer";
        string bootstrap = Environment.GetEnvironmentVariable("BOOTSTRAP") ?? "kafka-1:9092,kafka-2:9092";
        string member = Environment.GetEnvironmentVariable("MEMBER") ?? "primary";
        string scenario = Environment.GetEnvironmentVariable("SCENARIO") ?? "mixed";
        int partitions = ReadInt("PARTITIONS", 12);
        int seed = ReadInt("SEED", 1729);
        string prefix = Environment.GetEnvironmentVariable("PREFIX") ?? throw new InvalidOperationException("PREFIX is required.");
        string[] topics = [prefix + "-single", prefix + "-batch", prefix + "-batch2", prefix + "-stream"];

        Console.WriteLine($"START role={role} member={member} scenario={scenario} seed={seed} runtime={Environment.Version} " +
                          $"silverback={typeof(Silverback.Messaging.Broker.KafkaConsumer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion} " +
                          $"confluent={typeof(ConsumerConfig).Assembly.GetName().Version} librdkafka={Library.VersionString}");

        if (role == "producer")
        {
            using IAdminClient admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrap }).Build();
            await admin.CreateTopicsAsync(topics.Append(prefix + "-responses").Select(topic => new TopicSpecification
            {
                Name = topic,
                NumPartitions = partitions,
                ReplicationFactor = 1
            }));

            using IProducer<Null, byte[]> producer = new ProducerBuilder<Null, byte[]>(new ProducerConfig
            {
                BootstrapServers = bootstrap,
                ClientId = "stress-producer",
                Acks = Acks.All,
                MessageTimeoutMs = 10000
            }).Build();

            Random random = new(seed);
            long sent = 0;

            while (true)
            {
                // Produce to every partition, including sparse batches completed only by their timeout
                for (int partition = 0; partition < partitions; partition++)
                {
                    foreach (string topic in scenario is "single" or "control" ? topics.Take(1) : topics)
                    {
                        long index = sent++;
                        TestBenchMessage message = new()
                        {
                            MessageId = $"{seed}-{index}",
                            CreatedAt = DateTime.UtcNow,
                            SimulatedProcessingTime = TimeSpan.FromMilliseconds(
                                scenario == "control" && index == 0 ? 90000 : random.Next(0, 8)),
                            SimulatedFailuresCount = scenario != "control" && random.Next(100) < 8 ? random.Next(1, 4) : 0
                        };

                        await producer.ProduceAsync(new TopicPartition(topic, partition), new Message<Null, byte[]>
                        {
                            Value = JsonSerializer.SerializeToUtf8Bytes(message)
                        });
                    }
                }

                if (sent % (partitions * 20) == 0)
                    Console.WriteLine($"PRODUCED {sent}");

                await Task.Delay(ReadInt("PRODUCE_DELAY_MS", 20));
            }
        }

        using ProgressProbe probe = new(member, ReadInt("STALL_SECONDS", 20));
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        builder.Logging
            .ClearProviders()
            .SetMinimumLevel(LogLevel.Trace)
            .AddProvider(probe);

        builder.Services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddSingletonSubscriber<Subscriber>()
            .AddKafkaClients(clients => clients
                .WithBootstrapServers(bootstrap)
                .AddConsumer(consumer =>
                {
                    consumer
                        .WithGroupId(prefix + "-group")
                        .WithClientId(member)
                        .WithMaxPollIntervalMs(ReadInt("MAX_POLL_MS", 300000))
                        .WithSessionTimeoutMs(6000)
                        .WithHeartbeatIntervalMs(1000)
                        .WithStatisticsIntervalMs(1000)
                        .LimitParallelism(ReadInt("PARALLELISM", 100))
                        .LimitBackpressure(ReadInt("BACKPRESSURE", 50))
                        .AutoResetOffsetToEarliest();

                    string? assignor = Environment.GetEnvironmentVariable("ASSIGNOR");

                    if (!string.IsNullOrEmpty(assignor))
                        consumer.WithPartitionAssignmentStrategy(Enum.Parse<PartitionAssignmentStrategy>(assignor, true));

                    if (Environment.GetEnvironmentVariable("AUTO_RECOVERY") != "true")
                        consumer.DisableAutoRecovery();

                    if (Environment.GetEnvironmentVariable("MANUAL_COMMIT") == "true")
                        consumer.CommitOffsetEach(1);

                    consumer.Consume<SingleMessage>(endpoint => endpoint
                        .ConsumeFrom(topics[0])
                        .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                        .OnError(policy => policy.Retry(5).ThenSkip()));

                    if (scenario is "single" or "control")
                        return;

                    consumer.Consume<BatchMessage>(endpoint => endpoint
                            .ConsumeFrom(topics[1])
                            .EnableBatchProcessing(100, TimeSpan.FromMilliseconds(ReadInt("BATCH_TIMEOUT_MS", 100)))
                            .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                            .OnError(policy => policy.Retry(5).ThenSkip()))
                        .Consume<BatchMessage2>(endpoint => endpoint
                            .ConsumeFrom(topics[2])
                            .EnableBatchProcessing(50, TimeSpan.FromMilliseconds(ReadInt("BATCH_TIMEOUT_MS", 100)))
                            .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                            .OnError(policy => policy.Retry(5).ThenSkip()))
                        .Consume<UnboundedMessage>(endpoint => endpoint
                            .ConsumeFrom(topics[3])
                            .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                            .AllowStreaming());
                })
                .AddProducer(producer => producer.Produce<KafkaResponseMessage>(endpoint => endpoint.ProduceTo(prefix + "-responses"))));

        using IHost host = builder.Build();
        probe.Start();
        await host.RunAsync();

        static int ReadInt(string name, int defaultValue) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : defaultValue;
    }
}
