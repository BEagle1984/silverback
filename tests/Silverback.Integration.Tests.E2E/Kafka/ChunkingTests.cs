// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace Silverback.Tests.Integration.E2E.Kafka
{
    [Trait("Category", "E2E")]
    public class ChunkingTests : E2ETestFixture
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        [Fact]
        public async Task Chunking_Json_ProducedAndConsumed()
        {
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

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Count.Should().Be(5);
            Subscriber.InboundEnvelopes.Count.Should().Be(5);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(15);
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope => envelope.RawMessage.ReReadAll()!.Length.Should().BeLessOrEqualTo(10));
            SpyBehavior.InboundEnvelopes.Count.Should().Be(5);

            for (int i = 0; i < SpyBehavior.OutboundEnvelopes.Count; i++)
            {
                var firstEnvelope = SpyBehavior.OutboundEnvelopes[(i / 3) * 3];
                var envelope = SpyBehavior.OutboundEnvelopes[i];

                if (envelope == firstEnvelope)
                {
                    envelope.Headers.GetValue(DefaultMessageHeaders.FirstChunkOffset).Should().BeNull();
                }
                else
                {
                    envelope.Headers.GetValue(DefaultMessageHeaders.FirstChunkOffset).Should()
                        .Be(firstEnvelope.Offset!.Value);
                }
            }

            var receivedContents =
                SpyBehavior.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).Content);

            receivedContents.Should()
                .BeEquivalentTo(Enumerable.Range(1, 5).Select(i => $"Long message {i}"));
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(6);
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope => envelope.RawMessage.ReReadAll()!.Length.Should().BeLessOrEqualTo(10));
            SpyBehavior.InboundEnvelopes.Count.Should().Be(2);

            SpyBehavior.InboundEnvelopes[0].Message.As<BinaryFileMessage>().ContentType.Should().Be("application/pdf");
            SpyBehavior.InboundEnvelopes[1].Message.As<BinaryFileMessage>().ContentType.Should().Be("text/plain");

            receivedFiles.Should().HaveCount(2);
            receivedFiles[0].Should().BeEquivalentTo(message1.Content.ReReadAll());
            receivedFiles[1].Should().BeEquivalentTo(message2.Content.ReReadAll());
        }

        [Fact]
        public async Task Chunking_BinaryFileReadAborted_NextMessageConsumed()
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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
            await publisher.PublishAsync(message2);

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(6);
            SpyBehavior.OutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.Should().Be(10));
            SpyBehavior.InboundEnvelopes.Count.Should().Be(2);

            SpyBehavior.InboundEnvelopes[0].Message.As<BinaryFileMessage>().ContentType.Should().Be("application/pdf");
            SpyBehavior.InboundEnvelopes[1].Message.As<BinaryFileMessage>().ContentType.Should().Be("text/plain");

            receivedFiles.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());
        }

        [Fact]
        public async Task Chunking_Json_AtomicallyCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_BinaryFile_AtomicallyCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_Json_BackpressureHandled()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_BinaryFile_BackpressureHandled()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_NonConsecutiveJsonChunks_IncompleteSequenceIsDiscarded()
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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
                                            ConsecutiveMessages = true
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.ProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("1", 0, 3));
            await producer.ProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("1", 1, 3));
            await producer.ProduceAsync(
                rawMessage2.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("2", 0, 3));
            await producer.ProduceAsync(
                rawMessage2.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("2", 1, 3));
            await producer.ProduceAsync(
                rawMessage2.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("2", 2, 3));

            await AsyncTestingUtil.WaitAsync(() => SpyBehavior.InboundEnvelopes.Count == 1);

            Subscriber.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(5);

            var sequenceStore = broker.Consumers[0].GetSequenceStore(new KafkaOffset(DefaultTopicName, 0, 0));
            sequenceStore.HasPendingSequences.Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_NonConsecutiveJsonChunks_ConsumedAndCommittedOnlyWhenSequencesAreComplete()
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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
                                            ConsecutiveMessages = false
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.ProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("1", 0, 3));
            await producer.ProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("1", 1, 3));
            await producer.ProduceAsync(
                rawMessage2.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("2", 0, 3));
            await producer.ProduceAsync(
                rawMessage2.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("2", 1, 3));
            await producer.ProduceAsync(
                rawMessage2.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("2", 2, 3));

            await AsyncTestingUtil.WaitAsync(() => SpyBehavior.InboundEnvelopes.Count == 1);

            Subscriber.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);

            await producer.ProduceAsync(
                rawMessage1.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("1", 2, 3));

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Count.Should().Be(2);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(2);
            SpyBehavior.InboundEnvelopes[1].Message.As<TestEventOne>().Content.Should().Be("Message 1");
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(6);
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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
            var sequenceStore = broker.Consumers[0].GetSequenceStore(new KafkaOffset(DefaultTopicName, 0, 0));
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));

            await producer.ProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("1", 0, 3));
            await producer.ProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("1", 1, 3));

            await AsyncTestingUtil.WaitAsync(() => DefaultTopic.GetCommittedOffsetsCount("consumer1") > 0, 200);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);

            var originalSequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            originalSequence.Should().NotBeNull();
            originalSequence!.IsComplete.Should().BeFalse();
            originalSequence!.IsAborted.Should().BeFalse();

            await producer.ProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("1", 0, 3));
            await producer.ProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("1", 1, 3));
            await producer.ProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("1", 2, 3));

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(5);

            originalSequence.IsAborted.Should().BeTrue();
            sequenceStore.HasPendingSequences.Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_IncompleteJson_AbortedAfterTimeoutAndCommittedWithNextMessage()
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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
            var sequenceStore = broker.Consumers[0].GetSequenceStore(new KafkaOffset(DefaultTopicName, 0, 0));
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));

            await producer.ProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("1", 0, 3));

            await AsyncTestingUtil.WaitAsync(() => sequenceStore.HasPendingSequences);

            await Task.Delay(200);

            var sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            sequence.Should().NotBeNull();
            sequence!.IsAborted.Should().BeFalse();

            await producer.ProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("1", 1, 3));

            await Task.Delay(300);
            sequence.IsAborted.Should().BeFalse();

            await AsyncTestingUtil.WaitAsync(() => !sequence.IsPending, 1000);
            sequence.IsAborted.Should().BeTrue();

            sequenceStore.HasPendingSequences.Should().BeFalse();

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);

            await producer.ProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("2", 0, 3));
            await producer.ProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("2", 1, 3));
            await producer.ProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("2", 2, 3));

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(5);

            sequenceStore.HasPendingSequences.Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_IncompleteBinaryFile_AbortedAfterTimeoutAndCommittedWithNextMessage()
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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
            var sequenceStore = broker.Consumers[0].GetSequenceStore(new KafkaOffset(DefaultTopicName, 0, 0));
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));

            await producer.ProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<BinaryFileMessage>("1", 0, 3));

            await AsyncTestingUtil.WaitAsync(() => sequenceStore.HasPendingSequences);

            await Task.Delay(200);

            var sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            sequence.Should().NotBeNull();
            sequence!.IsAborted.Should().BeFalse();

            await producer.ProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<BinaryFileMessage>("1", 1, 3));

            await Task.Delay(300);
            sequence.IsAborted.Should().BeFalse();

            await AsyncTestingUtil.WaitAsync(() => !sequence.IsPending && enumerationAborted, 1000);
            sequence.IsAborted.Should().BeTrue();
            sequenceStore.HasPendingSequences.Should().BeFalse();
            enumerationAborted.Should().BeTrue();

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);

            await producer.ProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<BinaryFileMessage>("2", 0, 3));
            await producer.ProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<BinaryFileMessage>("2", 1, 3));
            await producer.ProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<BinaryFileMessage>("2", 2, 3));

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Count.Should().Be(2);
            receivedFiles.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(rawMessage);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(5);

            sequenceStore.HasPendingSequences.Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_IncompleteBinaryFile_NextMessageConsumedAndCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_JsonMissingFirstChunk_NextMessageConsumedAndCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_BinaryFileMissingFirstChunk_NextMessageConsumedAndCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_MultipleSequencesFromMultiplePartitions_ConcurrentlyConsumed()
        {
            throw new NotImplementedException();
        }

        // TODO: Test with concurrent consumers
    }
}
