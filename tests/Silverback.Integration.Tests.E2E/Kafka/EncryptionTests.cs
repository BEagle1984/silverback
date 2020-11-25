// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
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
    public class EncryptionTests : E2ETestFixture
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        public EncryptionTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Encryption_SimpleMessage_EncryptedAndDecrypted()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessageStream = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

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
                                        Encryption = new SymmetricEncryptionSettings
                                        {
                                            Key = AesEncryptionKey
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
                                        Encryption = new SymmetricEncryptionSettings
                                        {
                                            Key = AesEncryptionKey
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.ReadAll().Should()
                .NotBeEquivalentTo(rawMessageStream.ReReadAll());
            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(1);
        }

        [Fact]
        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task Encryption_ChunkedMessage_EncryptedAndDecrypted()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

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
                                        },
                                        Encryption = new SymmetricEncryptionSettings
                                        {
                                            Key = AesEncryptionKey
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
                                        Encryption = new SymmetricEncryptionSettings
                                        {
                                            Key = AesEncryptionKey
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(6);
            for (int i = 0; i < SpyBehavior.OutboundEnvelopes.Count; i++)
            {
                SpyBehavior.OutboundEnvelopes[i].RawMessage.Should().NotBeNull();
                SpyBehavior.OutboundEnvelopes[i].RawMessage!.Length.Should().BeLessOrEqualTo(10);
                SpyBehavior.OutboundEnvelopes[i].RawMessage.ReReadAll().Should()
                    .NotBeEquivalentTo(rawMessage.ReReadAll()!.Skip(i * 10).Take(10));
            }

            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));

            Subscriber.InboundEnvelopes.Should().HaveCount(1);
            Subscriber.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(6);
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Encryption_BinaryFile_EncryptedAndDecrypted()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Encryption_ChunkedBinaryFile_EncryptedAndDecrypted()
        {
            throw new NotImplementedException();
        }
    }
}
