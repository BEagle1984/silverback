// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class ErrorHandlingTests : E2ETestFixture
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        public ErrorHandlingTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            // TODO: Test rollback always called with all kind of policies
        }

        [Fact]
        public async Task RetryPolicy_ProcessingRetriedMultipleTimes()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(1);
            tryCount.Should().Be(11);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(11);
            SpyBehavior.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
        }

        [Fact]
        public async Task RetryPolicy_SuccessfulAfterSomeTries_OffsetCommitted()
        {
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            tryCount.Should().Be(3);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(1);
        }

        [Fact]
        public async Task RetryPolicy_StillFailingAfterRetries_OffsetNotCommitted()
        {
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            tryCount.Should().Be(11);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);
        }

        [Fact]
        public async Task RetryPolicy_JsonChunkSequenceProcessedAfterSomeTries_RetriedMultipleTimesAndCommitted()
        {
            var tryCount = 0;

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
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount % 2 != 0)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Long message one"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Long message two"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(6);
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
                });

            tryCount.Should().Be(4);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(4);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Long message one");
            SpyBehavior.InboundEnvelopes[1].Message.As<TestEventOne>().Content.Should().Be("Long message one");
            SpyBehavior.InboundEnvelopes[2].Message.As<TestEventOne>().Content.Should().Be("Long message two");
            SpyBehavior.InboundEnvelopes[3].Message.As<TestEventOne>().Content.Should().Be("Long message two");

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(6);
        }

        [Fact]
        public async Task RetryPolicy_BinaryFileChunkSequenceProcessedAfterSomeTries_RetriedMultipleTimesAndCommitted()
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

            var tryCount = 0;
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
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IBinaryFileMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                if (binaryFile.ContentType != "text/plain")
                                {
                                    tryCount++;

                                    if (tryCount != 2)
                                    {
                                        // Read only first chunk
                                        var buffer = new byte[10];
                                        binaryFile.Content!.Read(buffer, 0, 10);
                                        throw new InvalidOperationException("Retry!");
                                    }
                                }

                                lock (receivedFiles)
                                {
                                    receivedFiles.Add(binaryFile.Content.ReadAll());
                                }
                            })
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            tryCount.Should().Be(2);

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(6);
            SpyBehavior.OutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.Should().Be(10));
            SpyBehavior.InboundEnvelopes.Should().HaveCount(3);

            SpyBehavior.InboundEnvelopes[0].Message.As<BinaryFileMessage>().ContentType.Should().Be("application/pdf");
            SpyBehavior.InboundEnvelopes[1].Message.As<BinaryFileMessage>().ContentType.Should().Be("application/pdf");
            SpyBehavior.InboundEnvelopes[2].Message.As<BinaryFileMessage>().ContentType.Should().Be("text/plain");

            receivedFiles.Should().HaveCount(2);
            receivedFiles[0].Should().BeEquivalentTo(message1.Content.ReReadAll());
            receivedFiles[1].Should().BeEquivalentTo(message2.Content.ReReadAll());

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(6);
        }

        [Fact]
        public async Task RetryPolicy_StillFailingAfterRetries_ConsumerStopped()
        {
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            await AsyncTestingUtil.WaitAsync(() => !Broker.Consumers[0].IsConnected);

            tryCount.Should().Be(11);
            Broker.Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task RetryAndSkipPolicies_StillFailingAfterRetries_OffsetCommitted()
        {
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10).ThenSkip())
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            tryCount.Should().Be(11);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(1);
        }

        [Fact]
        public async Task RetryAndSkipPolicies_JsonChunkSequenceStillFailingAfterRetries_OffsetCommitted()
        {
            var tryCount = 0;

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
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10).ThenSkip())
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _, IServiceProvider sp) =>
                            {
                                var logger = sp.GetRequiredService<ISilverbackLogger<ErrorHandlingTests>>();
                                tryCount++;
                                logger.LogInformation($"Handling message ({tryCount})...");
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            tryCount.Should().Be(11);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);
        }

        [Fact]
        public async Task SkipPolicy_JsonDeserializationError_MessageSkipped()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

            byte[] invalidRawMessage = Encoding.UTF8.GetBytes("<what?!>");

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
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var producer = Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.RawProduceAsync(
                invalidRawMessage,
                new MessageHeaderCollection
                {
                    { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
                });
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().BeEmpty();
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(1);

            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
                });
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(1);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(2);
        }

        [Fact]
        public async Task SkipPolicy_ChunkedJsonDeserializationError_SequenceSkipped()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

            byte[] invalidRawMessage = Encoding.UTF8.GetBytes("<what?!><what?!><what?!>");

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
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var producer = Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.RawProduceAsync(
                invalidRawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                invalidRawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                invalidRawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, true, typeof(TestEventOne)));
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().BeEmpty();
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 1, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("2", 2, true, typeof(TestEventOne)));
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(1);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(6);
        }

        [Fact]
        public async Task RetryPolicy_EncryptedMessage_RetriedMultipleTimes()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessageStream = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))
                                        .DecryptUsingAes(AesEncryptionKey)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.ReadAll().Should()
                .NotBeEquivalentTo(rawMessageStream.ReReadAll());
            SpyBehavior.InboundEnvelopes.Should().HaveCount(3);
            SpyBehavior.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
        }

        [Fact]
        public async Task RetryPolicy_EncryptedAndChunkedMessage_RetriedMultipleTimes()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10)
                                        .EncryptUsingAes(AesEncryptionKey))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))
                                        .DecryptUsingAes(AesEncryptionKey)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(6);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.ReReadAll().Should().NotBeEquivalentTo(rawMessage.Read(10));
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
                });
            SpyBehavior.InboundEnvelopes.Should().HaveCount(3);
            SpyBehavior.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
        }

        [Fact]
        public async Task MovePolicy_ToOtherTopic_MessageMoved()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(
                                            policy => policy.MoveToKafkaTopic(
                                                moveEndpoint => moveEndpoint.ProduceTo("other-topic")))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber((IIntegrationEvent _) => throw new InvalidOperationException("Move!")))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);

            SpyBehavior.OutboundEnvelopes[1].Message.Should().BeEquivalentTo(SpyBehavior.OutboundEnvelopes[0].Message);
            SpyBehavior.OutboundEnvelopes[1].Endpoint.Name.Should().Be("other-topic");

            GetTopic("other-topic").TotalMessagesCount.Should().Be(1);
        }

        [Fact]
        public async Task MovePolicy_ToSameTopic_MessageMovedAndRetried()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(
                                            policy => policy.MoveToKafkaTopic(
                                                moveEndpoint => moveEndpoint.ProduceTo(DefaultTopicName),
                                                movePolicy => movePolicy.MaxFailedAttempts(10)))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(11);
            tryCount.Should().Be(11);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(11);
            SpyBehavior.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
        }

        [Fact]
        public async Task MovePolicy_ToOtherTopicAfterRetry_MessageRetriedAndMoved()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(
                                            policy => policy
                                                .Retry(1)
                                                .ThenMoveToKafkaTopic(
                                                moveEndpoint => moveEndpoint.ProduceTo("other-topic")))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);

            tryCount.Should().Be(2);

            SpyBehavior.OutboundEnvelopes[1].Message.Should().BeEquivalentTo(SpyBehavior.OutboundEnvelopes[0].Message);
            SpyBehavior.OutboundEnvelopes[1].Endpoint.Name.Should().Be("other-topic");

            GetTopic("other-topic").TotalMessagesCount.Should().Be(1);
        }
    }
}
