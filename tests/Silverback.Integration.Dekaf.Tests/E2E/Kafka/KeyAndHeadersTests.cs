// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dekaf.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

// Covers the mapping of the key and headers onto the Dekaf records, which the shared tests only verify through the
// Confluent.Kafka based mock
public class KeyAndHeadersTests : KafkaTests
{
    public KeyAndHeadersTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Use case: the message key and the custom headers reach the consumer, and the key drives the partitioning
    [Fact]
    public async Task KeyAndHeaders_ShouldBeForwardedToConsumer()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 6; i++)
        {
            await publisher.WrapAndPublishAsync(
                new TestEventOne { ContentEventOne = $"{i}" },
                envelope => envelope
                    .SetKafkaKey($"key-{i % 2}")
                    .AddHeader("x-custom", $"value-{i}"));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(6);

        foreach (IInboundEnvelope envelope in Helper.Spy.InboundEnvelopes)
        {
            int index = int.Parse(((TestEventOne)envelope.Message!).ContentEventOne!, System.Globalization.CultureInfo.InvariantCulture);
            envelope.GetKafkaKey().ShouldBe($"key-{index % 2}");
            envelope.Headers.GetValue("x-custom").ShouldBe($"value-{index}");
        }

        Helper.Spy.InboundEnvelopes
            .GroupBy(envelope => envelope.GetKafkaKey())
            .ShouldAllBe(group => group.Select(envelope => ((KafkaOffset)envelope.BrokerMessageIdentifier).TopicPartition.Partition).Distinct().Count() == 1);

        IReadOnlyList<InMemoryRecord> records = [.. Enumerable.Range(0, 3).SelectMany(partition => Helper.Cluster.ReadRecords(DefaultTopicName, partition))];
        records.Count.ShouldBe(6);
        records.ShouldAllBe(record => record.Key != null && Encoding.UTF8.GetString(record.Key).StartsWith("key-", StringComparison.Ordinal));
    }
}
