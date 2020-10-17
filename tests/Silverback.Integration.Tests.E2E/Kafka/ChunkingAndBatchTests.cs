// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class ChunkingAndBatchTests : E2ETestFixture
    {
        [Fact]
        public async Task Chunking_Json_ProducedAndConsumed()
        {
            var batches = new List<List<TestEventOne>>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint(DefaultTopicName)
                                    {
                                        Chunk = new ChunkSettings
                                        {
                                            Size = 10
                                        }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        Batch = new BatchSettings
                                        {
                                            Size = 5
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventOne> streamEnumerable) =>
                            {
                                var list = new List<TestEventOne>();

                                await foreach (var message in streamEnumerable)
                                {
                                    list.Add(message);
                                }

                                batches.Add(list);
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await Enumerable.Range(1, 15).ForEachAsync(
                i =>
                    publisher.PublishAsync(
                        new TestEventOne
                        {
                            Content = $"Long message {i}"
                        }));

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync(TimeSpan.FromMinutes(1));

            batches.Should().HaveCount(3);
            batches[0].Should().HaveCount(5);
            batches[1].Should().HaveCount(5);
            batches[2].Should().HaveCount(5);

            batches[0][0].Content.Should().Be("Long message 1");
            batches[0][1].Content.Should().Be("Long message 2");
            batches[0][2].Content.Should().Be("Long message 3");
            batches[0][3].Content.Should().Be("Long message 4");
            batches[0][4].Content.Should().Be("Long message 5");

            batches[1][0].Content.Should().Be("Long message 6");
            batches[1][1].Content.Should().Be("Long message 7");
            batches[1][2].Content.Should().Be("Long message 8");
            batches[1][3].Content.Should().Be("Long message 9");
            batches[1][4].Content.Should().Be("Long message 10");

            batches[2][0].Content.Should().Be("Long message 11");
            batches[2][1].Content.Should().Be("Long message 12");
            batches[2][2].Content.Should().Be("Long message 13");
            batches[2][3].Content.Should().Be("Long message 14");
            batches[2][4].Content.Should().Be("Long message 15");
        }

        // TODO: Test different error kinds (deserialization, processing, etc.)

        // TODO: Test message with single chunk (index 0, last true) -> above all if it is the first in the new batch
    }
}
