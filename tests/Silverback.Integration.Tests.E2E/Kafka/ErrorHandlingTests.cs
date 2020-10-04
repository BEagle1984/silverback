// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Chunking;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    [Trait("Category", "E2E")]
    public class ErrorHandlingTests : E2ETestFixture
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        [Fact]
        public async Task RetryPolicy_ProcessingRetriedMultipleTimes()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var tryCount = 0;

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(10)
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await DefaultTopic.WaitUntilAllMessagesAreConsumed();

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            tryCount.Should().Be(3);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(3);
            SpyBehavior.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
        }

        [Fact]
        public async Task RetryPolicy_SuccessfulAfterSomeTries_OffsetCommitted()
        {
            var tryCount = 0;

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(10)
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await DefaultTopic.WaitUntilAllMessagesAreConsumed();

            tryCount.Should().Be(3);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(1);
        }

        [Fact]
        public async Task RetryPolicy_StillFailingAfterRetries_OffsetNotCommitted()
        {
            var tryCount = 0;

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(10)
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await DefaultTopic.WaitUntilAllMessagesAreConsumed();

            tryCount.Should().Be(11);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);
        }

        [Fact]
        public async Task RetryPolicy_StillFailingAfterRetries_ConsumerStopped()
        {
            var tryCount = 0;

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(10)
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await DefaultTopic.WaitUntilAllMessagesAreConsumed();

            tryCount.Should().Be(11); // TODO: Is this the expected behavior? Or should it stop at 10?
            serviceProvider.GetRequiredService<IBroker>().Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task RetryAndSkipPolicies_OffsetCommitted()
        {
            var tryCount = 0;

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        ErrorPolicy = ErrorPolicy.Chain(
                                            ErrorPolicy.Retry().MaxFailedAttempts(10),
                                            ErrorPolicy.Skip())
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await DefaultTopic.WaitUntilAllMessagesAreConsumed();

            tryCount.Should().Be(11);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(1);
        }

        [Fact]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task RetryPolicy_ChunkSequencesProcessedAfterSomeTries_RetriedMultipleTimesAndCommitted()
        {
            var tryCount = 0;

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
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(10)
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount != 2)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
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

            await DefaultTopic.WaitUntilAllMessagesAreConsumed();

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(6);
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
                });

            tryCount.Should().Be(4);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(4);
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Long message one");
            SpyBehavior.InboundEnvelopes[1].Message.As<TestEventOne>().Content.Should().Be("Long message one");
            SpyBehavior.InboundEnvelopes[2].Message.As<TestEventOne>().Content.Should().Be("Long message two");
            SpyBehavior.InboundEnvelopes[3].Message.As<TestEventOne>().Content.Should().Be("Long message two");

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(2);
        }

        [Fact]
        public async Task SkipPolicy_ChunkSequenceOrderingError_SequenceSkipped()
        {
            throw new NotImplementedException();
        }

        //
        // [Fact]
        // [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        // public async Task EncryptionWithRetries_RetriedMultipleTimes()
        // {
        //     var message = new TestEventOne
        //     {
        //         Content = "Hello E2E!"
        //     };
        //     var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
        //         message,
        //         new MessageHeaderCollection(),
        //         MessageSerializationContext.Empty);
        //     var tryCount = 0;
        //
        //     var serviceProvider = Host.ConfigureServices(
        //             services => services
        //                 .AddLogging()
        //                 .AddSilverback()
        //                 .UseModel()
        //                 .WithConnectionToMessageBroker(
        //                     options => options
        //                         .AddMockedKafka()
        //                         .AddInMemoryChunkStore())
        //                 .AddEndpoints(
        //                     endpoints => endpoints
        //                         .AddOutbound<IIntegrationEvent>(
        //                             new KafkaProducerEndpoint(DefaultTopicName)
        //                             {
        //                                 Encryption = new SymmetricEncryptionSettings
        //                                 {
        //                                     Key = AesEncryptionKey
        //                                 }
        //                             })
        //                         .AddInbound(
        //                             new KafkaConsumerEndpoint(DefaultTopicName)
        //                             {
        //                                 Encryption = new SymmetricEncryptionSettings
        //                                 {
        //                                     Key = AesEncryptionKey
        //                                 },
        //                                 ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(10)
        //                             }))
        //                 .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
        //                 .AddDelegateSubscriber(
        //                     (IIntegrationEvent _) =>
        //                     {
        //                         tryCount++;
        //                         if (tryCount != 3)
        //                             throw new InvalidOperationException("Retry!");
        //                     }))
        //         .Run();
        //
        //     var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
        //     await publisher.PublishAsync(message);
        //
        //     await DefaultTopic.WaitUntilAllMessagesAreConsumed();
        //
        //     SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
        //     SpyBehavior.OutboundEnvelopes[0].RawMessage.Should().NotBeEquivalentTo(rawMessage);
        //     SpyBehavior.InboundEnvelopes.Count.Should().Be(3);
        //     SpyBehavior.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
        // }
        //
        // [Fact]
        // [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        // public async Task EncryptionAndChunkingWithRetries_RetriedMultipleTimes()
        // {
        //     var message = new TestEventOne
        //     {
        //         Content = "Hello E2E!"
        //     };
        //
        //     var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
        //         message,
        //         new MessageHeaderCollection(),
        //         MessageSerializationContext.Empty);
        //
        //     var tryCount = 0;
        //
        //     var serviceProvider = Host.ConfigureServices(
        //             services => services
        //                 .AddLogging()
        //                 .AddSilverback()
        //                 .UseModel()
        //.WithConnectionToMessageBroker(options => options.AddMockedKafka())
        //                 .AddEndpoints(
        //                     endpoints => endpoints
        //                         .AddOutbound<IIntegrationEvent>(
        //                             new KafkaProducerEndpoint(DefaultTopicName)
        //                             {
        //                                 Chunk = new ChunkSettings
        //                                 {
        //                                     Size = 10
        //                                 },
        //                                 Encryption = new SymmetricEncryptionSettings
        //                                 {
        //                                     Key = AesEncryptionKey
        //                                 }
        //                             })
        //                         .AddInbound(
        //                             new KafkaConsumerEndpoint(DefaultTopicName)
        //                             {
        //                                 Encryption = new SymmetricEncryptionSettings
        //                                 {
        //                                     Key = AesEncryptionKey
        //                                 },
        //                                 ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(10)
        //                             }))
        //                 .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
        //                 .AddDelegateSubscriber(
        //                     (IIntegrationEvent _) =>
        //                     {
        //                         tryCount++;
        //                         if (tryCount != 3)
        //                             throw new InvalidOperationException("Retry!");
        //                     }))
        //         .Run();
        //
        //     var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
        //     await publisher.PublishAsync(message);
        //
        //     SpyBehavior.OutboundEnvelopes.Count.Should().Be(5);
        //     SpyBehavior.OutboundEnvelopes[0].RawMessage.Should().NotBeEquivalentTo(rawMessage.Read(10));
        //     SpyBehavior.OutboundEnvelopes.ForEach(
        //         envelope =>
        //         {
        //             envelope.RawMessage.Should().NotBeNull();
        //             envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
        //         });
        //     SpyBehavior.InboundEnvelopes.Count.Should().Be(3);
        //     SpyBehavior.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
        // }
    }
}
