// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
    [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
    public class ChunkingTests : E2ETestFixture
    {
        public ChunkingTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Chunking_Json_ProducedAndConsumed()
        {
            const int chunksPerMessage = 3;

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
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
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await Enumerable.Range(1, 5).ForEachAsync(
                i =>
                    publisher.PublishAsync(
                        new TestEventOne
                        {
                            Content = $"Long message {i}"
                        }));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(5);
            Subscriber.InboundEnvelopes.Should().HaveCount(5);

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(5 * chunksPerMessage);
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope => envelope.RawMessage.ReReadAll()!.Length.Should().BeLessOrEqualTo(10));
            SpyBehavior.InboundEnvelopes.Should().HaveCount(5);

            for (int i = 0; i < SpyBehavior.OutboundEnvelopes.Count; i++)
            {
                var firstEnvelope = SpyBehavior.OutboundEnvelopes[(i / chunksPerMessage) * chunksPerMessage];
                var lastEnvelope =
                    SpyBehavior.OutboundEnvelopes[(((i / chunksPerMessage) * chunksPerMessage) + chunksPerMessage) - 1];
                var envelope = SpyBehavior.OutboundEnvelopes[i];

                envelope.Headers.GetValue(DefaultMessageHeaders.ChunksCount).Should()
                    .Be(chunksPerMessage.ToString(CultureInfo.InvariantCulture));

                if (envelope == firstEnvelope)
                {
                    envelope.Headers.GetValue(DefaultMessageHeaders.FirstChunkOffset).Should().BeNull();
                }
                else
                {
                    envelope.Headers.GetValue(DefaultMessageHeaders.FirstChunkOffset).Should()
                        .Be(firstEnvelope.Offset!.Value);
                }

                if (envelope == lastEnvelope)
                {
                    envelope.Headers.GetValue(DefaultMessageHeaders.IsLastChunk).Should().Be(true.ToString());
                }
                else
                {
                    envelope.Headers.GetValue(DefaultMessageHeaders.IsLastChunk).Should().BeNull();
                }
            }

            SpyBehavior.InboundEnvelopes
                .Select(envelope => ((TestEventOne)envelope.Message!).Content)
                .Should().BeEquivalentTo(Enumerable.Range(1, 5).Select(i => $"Long message {i}"));
        }

        [Fact]
        public async Task Chunking_BinaryFile_ProducedAndConsumed()
        {
            var message1 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                        0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
                    }),
                ContentType = "application/pdf"
            };

            var message2 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                        0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x40,
                        0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x50
                    }),
                ContentType = "text/plain"
            };

            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
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
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) => { receivedFiles.Add(binaryFile.Content.ReadAll()); })
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(6);
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope => envelope.RawMessage.ReReadAll()!.Length.Should().BeLessOrEqualTo(10));
            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeOfType<BinaryFileMessage>());

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(message1.Content.ReReadAll(), message2.Content.ReReadAll());
        }

        [Fact]
        public async Task Chunking_JsonWithIsLastChunkHeader_Consumed()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings()
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, false, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, true, typeof(TestEventOne)));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);

            var sequenceStores = broker.Consumers[0].GetCurrentSequenceStores();
            sequenceStores.Any(store => store.HasPendingSequences).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_JsonWithChunksCountHeader_Consumed()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings()
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3, typeof(TestEventOne)));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);

            var sequenceStores = broker.Consumers[0].GetCurrentSequenceStores();
            sequenceStores.Any(store => store.HasPendingSequences).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_JsonWithMessageIdHeader_Consumed()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings()
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 2, 3, typeof(TestEventOne)));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);

            var sequenceStores = broker.Consumers[0].GetCurrentSequenceStores();
            sequenceStores.Any(store => store.HasPendingSequences).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileWithIsLastChunkHeader_Consumed()
        {
            var rawMessage = Encoding.UTF8.GetBytes("Hello E2E!");
            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings(),
                                        Serializer = BinaryFileMessageSerializer.Default
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) => receivedFiles.Add(binaryFile.Content.ReadAll())))
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.RawProduceAsync(
                rawMessage.Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, false));
            await producer.RawProduceAsync(
                rawMessage.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1));
            await producer.RawProduceAsync(
                rawMessage.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, true));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(rawMessage);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);

            var sequenceStores = broker.Consumers[0].GetCurrentSequenceStores();
            sequenceStores.Any(store => store.HasPendingSequences).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileWithChunksCountHeader_Consumed()
        {
            var rawMessage = Encoding.UTF8.GetBytes("Hello E2E!");
            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings(),
                                        Serializer = BinaryFileMessageSerializer.Default
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) => receivedFiles.Add(binaryFile.Content.ReadAll())))
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.RawProduceAsync(
                rawMessage.Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(rawMessage);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);

            var sequenceStores = broker.Consumers[0].GetCurrentSequenceStores();
            sequenceStores.Any(store => store.HasPendingSequences).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileWithMessageIdHeader_Consumed()
        {
            var rawMessage = Encoding.UTF8.GetBytes("Hello E2E!");
            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings(),
                                        Serializer = BinaryFileMessageSerializer.Default
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) => receivedFiles.Add(binaryFile.Content.ReadAll())))
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.RawProduceAsync(
                rawMessage.Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 0, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 1, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 2, 3));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(rawMessage);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);

            var sequenceStores = broker.Consumers[0].GetCurrentSequenceStores();
            sequenceStores.Any(store => store.HasPendingSequences).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileWithMessageTypeHeader_Consumed()
        {
            var rawMessage = Encoding.UTF8.GetBytes("Hello E2E!");
            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings()
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) => receivedFiles.Add(binaryFile.Content.ReadAll())))
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.RawProduceAsync(
                rawMessage.Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 0, 3, typeof(BinaryFileMessage)));
            await producer.RawProduceAsync(
                rawMessage.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 1, 3, typeof(BinaryFileMessage)));
            await producer.RawProduceAsync(
                rawMessage.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 2, 3, typeof(BinaryFileMessage)));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(rawMessage);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);

            var sequenceStores = broker.Consumers[0].GetCurrentSequenceStores();
            sequenceStores.Any(store => store.HasPendingSequences).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_JsonWithDuplicatedChunks_DuplicatesIgnored()
        {
            var message1 = new TestEventOne { Content = "Message 1" };
            byte[] rawMessage1 = (await Endpoint.DefaultSerializer.SerializeAsync(
                                     message1,
                                     new MessageHeaderCollection(),
                                     MessageSerializationContext.Empty)).ReadAll() ??
                                 throw new InvalidOperationException("Serializer returned null");
            var message2 = new TestEventOne { Content = "Message 2" };
            byte[] rawMessage2 = (await Endpoint.DefaultSerializer.SerializeAsync(
                                     message2,
                                     new MessageHeaderCollection(),
                                     MessageSerializationContext.Empty)).ReadAll() ??
                                 throw new InvalidOperationException("Serializer returned null");

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings()
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));

            await producer.RawProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("2", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("2", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("2", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("2", 1, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage1.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("2", 2, true, typeof(TestEventOne)));

            await producer.RawProduceAsync(
                rawMessage2.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3, typeof(TestEventOne)));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes
                .Select(envelope => envelope.Message.As<TestEventOne>().Content)
                .Should().BeEquivalentTo("Message 1", "Message 2");

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            var sequenceStores = broker.Consumers[0].GetCurrentSequenceStores();
            sequenceStores.Any(store => store.HasPendingSequences).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileWithDuplicatedChunks_DuplicatesIgnored()
        {
            byte[] rawMessage1 = Encoding.UTF8.GetBytes("Message 1");
            byte[] rawMessage2 = Encoding.UTF8.GetBytes("Message 2");
            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings(),
                                        Serializer = BinaryFileMessageSerializer.Default
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) => receivedFiles.Add(binaryFile.Content.ReadAll())))
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));

            await producer.RawProduceAsync(
                rawMessage1.Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("6", 0));
            await producer.RawProduceAsync(
                rawMessage1.Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("6", 0));
            await producer.RawProduceAsync(
                rawMessage1.Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("6", 0));
            await producer.RawProduceAsync(
                rawMessage1.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("6", 1));
            await producer.RawProduceAsync(
                rawMessage1.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("6", 2, true));

            await producer.RawProduceAsync(
                rawMessage2.Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3));
            await producer.RawProduceAsync(
                rawMessage2.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3));
            await producer.RawProduceAsync(
                rawMessage2.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3));
            await producer.RawProduceAsync(
                rawMessage2.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3));
            await producer.RawProduceAsync(
                rawMessage2.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(4);
            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(rawMessage1, rawMessage2);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            var sequenceStores = broker.Consumers[0].GetCurrentSequenceStores();
            sequenceStores.Any(store => store.HasPendingSequences).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileReadAborted_CommittedAndNextMessageConsumed()
        {
            var message1 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                        0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
                    }),
                ContentType = "application/pdf"
            };

            var message2 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                        0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x40,
                        0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x50
                    }),
                ContentType = "text/plain"
            };

            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
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
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                if (binaryFile.ContentType != "text/plain")
                                {
                                    // Read only first chunk
                                    var buffer = new byte[10];
                                    binaryFile.Content!.Read(buffer, 0, 10);
                                    return;
                                }

                                receivedFiles.Add(binaryFile.Content.ReadAll());
                            })
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);

            await publisher.PublishAsync(message2);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);

            SpyBehavior.InboundEnvelopes[0].Message.As<BinaryFileMessage>().ContentType.Should().Be("application/pdf");
            SpyBehavior.InboundEnvelopes[1].Message.As<BinaryFileMessage>().ContentType.Should().Be("text/plain");

            receivedFiles.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(6);
        }

        [Fact]
        public async Task Chunking_BinaryFileReadAbortedMidChunk_CommittedAndNextMessageConsumed()
        {
            var message1 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                        0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
                    }),
                ContentType = "application/pdf"
            };

            var message2 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                        0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x40,
                        0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x50
                    }),
                ContentType = "text/plain"
            };

            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
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
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                if (binaryFile.ContentType != "text/plain")
                                {
                                    // Read only part of first chunk
                                    var buffer = new byte[5];
                                    binaryFile.Content!.Read(buffer, 0, 10);
                                    return;
                                }

                                receivedFiles.Add(binaryFile.Content.ReadAll());
                            })
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);

            await publisher.PublishAsync(message2);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);

            SpyBehavior.InboundEnvelopes[0].Message.As<BinaryFileMessage>().ContentType.Should().Be("application/pdf");
            SpyBehavior.InboundEnvelopes[1].Message.As<BinaryFileMessage>().ContentType.Should().Be("text/plain");

            receivedFiles.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(6);
        }

        [Fact]
        public async Task Chunking_BinaryFileProcessingFailedAfterFirstChunk_DisconnectedAndNotCommitted()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
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
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                // Read only first chunk
                                var buffer = new byte[10];
                                binaryFile.Content!.Read(buffer, 0, 10);

                                throw new InvalidOperationException("Test");
                            })
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(
                new BinaryFileMessage
                {
                    Content = new MemoryStream(
                        new byte[]
                        {
                            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
                        }),
                    ContentType = "application/pdf"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);
            Broker.Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileProcessingFailedImmediately_DisconnectedAndNotCommitted()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
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
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) => { throw new InvalidOperationException("Test"); })
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(
                new BinaryFileMessage
                {
                    Content = new MemoryStream(
                        new byte[]
                        {
                            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
                        }),
                    ContentType = "application/pdf"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);
            Broker.Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_Json_AtomicallyCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_BinaryFile_AtomicallyCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_Json_BackpressureHandled()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_BinaryFile_BackpressureHandled()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_EnforcingConsecutiveJsonChunks_IncompleteSequenceDiscardedWhenOtherSequenceStarts()
        {
            var message1 = new TestEventOne { Content = "Message 1" };
            byte[] rawMessage1 = (await Endpoint.DefaultSerializer.SerializeAsync(
                                     message1,
                                     new MessageHeaderCollection(),
                                     MessageSerializationContext.Empty)).ReadAll() ??
                                 throw new InvalidOperationException("Serializer returned null");

            var message2 = new TestEventOne { Content = "Message 2" };
            var rawMessage2 = (await Endpoint.DefaultSerializer.SerializeAsync(
                                  message2,
                                  new MessageHeaderCollection(),
                                  MessageSerializationContext.Empty)).ReadAll() ??
                              throw new InvalidOperationException("Serializer returned null");

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings()
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.RawProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("6", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("6", 1, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("6", 2, true, typeof(TestEventOne)));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(5);

            var sequenceStores = broker.Consumers[0].GetCurrentSequenceStores();
            sequenceStores.Any(store => store.HasPendingSequences).Should().BeFalse();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_EnforcingConsecutiveJsonChunks_IncompleteSequenceDiscardedOnNoSequenceMessage()
        {
            // TODO: Ensure that the rollback always happens before the next commit (and doesn't actually rollback the offset)
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_EnforcingConsecutiveJsonChunks_ErrorPolicyIgnoredWhenIncompleteSequenceDiscarded()
        {
            // TODO: Ensure that the abort doesn't trigger the error policies! (for both sequence and no-sequence message)
            throw new NotImplementedException();
        }

        [Fact(Skip = "Consecutive sequence always enforced at the moment")]
        public async Task Chunking_NotEnforcingConsecutiveJsonChunks_ConsumedAndCommittedOnlyWhenSequencesAreComplete()
        {
            var message1 = new TestEventOne { Content = "Message 1" };
            byte[] rawMessage1 = (await Endpoint.DefaultSerializer.SerializeAsync(
                                     message1,
                                     new MessageHeaderCollection(),
                                     MessageSerializationContext.Empty)).ReadAll() ??
                                 throw new InvalidOperationException("Serializer returned null");

            var message2 = new TestEventOne { Content = "Message 2" };
            var rawMessage2 = (await Endpoint.DefaultSerializer.SerializeAsync(
                                  message2,
                                  new MessageHeaderCollection(),
                                  MessageSerializationContext.Empty)).ReadAll() ??
                              throw new InvalidOperationException("Serializer returned null");

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings()
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.RawProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 2, 3, typeof(TestEventOne)));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);

            await producer.RawProduceAsync(
                rawMessage1.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3, typeof(TestEventOne)));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes[1].Message.As<TestEventOne>().Content.Should().Be("Message 1");
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(6);
        }

        [Fact(Skip = "Always enforcing consecutive sequences at the moment.")]
        public Task Chunking_NotEnforcingConsecutiveJsonChunks_ProcessingErrorAbortsAllSequences()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_IncompleteJsonResent_SecondMessageConsumedAndCommitted()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var sequenceStore = broker.Consumers[0].GetCurrentSequenceStores()[0];
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));

            await AsyncTestingUtil.WaitAsync(
                () => DefaultTopic.GetCommittedOffsetsCount("consumer1") > 0,
                TimeSpan.FromMilliseconds(200));
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);

            var originalSequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            originalSequence.Should().NotBeNull();
            originalSequence!.IsComplete.Should().BeFalse();
            originalSequence!.IsAborted.Should().BeFalse();

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3, typeof(TestEventOne)));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(5);

            originalSequence.IsAborted.Should().BeTrue();
            sequenceStore.HasPendingSequences.Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_IncompleteJson_AbortedAfterTimeoutAndCommitted()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings
                                        {
                                            Timeout = TimeSpan.FromMilliseconds(500)
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var sequenceStore = broker.Consumers[0].GetCurrentSequenceStores()[0];
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));

            await AsyncTestingUtil.WaitAsync(() => sequenceStore.HasPendingSequences);

            await Task.Delay(200);

            var sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            sequence.Should().NotBeNull();
            sequence!.IsAborted.Should().BeFalse();

            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));

            await Task.Delay(300);
            sequence.IsAborted.Should().BeFalse();

            await AsyncTestingUtil.WaitAsync(
                () => !sequence.IsPending && DefaultTopic.GetCommittedOffsetsCount("consumer1") >= 2,
                TimeSpan.FromSeconds(1));
            sequence.IsAborted.Should().BeTrue();

            sequenceStore.HasPendingSequences.Should().BeFalse();

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(2);

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 2, 3, typeof(TestEventOne)));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(5);

            sequenceStore.HasPendingSequences.Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_IncompleteBinaryFile_AbortedAfterTimeoutAndCommitted()
        {
            var rawMessage = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
            };

            bool enumerationAborted = false;
            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings
                                        {
                                            Timeout = TimeSpan.FromMilliseconds(500)
                                        },
                                        Serializer = BinaryFileMessageSerializer.Default
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                try
                                {
                                    receivedFiles.Add(binaryFile.Content.ReadAll());
                                }
                                catch (OperationCanceledException)
                                {
                                    enumerationAborted = true;
                                    throw;
                                }
                            })
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var sequenceStore = broker.Consumers[0].GetCurrentSequenceStores()[0];
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0));

            await AsyncTestingUtil.WaitAsync(() => sequenceStore.HasPendingSequences);

            await Task.Delay(200);

            var sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            sequence.Should().NotBeNull();
            sequence!.IsAborted.Should().BeFalse();

            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1));

            await Task.Delay(300);
            sequence.IsAborted.Should().BeFalse();

            await AsyncTestingUtil.WaitAsync(() => enumerationAborted);
            sequence.IsAborted.Should().BeTrue();
            sequenceStore.HasPendingSequences.Should().BeFalse();
            enumerationAborted.Should().BeTrue();

            await AsyncTestingUtil.WaitAsync(() => DefaultTopic.GetCommittedOffsetsCount("consumer1") >= 2);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(2);

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 0));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 1));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 2, true));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);
            receivedFiles.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(rawMessage);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(5);

            sequenceStore.HasPendingSequences.Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_JsonMissingFirstChunk_NextMessageConsumedAndCommitted()
        {
            var message1 = new TestEventOne { Content = "Message 1" };
            byte[] rawMessage1 = (await Endpoint.DefaultSerializer.SerializeAsync(
                                     message1,
                                     new MessageHeaderCollection(),
                                     MessageSerializationContext.Empty)).ReadAll() ??
                                 throw new InvalidOperationException("Serializer returned null");

            var message2 = new TestEventOne { Content = "Message 2" };
            var rawMessage2 = (await Endpoint.DefaultSerializer.SerializeAsync(
                                  message2,
                                  new MessageHeaderCollection(),
                                  MessageSerializationContext.Empty)).ReadAll() ??
                              throw new InvalidOperationException("Serializer returned null");

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings()
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.RawProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage1.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, true, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 1, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 2, true, typeof(TestEventOne)));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(5);

            var sequenceStores = broker.Consumers[0].GetCurrentSequenceStores();
            sequenceStores.Any(store => store.HasPendingSequences).Should().BeFalse();
        }

        [Fact(Skip = "Hard to implement. Worth it?")]
        public Task Chunking_BinaryFileMissingFirstChunk_NextMessageConsumedAndCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_DisconnectWithIncompleteJson_AbortedAndNotCommitted()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings
                                        {
                                            Timeout = TimeSpan.FromMilliseconds(500)
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var sequenceStore = broker.Consumers[0].GetCurrentSequenceStores()[0];
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));

            await AsyncTestingUtil.WaitAsync(
                async () => sequenceStore.Count == 1 &&
                            (await sequenceStore.GetAsync<ChunkSequence>("1"))?.Length == 2);

            var sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            sequence.Should().NotBeNull();
            sequence!.Length.Should().Be(2);
            sequence!.IsAborted.Should().BeFalse();

            Broker.Disconnect();

            sequence.IsAborted.Should().BeTrue();
            sequenceStore.HasPendingSequences.Should().BeFalse();
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);
        }

        [Fact]
        public async Task Chunking_DisconnectWithIncompleteBinaryFile_AbortedAndNotCommitted()
        {
            var rawMessage = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
            };

            bool enumerationAborted = false;

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Sequence = new SequenceSettings
                                        {
                                            Timeout = TimeSpan.FromMilliseconds(500)
                                        },
                                        Serializer = BinaryFileMessageSerializer.Default
                                    }))
                        .AddDelegateSubscriber(
                            async (BinaryFileMessage binaryFile) =>
                            {
                                try
                                {
                                    await binaryFile.Content.ReadAllAsync();
                                }
                                catch (OperationCanceledException)
                                {
                                    enumerationAborted = true;
                                    throw;
                                }
                            })
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var sequenceStore = broker.Consumers[0].GetCurrentSequenceStores()[0];
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3));

            await AsyncTestingUtil.WaitAsync(
                async () => sequenceStore.Count == 1 &&
                            (await sequenceStore.GetAsync<ChunkSequence>("1"))?.Length == 2);

            var sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            sequence.Should().NotBeNull();
            sequence!.Length.Should().Be(2);
            sequence!.IsAborted.Should().BeFalse();

            Broker.Disconnect();

            sequence.IsAborted.Should().BeTrue();
            sequenceStore.HasPendingSequences.Should().BeFalse();
            enumerationAborted.Should().BeTrue();

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);
        }

        [Fact]
        public async Task Chunking_JsonFromMultiplePartitions_ConcurrentlyConsumed()
        {
            const int chunksPerMessage = 3;

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(3)))
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
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await Enumerable.Range(1, 10).ForEachAsync(
                i =>
                    publisher.PublishAsync(
                        new TestEventOne
                        {
                            Content = $"Long message {i}"
                        }));

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(10);
            Subscriber.InboundEnvelopes.Should().HaveCount(10);

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(10 * chunksPerMessage);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(10);

            SpyBehavior.InboundEnvelopes
                .Select(envelope => ((TestEventOne)envelope.Message!).Content)
                .Should().BeEquivalentTo(Enumerable.Range(1, 10).Select(i => $"Long message {i}"));
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_BinaryFilesFromMultiplePartitions_ConcurrentlyConsumed()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_SingleChunkJson_ProducedAndConsumed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_SingleChunkBinaryFile_ProducedAndConsumed()
        {
            var message1 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                    }),
                ContentType = "application/pdf"
            };

            var message2 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
                    }),
                ContentType = "text/plain"
            };

            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
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
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                receivedFiles.Add(binaryFile.Content.ReadAll());
                            })
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedFiles.Should().HaveCount(2);
            receivedFiles[0].Should().BeEquivalentTo(message1.Content.ReReadAll());
            receivedFiles[1].Should().BeEquivalentTo(message2.Content.ReReadAll());

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(2);
        }

        [Fact]
        public async Task Chunking_SingleChunkBinaryFileReadAborted_CommittedAndNextMessageConsumed()
        {
            var message1 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                    }),
                ContentType = "application/pdf"
            };

            var message2 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                        0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x40,
                        0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x50
                    }),
                ContentType = "text/plain"
            };

            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
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
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                if (binaryFile.ContentType != "text/plain")
                                {
                                    // Read only first chunk
                                    var buffer = new byte[10];
                                    binaryFile.Content!.Read(buffer, 0, 10);
                                    return;
                                }

                                receivedFiles.Add(binaryFile.Content.ReadAll());
                            })
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(1);

            await publisher.PublishAsync(message2);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);

            SpyBehavior.InboundEnvelopes[0].Message.As<BinaryFileMessage>().ContentType.Should().Be("application/pdf");
            SpyBehavior.InboundEnvelopes[1].Message.As<BinaryFileMessage>().ContentType.Should().Be("text/plain");

            receivedFiles.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(4);
        }
    }
}
