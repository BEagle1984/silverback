// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ChunkingFixture
{
    [Fact]
    public async Task Chunking_ShouldProduceChunkedJsonViaOutbox()
    {
        const int chunkSize = 10;
        const int chunksPerMessage = 4;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(
                            mockOptions => mockOptions
                                .WithDefaultPartitionsCount(1)).AddInMemoryOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseMemory())
                                .WithInterval(TimeSpan.FromMilliseconds(50))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(
                                    "my-endpoint",
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(chunkSize)
                                        .StoreToOutbox(outbox => outbox.UseMemory())))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(5);
        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(5 * chunksPerMessage);
        Helper.Spy.RawOutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.Should().BeLessOrEqualTo(chunkSize));
        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);

        for (int i = 0; i < Helper.Spy.RawOutboundEnvelopes.Count; i++)
        {
            int firstEnvelopeIndex = i / chunksPerMessage * chunksPerMessage;
            IOutboundEnvelope lastEnvelope = Helper.Spy.RawOutboundEnvelopes[firstEnvelopeIndex + chunksPerMessage - 1];
            IOutboundEnvelope envelope = Helper.Spy.RawOutboundEnvelopes[i];

            envelope.Headers.GetValue(DefaultMessageHeaders.ChunksCount).Should()
                .Be(chunksPerMessage.ToString(CultureInfo.InvariantCulture));

            if (envelope == lastEnvelope)
            {
                envelope.Headers.GetValue(DefaultMessageHeaders.IsLastChunk).Should().Be(true.ToString());
            }
            else
            {
                envelope.Headers.GetValue(DefaultMessageHeaders.IsLastChunk).Should().BeNull();
            }
        }

        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .Should().BeEquivalentTo(Enumerable.Range(1, 5).Select(i => $"Long message {i}"));
    }
}
