﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
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
    public class ChunkingTests : KafkaTestFixture
    {
        public ChunkingTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Chunking_Json_ProducedAndConsumed()
        {
            const int chunksPerMessage = 3;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"Long message {i}" });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(5);
            Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(5 * chunksPerMessage);
            Helper.Spy.RawOutboundEnvelopes.ForEach(
                envelope => envelope.RawMessage.ReReadAll()!.Length.Should().BeLessOrEqualTo(10));
            Helper.Spy.InboundEnvelopes.Should().HaveCount(5);

            for (int i = 0; i < Helper.Spy.RawOutboundEnvelopes.Count; i++)
            {
                int firstEnvelopeIndex = i / chunksPerMessage * chunksPerMessage;
                var firstEnvelope = Helper.Spy.RawOutboundEnvelopes[firstEnvelopeIndex];
                var lastEnvelope = Helper.Spy.RawOutboundEnvelopes[firstEnvelopeIndex + chunksPerMessage - 1];
                var envelope = Helper.Spy.RawOutboundEnvelopes[i];

                envelope.Headers.GetValue(DefaultMessageHeaders.ChunksCount).Should()
                    .Be(chunksPerMessage.ToString(CultureInfo.InvariantCulture));

                if (envelope == firstEnvelope)
                {
                    envelope.Headers.GetValue(DefaultMessageHeaders.FirstChunkOffset).Should().BeNull();
                }
                else
                {
                    envelope.Headers.GetValue(DefaultMessageHeaders.FirstChunkOffset).Should()
                        .Be(firstEnvelope.BrokerMessageIdentifier!.Value);
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

            Helper.Spy.InboundEnvelopes
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IBinaryFileMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                            })))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                receivedFiles.Add(binaryFile.Content.ReadAll());
                            })
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(6);
            Helper.Spy.RawOutboundEnvelopes.ForEach(
                envelope => envelope.RawMessage.ReReadAll()!.Length.Should().BeLessOrEqualTo(10));
            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.InboundEnvelopes.ForEach(
                envelope => envelope.Message.Should().BeOfType<BinaryFileMessage>());

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(
                new[]
                {
                    message1.Content.ReReadAll(),
                    message2.Content.ReReadAll()
                });
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, false, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, true, typeof(TestEventOne)));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3, typeof(TestEventOne)));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 2, 3, typeof(TestEventOne)));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileWithIsLastChunkHeader_Consumed()
        {
            var rawMessage = Encoding.UTF8.GetBytes("Hello E2E!");
            var receivedFiles = new List<byte[]?>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .ConsumeBinaryFiles()
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                                receivedFiles.Add(binaryFile.Content.ReadAll())))
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.RawProduceAsync(
                rawMessage.Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, false));
            await producer.RawProduceAsync(
                rawMessage.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1));
            await producer.RawProduceAsync(
                rawMessage.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, true));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(rawMessage);

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileWithChunksCountHeader_Consumed()
        {
            var rawMessage = Encoding.UTF8.GetBytes("Hello E2E!");
            var receivedFiles = new List<byte[]?>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .ConsumeBinaryFiles()
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                                receivedFiles.Add(binaryFile.Content.ReadAll())))
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.RawProduceAsync(
                rawMessage.Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(rawMessage);

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileWithMessageIdHeader_Consumed()
        {
            var rawMessage = Encoding.UTF8.GetBytes("Hello E2E!");
            var receivedFiles = new List<byte[]?>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .ConsumeBinaryFiles()
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                                receivedFiles.Add(binaryFile.Content.ReadAll())))
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.RawProduceAsync(
                rawMessage.Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 0, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 1, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 2, 3));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(rawMessage);

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileWithMessageTypeHeader_Consumed()
        {
            var rawMessage = Encoding.UTF8.GetBytes("Hello E2E!");
            var receivedFiles = new List<byte[]?>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                                receivedFiles.Add(binaryFile.Content.ReadAll())))
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.RawProduceAsync(
                rawMessage.Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 0, 3, typeof(BinaryFileMessage)));
            await producer.RawProduceAsync(
                rawMessage.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 1, 3, typeof(BinaryFileMessage)));
            await producer.RawProduceAsync(
                rawMessage.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 2, 3, typeof(BinaryFileMessage)));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(rawMessage);

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));

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

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.InboundEnvelopes
                .Select(envelope => envelope.Message.As<TestEventOne>().Content)
                .Should().BeEquivalentTo("Message 1", "Message 2");

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileWithDuplicatedChunks_DuplicatesIgnored()
        {
            byte[] rawMessage1 = Encoding.UTF8.GetBytes("Message 1");
            byte[] rawMessage2 = Encoding.UTF8.GetBytes("Message 2");
            var receivedFiles = new List<byte[]?>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .ConsumeBinaryFiles()
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                                receivedFiles.Add(binaryFile.Content.ReadAll())))
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));

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

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(new[] { rawMessage1, rawMessage2 });

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IBinaryFileMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                            })))
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
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

            await publisher.PublishAsync(message2);
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

            Helper.Spy.InboundEnvelopes[0].Message.As<BinaryFileMessage>().ContentType.Should()
                .Be("application/pdf");
            Helper.Spy.InboundEnvelopes[1].Message.As<BinaryFileMessage>().ContentType.Should()
                .Be("text/plain");

            receivedFiles.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IBinaryFileMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                            })))
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
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

            await publisher.PublishAsync(message2);
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

            Helper.Spy.InboundEnvelopes[0].Message.As<BinaryFileMessage>().ContentType.Should()
                .Be("application/pdf");
            Helper.Spy.InboundEnvelopes[1].Message.As<BinaryFileMessage>().ContentType.Should()
                .Be("text/plain");

            receivedFiles.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
        }

        [Fact]
        public async Task Chunking_BinaryFileProcessingFailedAfterFirstChunk_DisconnectedAndNotCommitted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IBinaryFileMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                            })))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                // Read only first chunk
                                var buffer = new byte[10];
                                binaryFile.Content!.Read(buffer, 0, 10);

                                throw new InvalidOperationException("Test");
                            })
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

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

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
            Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileProcessingFailedImmediately_DisconnectedAndNotCommitted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IBinaryFileMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                            })))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage _) => throw new InvalidOperationException("Test"))
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

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

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
            Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task
            Chunking_EnforcingConsecutiveJsonChunks_IncompleteSequenceDiscardedWhenOtherSequenceStarts()
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
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

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
        }

        [Fact]
        public async Task
            Chunking_EnforcingConsecutiveJsonChunks_IncompleteSequenceDiscardedOnNoSequenceMessage()
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.RawProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2,
                HeadersHelper.GetHeaders("6", typeof(TestEventOne)));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
        }

        [Fact]
        public async Task
            Chunking_EnforcingConsecutiveJsonChunks_ErrorPolicyIgnoredWhenIncompleteSequenceDiscarded()
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(5))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
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

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));

            await AsyncTestingUtil.WaitAsync(
                () => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) > 0,
                TimeSpan.FromMilliseconds(200));
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);

            var sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
            var originalSequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            originalSequence.Should().NotBeNull();
            originalSequence!.IsComplete.Should().BeFalse();
            originalSequence.IsAborted.Should().BeFalse();

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3, typeof(TestEventOne)));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

            originalSequence.IsAborted.Should().BeTrue();
            sequenceStore.GetPendingSequences().Should().BeEmpty();
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .WithSequenceTimeout(TimeSpan.FromMilliseconds(500))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));

            await AsyncTestingUtil.WaitAsync(
                () => Helper.Broker.Consumers[0].SequenceStores.Count >= 1 &&
                      Helper.Broker.Consumers[0].SequenceStores.Single().GetPendingSequences().Any());

            await Task.Delay(200);

            var sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
            var sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            sequence.Should().NotBeNull();
            sequence!.IsAborted.Should().BeFalse();

            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));

            await Task.Delay(300);
            sequence.IsAborted.Should().BeFalse();

            await AsyncTestingUtil.WaitAsync(
                () => !sequence.IsPending && DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) >= 2,
                TimeSpan.FromSeconds(1));
            sequence.IsAborted.Should().BeTrue();

            sequenceStore.GetPendingSequences().Should().BeEmpty();

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 2, 3, typeof(TestEventOne)));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

            sequenceStore.GetPendingSequences().Should().BeEmpty();
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .ConsumeBinaryFiles()
                                        .WithSequenceTimeout(TimeSpan.FromMilliseconds(500))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
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
                        .AddIntegrationSpy())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0));

            await AsyncTestingUtil.WaitAsync(
                () => Helper.Broker.Consumers[0].SequenceStores.Count >= 1 &&
                      Helper.Broker.Consumers[0].SequenceStores.Single().GetPendingSequences().Any());

            await Task.Delay(200);

            var sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
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
            sequenceStore.GetPendingSequences().Should().BeEmpty();
            enumerationAborted.Should().BeTrue();

            await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) >= 2);
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 0));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 1));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 2, true));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
            receivedFiles.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(rawMessage);
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

            sequenceStore.GetPendingSequences().Should().BeEmpty();
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
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

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
        }

        [Fact]
        public async Task Chunking_BinaryFileMissingFirstChunk_NextMessageConsumedAndCommitted()
        {
            var rawMessage1 = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
            };
            var rawMessage2 = new byte[]
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x40,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x50
            };

            var receivedFiles = new List<byte[]?>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                                receivedFiles.Add(binaryFile.Content.ReadAll())))
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.RawProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, typeof(BinaryFileMessage)));
            await producer.RawProduceAsync(
                rawMessage1.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, true, typeof(BinaryFileMessage)));
            await producer.RawProduceAsync(
                rawMessage2.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 0, typeof(BinaryFileMessage)));
            await producer.RawProduceAsync(
                rawMessage2.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 1, typeof(BinaryFileMessage)));
            await producer.RawProduceAsync(
                rawMessage2.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 2, true, typeof(BinaryFileMessage)));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            receivedFiles.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(rawMessage2);

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

            var sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
            sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpy())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));

            await AsyncTestingUtil.WaitAsync(
                async () => Helper.Broker.Consumers[0].SequenceStores.Count >= 1 &&
                            Helper.Broker.Consumers[0].SequenceStores.Single().Count == 1 &&
                            (await Helper.Broker.Consumers[0].SequenceStores.Single()
                                .GetAsync<ChunkSequence>("1"))?.Length == 2);

            var sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
            var sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            sequence.Should().NotBeNull();
            sequence!.Length.Should().Be(2);
            sequence.IsAborted.Should().BeFalse();

            await Helper.Broker.DisconnectAsync();

            sequence.IsAborted.Should().BeTrue();
            sequenceStore.GetPendingSequences().Should().BeEmpty();
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .ConsumeBinaryFiles()
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
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
                        .AddIntegrationSpy())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3));

            await AsyncTestingUtil.WaitAsync(
                async () => Helper.Broker.Consumers[0].SequenceStores.Count >= 1 &&
                            Helper.Broker.Consumers[0].SequenceStores.Single().Count == 1 &&
                            (await Helper.Broker.Consumers[0].SequenceStores.Single()
                                .GetAsync<ChunkSequence>("1"))?.Length == 2);

            var sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
            var sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            sequence.Should().NotBeNull();
            sequence!.Length.Should().Be(2);
            sequence.IsAborted.Should().BeFalse();

            await Helper.Broker.DisconnectAsync();

            sequence.IsAborted.Should().BeTrue();
            sequenceStore.GetPendingSequences().Should().BeEmpty();
            enumerationAborted.Should().BeTrue();

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
        }

        [Fact]
        public async Task Chunking_RebalanceWithIncompleteJson_AbortedAndNotCommitted()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpy())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));

            await AsyncTestingUtil.WaitAsync(
                async () => Helper.Broker.Consumers[0].SequenceStores.Count >= 1 &&
                            Helper.Broker.Consumers[0].SequenceStores.Single().Count == 1 &&
                            (await Helper.Broker.Consumers[0].SequenceStores.Single()
                                .GetAsync<ChunkSequence>("1"))?.Length == 2);

            var sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
            var sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            sequence.Should().NotBeNull();
            sequence!.Length.Should().Be(2);
            sequence.IsAborted.Should().BeFalse();

            DefaultConsumerGroup.Rebalance();

            await AsyncTestingUtil.WaitAsync(() => sequence.IsAborted);

            sequence.IsAborted.Should().BeTrue();
            sequenceStore.GetPendingSequences().Should().BeEmpty();
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
        }

        [Fact]
        public async Task Chunking_RebalanceWithIncompleteBinaryFile_AbortedAndNotCommitted()
        {
            var rawMessage = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
            };

            bool enumerationAborted = false;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .ConsumeBinaryFiles()
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
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
                        .AddIntegrationSpy())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3));

            await AsyncTestingUtil.WaitAsync(
                async () => Helper.Broker.Consumers[0].SequenceStores.Count >= 1 &&
                            Helper.Broker.Consumers[0].SequenceStores.Single().Count == 1 &&
                            (await Helper.Broker.Consumers[0].SequenceStores.Single()
                                .GetAsync<ChunkSequence>("1"))?.Length == 2);

            var sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
            var sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
            sequence.Should().NotBeNull();
            sequence!.Length.Should().Be(2);
            sequence.IsAborted.Should().BeFalse();

            DefaultConsumerGroup.Rebalance();

            await AsyncTestingUtil.WaitAsync(() => sequence.IsAborted && enumerationAborted);

            sequence.IsAborted.Should().BeTrue();
            sequenceStore.GetPendingSequences().Should().BeEmpty();
            enumerationAborted.Should().BeTrue();

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
        }

        [Fact]
        public async Task Chunking_JsonFromMultiplePartitions_ConcurrentlyConsumed()
        {
            const int messagesCount = 10;
            const int chunksPerMessage = 3;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(3)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= messagesCount; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"Long message {i}" });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(messagesCount);
            Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(messagesCount * chunksPerMessage);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(messagesCount);
            Helper.Spy.InboundEnvelopes
                .Select(envelope => ((TestEventOne)envelope.Message!).Content)
                .Should().BeEquivalentTo(Enumerable.Range(1, messagesCount).Select(i => $"Long message {i}"));
        }

        [Fact]
        public async Task Chunking_BinaryFilesFromMultiplePartitions_ConcurrentlyConsumed()
        {
            var rawMessage1 = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
            };
            var rawMessage2 = new byte[]
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x40,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x50
            };
            var rawMessage3 = new byte[]
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x50,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20
            };

            int receivedFilesCount = 0;
            var receivedFiles = new List<byte[]?>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(3)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .ConsumeBinaryFiles()
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                            })))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                Interlocked.Increment(ref receivedFilesCount);

                                byte[]? fileContent = binaryFile.Content.ReadAll();

                                lock (receivedFiles)
                                {
                                    receivedFiles.Add(fileContent);
                                }
                            }))
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));

            await producer.RawProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3));
            await producer.RawProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3));
            await producer.RawProduceAsync(
                rawMessage2.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 0));
            await producer.RawProduceAsync(
                rawMessage2.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 1));
            await producer.RawProduceAsync(
                rawMessage3.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("3", 0));
            await producer.RawProduceAsync(
                rawMessage3.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("3", 1));

            await AsyncTestingUtil.WaitAsync(() => receivedFilesCount == 3);

            receivedFilesCount.Should().Be(3);
            receivedFiles.Should().BeEmpty();

            await producer.RawProduceAsync(
                rawMessage3.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("3", 2, true));
            await producer.RawProduceAsync(
                rawMessage2.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 2, true));
            await producer.RawProduceAsync(
                rawMessage1.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            receivedFilesCount.Should().Be(3);
            receivedFiles.Should().HaveCount(3);
            receivedFiles.Should().BeEquivalentTo(new[] { rawMessage1, rawMessage2, rawMessage3 });
        }

        [Fact]
        public async Task Chunking_SingleChunkJson_ProducedAndConsumed()
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(50))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));

            await producer.RawProduceAsync(
                rawMessage1.ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 1, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage2.ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, true, typeof(TestEventOne)));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 1");
            Helper.Spy.InboundEnvelopes[1].Message.As<TestEventOne>().Content.Should().Be("Message 2");
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IBinaryFileMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                            })))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                lock (receivedFiles)
                                {
                                    receivedFiles.Add(binaryFile.Content.ReadAll());
                                }
                            })
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            receivedFiles.Should().HaveCount(2);
            receivedFiles[0].Should().BeEquivalentTo(message1.Content.ReReadAll());
            receivedFiles[1].Should().BeEquivalentTo(message2.Content.ReReadAll());

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IBinaryFileMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                            })))
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
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);

            await publisher.PublishAsync(message2);
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

            Helper.Spy.InboundEnvelopes[0].Message.As<BinaryFileMessage>().ContentType.Should()
                .Be("application/pdf");
            Helper.Spy.InboundEnvelopes[1].Message.As<BinaryFileMessage>().ContentType.Should()
                .Be("text/plain");

            receivedFiles.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(4);
        }
    }
}
