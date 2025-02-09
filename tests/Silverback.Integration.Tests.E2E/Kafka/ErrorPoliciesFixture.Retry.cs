// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ErrorPoliciesFixture
{
    [Fact]
    public async Task RetryPolicy_ShouldRetryProcessingMultipleTimes()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent dummy)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(1);
        tryCount.ShouldBe(11);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(11);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.ShouldBeEquivalentTo(message));
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryProcessingMultipleTimes_WhenProcessingMultiplePartitions()
    {
        int tryCount = 0;
        int consumedCount = 0;
        SemaphoreSlim semaphore = new(0);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(2).ThenSkip()))))
                .AddDelegateSubscriber<TestEventOne>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(TestEventOne message)
        {
            if (message.ContentEventOne is not "3" and not "17")
            {
                Interlocked.Increment(ref consumedCount);
                return;
            }

            tryCount++;

            semaphore.Wait();
            semaphore.Release();
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        for (int i = 1; i <= 10; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = i.ToString(CultureInfo.InvariantCulture) });
        }

        await AsyncTestingUtil.WaitAsync(() => tryCount == 1);
        semaphore.Release();

        for (int i = 11; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = i.ToString(CultureInfo.InvariantCulture) });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(20);
        tryCount.ShouldBe(6);
        consumedCount.ShouldBe(18);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(18 + 6);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryProcessingMultipleTimes_WhenProcessingMultiplePartitionsAllTogether()
    {
        int tryCount = 0;
        int consumedCount = 0;
        SemaphoreSlim semaphore = new(0);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .ProcessAllPartitionsTogether()
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(2).ThenSkip()))))
                .AddDelegateSubscriber<TestEventOne>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(TestEventOne message)
        {
            if (message.ContentEventOne is not "3" and not "17")
            {
                Interlocked.Increment(ref consumedCount);
                return;
            }

            tryCount++;

            semaphore.Wait();
            semaphore.Release();
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        for (int i = 1; i <= 10; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = i.ToString(CultureInfo.InvariantCulture) });
        }

        await AsyncTestingUtil.WaitAsync(() => tryCount == 1);
        semaphore.Release();

        for (int i = 11; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = i.ToString(CultureInfo.InvariantCulture) });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(20);
        tryCount.ShouldBe(6);
        consumedCount.ShouldBe(18);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(18 + 6);
    }

    [Fact]
    public async Task RetryPolicy_ShouldCommit_WhenSuccessfulAfterSomeRetries()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            if (tryCount != 3)
                throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(3);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);
    }

    [Fact]
    public async Task RetryPolicy_ShouldNotCommit_WhenStillFailingAfterRetries()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(11);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryChunkedJsonMultipleTimes()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            if (tryCount % 2 != 0)
                throw new InvalidOperationException("Retry!");
        }

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message three" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message four" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(16);
        Helper.Spy.RawOutboundEnvelopes.ForEach(
            envelope =>
            {
                envelope.RawMessage.ShouldNotBeNull();
                envelope.RawMessage.Length.ShouldBeLessThanOrEqualTo(10);
            });

        tryCount.ShouldBe(8);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(8);
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message one");
        Helper.Spy.InboundEnvelopes[1].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message one");
        Helper.Spy.InboundEnvelopes[2].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message two");
        Helper.Spy.InboundEnvelopes[3].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message two");
        Helper.Spy.InboundEnvelopes[4].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message three");
        Helper.Spy.InboundEnvelopes[5].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message three");
        Helper.Spy.InboundEnvelopes[6].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message four");
        Helper.Spy.InboundEnvelopes[7].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message four");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(16);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryChunkedJsonMultipleTimes_WhenProcessingPartitionIndependently()
    {
        Counter[] tryCounters = new Counter[4];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<TestIndexedMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(TestIndexedMessage message)
        {
            int tryCount = tryCounters[message.Index].Increment();
            if (tryCount <= 2)
                throw new InvalidOperationException("Retry!");
        }

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        for (int i = 0; i < 4; i++)
        {
            tryCounters[i] = new Counter();
            await publisher.PublishAsync(new TestIndexedMessage(i, "Long message"));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(16);
        tryCounters.ShouldAllBe(tryCount => tryCount.Value == 3);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(12);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(16);
    }

    [SuppressMessage("ReSharper", "MustUseReturnValue", Justification = "Test code")]
    [Fact]
    public async Task RetryPolicy_ShouldRetryChunkedBinaryMessageMultipleTimes()
    {
        BinaryMessage message1 = new() { Content = BytesUtil.GetRandomStream(30), ContentType = "application/pdf" };
        BinaryMessage message2 = new() { Content = BytesUtil.GetRandomStream(30), ContentType = "text/plain" };

        int tryCount = 0;
        TestingCollection<byte[]?> receivedFiles = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<BinaryMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage)
        {
            if (binaryMessage.ContentType != "text/plain")
            {
                tryCount++;

                if (tryCount != 2)
                {
                    // Read first chunk only
                    byte[] buffer = new byte[10];
                    binaryMessage.Content!.ReadExactly(buffer, 0, 10);
                    throw new InvalidOperationException("Retry!");
                }
            }

            receivedFiles.Add(binaryMessage.Content.ReadAll());
        }

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(2);

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(6);
        Helper.Spy.RawOutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.ShouldBe(10));
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);

        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<BinaryMessage>().ContentType.ShouldBe("application/pdf");
        Helper.Spy.InboundEnvelopes[1].Message.ShouldBeOfType<BinaryMessage>().ContentType.ShouldBe("application/pdf");
        Helper.Spy.InboundEnvelopes[2].Message.ShouldBeOfType<BinaryMessage>().ContentType.ShouldBe("text/plain");

        receivedFiles.Count.ShouldBe(2);
        receivedFiles[0].ShouldBe(message1.Content.ReReadAll());
        receivedFiles[1].ShouldBe(message2.Content.ReReadAll());

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(6);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryEncryptedMessageMultipleTimes()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        Stream rawMessage = await DefaultSerializers.Json.SerializeAsync(message);
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            if (tryCount != 3)
                throw new InvalidOperationException("Retry!");
        }

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.ReadAll().ShouldNotBe(rawMessage.ReReadAll());
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.ShouldBeEquivalentTo(message));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryEncryptedChunkedMessageMultipleTimes()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        Stream rawMessage = await DefaultSerializers.Json.SerializeAsync(message);
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10)
                                        .EncryptUsingAes(AesEncryptionKey)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            if (tryCount != 3)
                throw new InvalidOperationException("Retry!");
        }

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(8);
        Helper.Spy.RawOutboundEnvelopes[0].RawMessage.ReReadAll().ShouldNotBe(rawMessage.Read(10));
        Helper.Spy.RawOutboundEnvelopes.ForEach(
            envelope =>
            {
                envelope.RawMessage.ShouldNotBeNull();
                envelope.RawMessage!.Length.ShouldBeLessThanOrEqualTo(10);
            });
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.ShouldBeEquivalentTo(message));
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryBatchMultipleTimes_WhenThrowingWhileEnumerating()
    {
        int tryMessageCount = 0;
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(2)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IAsyncEnumerable<IIntegrationEvent>>(HandleBatch)
                .AddIntegrationSpy());

        async ValueTask HandleBatch(IAsyncEnumerable<IIntegrationEvent> batch)
        {
            await foreach (IIntegrationEvent dummy in batch)
            {
                tryMessageCount++;
                if (tryMessageCount is not 2 and not 4 and not 5)
                    throw new InvalidOperationException($"Retry {tryMessageCount}!");
            }

            completedBatches++;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(5);

        completedBatches.ShouldBe(1);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryBatchMultipleTimes_WhenProcessingPartitionsTogetherAndThrowingWhileEnumerating()
    {
        int tryMessageCount = 0;
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .ProcessAllPartitionsTogether()
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(2)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IAsyncEnumerable<IIntegrationEvent>>(HandleBatch)
                .AddIntegrationSpy());

        async ValueTask HandleBatch(IAsyncEnumerable<IIntegrationEvent> batch)
        {
            await foreach (IIntegrationEvent dummy in batch)
            {
                tryMessageCount++;
                if (tryMessageCount is not 2 and not 4 and not 5)
                    throw new InvalidOperationException($"Retry {tryMessageCount}!");
            }

            completedBatches++;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(5);

        completedBatches.ShouldBe(1);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryBatchMultipleTimes_WhenStaticAssignmentAndProcessingPartitionsTogetherAndThrowingWhileEnumerating()
    {
        int tryMessageCount = 0;
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .ProcessAllPartitionsTogether()
                                .Consume(
                                    "endpoint1",
                                    endpoint => endpoint
                                        .ConsumeFrom(
                                            new TopicPartition(DefaultTopicName, 0),
                                            new TopicPartition(DefaultTopicName, 1),
                                            new TopicPartition(DefaultTopicName, 2))
                                        .EnableBatchProcessing(2)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IAsyncEnumerable<IIntegrationEvent>>(HandleBatch)
                .AddIntegrationSpy());

        async ValueTask HandleBatch(IAsyncEnumerable<IIntegrationEvent> batch)
        {
            await foreach (IIntegrationEvent dummy in batch)
            {
                tryMessageCount++;
                if (tryMessageCount is not 2 and not 4 and not 5)
                    throw new InvalidOperationException($"Retry {tryMessageCount}!");
            }

            completedBatches++;
        }

        IProducer producer = Helper.GetProducerForEndpoint("endpoint1");
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(5);

        completedBatches.ShouldBe(1);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryBatchMultipleTimes_WhenThrowingAfterEnumerationCompleted()
    {
        int tryCount = 0;
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(2)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IAsyncEnumerable<IIntegrationEvent>>(HandleBatch)
                .AddIntegrationSpy());

        async ValueTask HandleBatch(IAsyncEnumerable<IIntegrationEvent> batch)
        {
            await foreach (IIntegrationEvent dummy in batch)
            {
                // Do nothing
            }

            tryCount++;
            if (tryCount != 3)
                throw new InvalidOperationException("Retry!");

            completedBatches++;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(6);

        completedBatches.ShouldBe(1);
    }

    [Fact]
    public async Task RetryPolicy_ShouldStopConsumer_WhenStillFailingAfterRetries()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(11);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }
}
