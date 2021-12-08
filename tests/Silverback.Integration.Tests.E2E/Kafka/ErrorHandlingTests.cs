// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class ErrorHandlingTests : KafkaTestFixture
{
    private static readonly byte[] AesEncryptionKey = BytesUtil.GetRandomBytes(32);

    public ErrorHandlingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        // TODO: Test rollback always called with all kind of policies
    }

    [Fact]
    public async Task RetryPolicy_ProcessingRetriedMultipleTimes()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        int tryCount = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(10))
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (IIntegrationEvent _) =>
                        {
                            tryCount++;
                            throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        tryCount.Should().Be(11);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(11);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
    }

    [Fact]
    public async Task RetryPolicy_SuccessfulAfterSomeTries_OffsetCommitted()
    {
        int tryCount = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(10))
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (IIntegrationEvent _) =>
                        {
                            tryCount++;
                            if (tryCount != 3)
                                throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(3);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);
    }

    [Fact]
    public async Task RetryPolicy_StillFailingAfterRetries_OffsetNotCommitted()
    {
        int tryCount = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(10))
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (IIntegrationEvent _) =>
                        {
                            tryCount++;
                            throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(11);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
    }

    [Fact]
    public async Task
        RetryPolicy_JsonChunkSequenceProcessedAfterSomeTries_RetriedMultipleTimesAndCommitted()
    {
        int tryCount = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(10))
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (IIntegrationEvent _) =>
                        {
                            tryCount++;
                            if (tryCount % 2 != 0)
                                throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Long message one" });
        await publisher.PublishAsync(new TestEventOne { Content = "Long message two" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.RawOutboundEnvelopes.ForEach(
            envelope =>
            {
                envelope.RawMessage.Should().NotBeNull();
                envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
            });

        tryCount.Should().Be(4);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Long message one");
        Helper.Spy.InboundEnvelopes[1].Message.As<TestEventOne>().Content.Should().Be("Long message one");
        Helper.Spy.InboundEnvelopes[2].Message.As<TestEventOne>().Content.Should().Be("Long message two");
        Helper.Spy.InboundEnvelopes[3].Message.As<TestEventOne>().Content.Should().Be("Long message two");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
    }

    [Fact]
    public async Task RetryPolicy_BinaryMessageChunkSequenceProcessedAfterSomeTries_RetriedMultipleTimesAndCommitted()
    {
        BinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(30),
            ContentType = "application/pdf"
        };

        BinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(30),
            ContentType = "text/plain"
        };

        int tryCount = 0;
        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<BinaryMessage>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(10))
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                        {
                            if (binaryMessage.ContentType != "text/plain")
                            {
                                tryCount++;

                                if (tryCount != 2)
                                {
                                    // Read first chunk only
                                    byte[] buffer = new byte[10];
                                    binaryMessage.Content!.Read(buffer, 0, 10);
                                    throw new InvalidOperationException("Retry!");
                                }
                            }

                            lock (receivedFiles)
                            {
                                receivedFiles.Add(binaryMessage.Content.ReadAll());
                            }
                        })
                    .AddIntegrationSpy())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(2);

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.RawOutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.Should().Be(10));
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

        Helper.Spy.InboundEnvelopes[0].Message.As<BinaryMessage>().ContentType.Should().Be("application/pdf");
        Helper.Spy.InboundEnvelopes[1].Message.As<BinaryMessage>().ContentType.Should().Be("application/pdf");
        Helper.Spy.InboundEnvelopes[2].Message.As<BinaryMessage>().ContentType.Should().Be("text/plain");

        receivedFiles.Should().HaveCount(2);
        receivedFiles[0].Should().BeEquivalentTo(message1.Content.ReReadAll());
        receivedFiles[1].Should().BeEquivalentTo(message2.Content.ReReadAll());

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
    }

    [Fact]
    public async Task RetryPolicy_StillFailingAfterRetries_ConsumerStopped()
    {
        int tryCount = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(10))
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (IIntegrationEvent _) =>
                        {
                            tryCount++;
                            throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        await AsyncTestingUtil.WaitAsync(() => !Helper.Broker.Consumers[0].IsConnected);

        tryCount.Should().Be(11);
        Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task RetryAndSkipPolicies_StillFailingAfterRetries_OffsetCommitted()
    {
        int tryCount = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(10).ThenSkip())
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (IIntegrationEvent _) =>
                        {
                            tryCount++;
                            throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(11);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);
    }

    [Fact]
    public async Task RetryAndSkipPolicies_JsonChunkSequenceStillFailingAfterRetries_OffsetCommitted()
    {
        int tryCount = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(10).ThenSkip())
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (IIntegrationEvent _) =>
                        {
                            tryCount++;
                            throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(11);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);
    }

    [Fact]
    public async Task SkipPolicy_JsonDeserializationError_MessageSkipped()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        byte[] invalidRawMessage = Encoding.UTF8.GetBytes("<what?!>");

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
        await producer.RawProduceAsync(
            invalidRawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().BeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);

        await producer.RawProduceAsync(
            rawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
    }

    [Fact]
    public async Task SkipPolicy_ChunkedJsonDeserializationError_SequenceSkipped()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        byte[] invalidRawMessage = Encoding.UTF8.GetBytes("<what?!><what?!><what?!>");

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Skip())
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
        await producer.RawProduceAsync(
            invalidRawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            invalidRawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            invalidRawMessage.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, true, typeof(TestEventOne)));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().BeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 2, true, typeof(TestEventOne)));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
    }

    [Fact]
    public async Task SkipPolicy_BatchJsonDeserializationError_MessageSkipped()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);
        byte[] invalidRawMessage = Encoding.UTF8.GetBytes("<what?!>");
        List<List<TestEventOne>> receivedBatches = new();
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .EnableBatchProcessing(5)
                                    .OnError(policy => policy.Skip())
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                        {
                            List<TestEventOne> list = new();
                            receivedBatches.Add(list);

                            await foreach (TestEventOne testEvent in eventsStream)
                            {
                                list.Add(testEvent);
                            }

                            completedBatches++;
                        }))
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
        await producer.RawProduceAsync(
            invalidRawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().BeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);

        for (int i = 0; i < 6; i++)
        {
            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
                });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 6);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(5);
        receivedBatches[1].Should().HaveCount(1);
        completedBatches.Should().Be(1);

        await producer.RawProduceAsync(
            invalidRawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        for (int i = 0; i < 4; i++)
        {
            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
                });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(5);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(12);
    }

    [Fact]
    public async Task RetryPolicy_EncryptedMessage_RetriedMultipleTimes()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        Stream rawMessage = DefaultSerializers.Json.Serialize(message);
        int tryCount = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EncryptUsingAes(AesEncryptionKey))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(10))
                                    .DecryptUsingAes(AesEncryptionKey)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (IIntegrationEvent _) =>
                        {
                            tryCount++;
                            if (tryCount != 3)
                                throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.ReadAll().Should()
            .NotBeEquivalentTo(rawMessage.ReReadAll());
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
    }

    [Fact]
    public async Task RetryPolicy_EncryptedAndChunkedMessage_RetriedMultipleTimes()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        Stream rawMessage = DefaultSerializers.Json.Serialize(message);
        int tryCount = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10)
                                    .EncryptUsingAes(AesEncryptionKey))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(10))
                                    .DecryptUsingAes(AesEncryptionKey)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (IIntegrationEvent _) =>
                        {
                            tryCount++;
                            if (tryCount != 3)
                                throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.RawOutboundEnvelopes[0].RawMessage.ReReadAll().Should()
            .NotBeEquivalentTo(rawMessage.Read(10));
        Helper.Spy.RawOutboundEnvelopes.ForEach(
            envelope =>
            {
                envelope.RawMessage.Should().NotBeNull();
                envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
            });
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
    }

    [Fact]
    public async Task RetryPolicy_BatchThrowingWhileEnumerating_RetriedMultipleTimes()
    {
        int tryMessageCount = 0;
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(10))
                                    .EnableBatchProcessing(2)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<IIntegrationEvent> events) =>
                        {
                            await foreach (IIntegrationEvent dummy in events)
                            {
                                tryMessageCount++;
                                if (tryMessageCount != 2 && tryMessageCount != 4 && tryMessageCount != 5)
                                    throw new InvalidOperationException($"Retry {tryMessageCount}!");
                            }

                            completedBatches++;
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(5);

        completedBatches.Should().Be(1);
    }

    [Fact]
    public async Task RetryPolicy_BatchThrowingAfterEnumerationCompleted_RetriedMultipleTimes()
    {
        int tryCount = 0;
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(10))
                                    .EnableBatchProcessing(2)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<IIntegrationEvent> events) =>
                        {
                            await foreach (IIntegrationEvent dummy in events)
                            {
                            }

                            tryCount++;
                            if (tryCount != 3)
                                throw new InvalidOperationException("Retry!");

                            completedBatches++;
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(6);

        completedBatches.Should().Be(1);
    }

    [Fact]
    public async Task MovePolicy_ToOtherTopic_MessageMoved()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.MoveToKafkaTopic(moveEndpoint => moveEndpoint.ProduceTo("other-topic")))
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber((IIntegrationEvent _) => throw new InvalidOperationException("Move!")))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);

        Helper.Spy.OutboundEnvelopes[1].Message.Should()
            .BeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].Endpoint.RawName.Should().Be("other-topic");

        IInMemoryTopic otherTopic = Helper.GetTopic("other-topic");
        otherTopic.MessagesCount.Should().Be(1);
        otherTopic.GetAllMessages()[0].Value.Should()
            .BeEquivalentTo(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
    }

    [Fact]
    public async Task MovePolicy_ToSameTopic_MessageMovedAndRetried()
    {
        TestEventOne message = new()
        {
            Content = "Hello E2E!"
        };
        int tryCount = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(
                                        policy => policy.MoveToKafkaTopic(
                                            moveEndpoint => moveEndpoint.ProduceTo(DefaultTopicName),
                                            movePolicy => movePolicy.MaxFailedAttempts(10)))
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (IIntegrationEvent _) =>
                        {
                            tryCount++;
                            throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(11);
        tryCount.Should().Be(11);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(11);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
    }

    [Fact]
    public async Task MovePolicy_ToOtherTopicAfterRetry_MessageRetriedAndMoved()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        int tryCount = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(
                                        policy => policy
                                            .Retry(1)
                                            .ThenMoveToKafkaTopic(moveEndpoint => moveEndpoint.ProduceTo("other-topic")))
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (IIntegrationEvent _) =>
                        {
                            tryCount++;
                            throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        tryCount.Should().Be(2);

        Helper.Spy.OutboundEnvelopes[1].Message.Should()
            .BeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].Endpoint.RawName.Should().Be("other-topic");

        IInMemoryTopic otherTopic = Helper.GetTopic("other-topic");
        otherTopic.MessagesCount.Should().Be(1);
        otherTopic.GetAllMessages()[0].Value.Should()
            .BeEquivalentTo(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
    }

    [Fact]
    public async Task RetryPolicy_WithMultiplePartitions_ProcessingRetriedMultipleTimes()
    {
        int tryCount = 0;
        int consumedCount = 0;
        SemaphoreSlim semaphore = new(0);

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.Content))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(2).ThenSkip())
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (TestEventOne message) =>
                        {
                            if (message.Content != "1")
                            {
                                Interlocked.Increment(ref consumedCount);
                                return;
                            }

                            tryCount++;

                            semaphore.Wait();
                            semaphore.Release();
                            throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });

        await AsyncTestingUtil.WaitAsync(() => tryCount == 1);

        semaphore.Release();
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        tryCount.Should().Be(3);
        consumedCount.Should().Be(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);
    }

    [Fact]
    public async Task RetryPolicy_ProcessingAllPartitionsTogether_ProcessingRetriedMultipleTimes()
    {
        int tryCount = 0;
        int consumedCount = 0;
        SemaphoreSlim semaphore = new(0);

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.Content))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(2).ThenSkip())
                                    .ProcessAllPartitionsTogether()
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (TestEventOne message) =>
                        {
                            if (message.Content != "1")
                            {
                                Interlocked.Increment(ref consumedCount);
                                return;
                            }

                            tryCount++;

                            semaphore.Wait();
                            semaphore.Release();
                            throw new InvalidOperationException("Retry!");
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });

        await AsyncTestingUtil.WaitAsync(() => tryCount == 1);

        semaphore.Release();
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        await AsyncTestingUtil.WaitAsync(() => tryCount == 3, TimeSpan.FromMinutes(2));

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        tryCount.Should().Be(3);
        consumedCount.Should().Be(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);
    }

    [Fact]
    public async Task MovePolicy_ToOtherTopic_HeadersSet()
    {
        TestEventOne message1 = new()
        {
            Content = "Hello E2E msg1."
        };
        TestEventOne message2 = new()
        {
            Content = "Hello E2E msg2."
        };
        TestEventOne message3 = new()
        {
            Content = "Hello E2E msg3."
        };

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.MoveToKafkaTopic(moveEndpoint => moveEndpoint.ProduceTo("other-topic")))
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber((IIntegrationEvent _) => throw new InvalidOperationException("Move!")))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await publisher.PublishAsync(message3);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

        IInMemoryTopic otherTopic = Helper.GetTopic("other-topic");
        IReadOnlyList<Message<byte[]?, byte[]?>> otherTopicMessages = otherTopic.GetAllMessages();
        otherTopic.MessagesCount.Should().Be(3);

        otherTopicMessages[0].Value.Should()
            .BeEquivalentTo(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
        otherTopicMessages[1].Value.Should()
            .BeEquivalentTo(Helper.Spy.InboundEnvelopes[1].RawMessage.ReReadAll());
        otherTopicMessages[2].Value.Should()
            .BeEquivalentTo(Helper.Spy.InboundEnvelopes[2].RawMessage.ReReadAll());

        Helper.Spy.OutboundEnvelopes[5].Endpoint.RawName.Should().Be("other-topic");
        Helper.Spy.OutboundEnvelopes[5].Headers
            .Should().ContainEquivalentOf(
                new MessageHeader(
                    DefaultMessageHeaders.FailedAttempts,
                    1));
        Helper.Spy.OutboundEnvelopes[5].Headers
            .Should().ContainEquivalentOf(
                new MessageHeader(
                    DefaultMessageHeaders.FailureReason,
                    "System.InvalidOperationException in Silverback.Integration.Tests.E2E"));
        Helper.Spy.OutboundEnvelopes[5].Headers
            .Count(header => header.Name == KafkaMessageHeaders.SourceTimestamp)
            .Should().Be(1);
        Helper.Spy.OutboundEnvelopes[5].Headers
            .Any(header => header.Name == KafkaMessageHeaders.TimestampKey)
            .Should().BeTrue();
        Helper.Spy.OutboundEnvelopes[5].Headers
            .Should().ContainEquivalentOf(
                new MessageHeader(
                    KafkaMessageHeaders.SourcePartition,
                    0));
        Helper.Spy.OutboundEnvelopes[5].Headers
            .Should().ContainEquivalentOf(
                new MessageHeader(
                    KafkaMessageHeaders.SourceOffset,
                    2));
    }
}
