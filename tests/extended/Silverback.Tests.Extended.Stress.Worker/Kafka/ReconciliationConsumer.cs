// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Extended.Shared.Kafka;

namespace Silverback.Tests.Extended.Stress.Worker.Kafka;

internal static class ReconciliationConsumer
{
    public static async Task RunAsync()
    {
        string bootstrap = Environment.GetEnvironmentVariable("BOOTSTRAP") ?? "kafka-1:9092,kafka-2:9092";
        string prefix = Environment.GetEnvironmentVariable("PREFIX") ?? throw new InvalidOperationException("PREFIX is required.");
        string member = Environment.GetEnvironmentVariable("MEMBER") ?? "primary";
        bool batch = Environment.GetEnvironmentVariable("WORKLOAD") == "batch";
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        using ReconciliationSubscriber subscriber = new(bootstrap, prefix, member);
        SilverbackBuilder silverback = builder.Services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddSingletonBrokerClientCallback(subscriber);

        // Register exactly one method for the selected workload.
        if (batch)
        {
            silverback.AddDelegateSubscriber<System.Collections.Generic.IAsyncEnumerable<Silverback.Messaging.Messages.IInboundEnvelope<ReconciliationMessage>>>(
                subscriber.OnBatchReceivedAsync);
        }
        else
        {
            silverback.AddDelegateSubscriber<Silverback.Messaging.Messages.IInboundEnvelope<ReconciliationMessage>>(subscriber.OnMessageReceivedAsync);
        }

        silverback.AddKafkaClients(clients => clients.WithBootstrapServers(bootstrap).AddConsumer(consumer =>
        {
            consumer.WithGroupId(prefix + "-group").WithClientId(member)
                .WithMaxPollIntervalMs(300000).WithSessionTimeoutMs(6000).WithHeartbeatIntervalMs(1000)
                .LimitParallelism(2).LimitBackpressure(2).AutoResetOffsetToEarliest();
            if (Environment.GetEnvironmentVariable("SHARED_CHANNEL") == "true")
                consumer.ProcessAllPartitionsTogether().LimitParallelism(1);
            if (Environment.GetEnvironmentVariable("AUTO_COMMIT") != "true")
                consumer.CommitOffsetEach(1);
            string? assignor = Environment.GetEnvironmentVariable("ASSIGNOR");
            if (!string.IsNullOrEmpty(assignor))
                consumer.WithPartitionAssignmentStrategy(Enum.Parse<PartitionAssignmentStrategy>(assignor, true));
            consumer.Consume<ReconciliationMessage>(endpoint =>
            {
                endpoint.ConsumeFrom(prefix + "-records")
                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())
                    .OnError(policy => policy.Retry(2));
                if (batch)
                    endpoint.EnableBatchProcessing(10, TimeSpan.FromMilliseconds(500));
            });
        }));
        using IHost host = builder.Build();
        await host.RunAsync();
    }
}
