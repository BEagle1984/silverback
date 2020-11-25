// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
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
        public async Task Encryption_SimpleMessages_EncryptedAndDecrypted()
        {
            var message1 = new TestEventOne { Content = "Message 1" };
            var message2 = new TestEventOne { Content = "Message 2" };

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
                                .Configure(clientConfig => { clientConfig.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
            SpyBehavior.OutboundEnvelopes[1].RawMessage.Should().BeOfType<SymmetricEncryptStream>();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
            SpyBehavior.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(2);
        }

        [Fact]
        public async Task Encryption_ChunkedMessages_EncryptedAndDecrypted()
        {
            var message1 = new TestEventOne { Content = "Message 1" };
            var rawMessageStream1 = await Endpoint.DefaultSerializer.SerializeAsync(
                message1,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var message2 = new TestEventOne { Content = "Message 2" };
            var rawMessageStream2 = await Endpoint.DefaultSerializer.SerializeAsync(
                message2,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

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
                                .Configure(clientConfig => { clientConfig.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10)
                                        .EncryptUsingAes(AesEncryptionKey))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(12);

            for (int i = 0; i < 6; i++)
            {
                SpyBehavior.OutboundEnvelopes[i].RawMessage.Should().NotBeNull();
                SpyBehavior.OutboundEnvelopes[i].RawMessage!.Length.Should().BeLessOrEqualTo(10);
                SpyBehavior.OutboundEnvelopes[i].RawMessage.ReReadAll().Should()
                    .NotBeEquivalentTo(rawMessageStream1.ReReadAll()!.Skip(i * 10).Take(10));
            }

            for (int i = 0; i < 6; i++)
            {
                SpyBehavior.OutboundEnvelopes[i + 6].RawMessage.Should().NotBeNull();
                SpyBehavior.OutboundEnvelopes[i + 6].RawMessage!.Length.Should().BeLessOrEqualTo(10);
                SpyBehavior.OutboundEnvelopes[i + 6].RawMessage.ReReadAll().Should()
                    .NotBeEquivalentTo(rawMessageStream2.ReReadAll()!.Skip(i * 10).Take(10));
            }

            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
            SpyBehavior.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(12);
        }

        [Fact]
        public async Task Encryption_BinaryFile_EncryptedAndDecrypted()
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
                    services =>
                    {
                        services
                            .AddLogging()
                            .AddSilverback()
                            .UseModel()
                            .WithConnectionToMessageBroker(
                                options => options.AddMockedKafka(
                                    mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                            .AddKafkaEndpoints(
                                endpoints => endpoints
                                    .Configure(clientConfig => { clientConfig.BootstrapServers = "PLAINTEXT://e2e"; })
                                    .AddOutbound<IBinaryFileMessage>(
                                        endpoint => endpoint
                                            .ProduceTo(DefaultTopicName)
                                            .EncryptUsingAes(AesEncryptionKey))
                                    .AddInbound(
                                        endpoint => endpoint
                                            .ConsumeFrom(DefaultTopicName)
                                            .DecryptUsingAes(AesEncryptionKey)
                                            .Configure(
                                                config =>
                                                {
                                                    config.GroupId = "consumer1";
                                                    config.AutoCommitIntervalMs = 50;
                                                })))
                            .AddDelegateSubscriber(
                                (BinaryFileMessage binaryFile) => receivedFiles.Add(binaryFile.Content.ReadAll()))
                            .AddSingletonBrokerBehavior<SpyBrokerBehavior>();
                    })
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
            SpyBehavior.OutboundEnvelopes[1].RawMessage.Should().BeOfType<SymmetricEncryptStream>();

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(message1.Content.ReReadAll(), message2.Content.ReReadAll());
        }

        [Fact]
        public async Task Encryption_ChunkedBinaryFile_EncryptedAndDecrypted()
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
                    services =>
                    {
                        services
                            .AddLogging()
                            .AddSilverback()
                            .UseModel()
                            .WithConnectionToMessageBroker(
                                options => options.AddMockedKafka(
                                    mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                            .AddKafkaEndpoints(
                                endpoints => endpoints
                                    .Configure(clientConfig => { clientConfig.BootstrapServers = "PLAINTEXT://e2e"; })
                                    .AddOutbound<IBinaryFileMessage>(
                                        endpoint => endpoint
                                            .ProduceTo(DefaultTopicName)
                                            .EnableChunking(10)
                                            .EncryptUsingAes(AesEncryptionKey))
                                    .AddInbound(
                                        endpoint => endpoint
                                            .ConsumeFrom(DefaultTopicName)
                                            .DecryptUsingAes(AesEncryptionKey)
                                            .Configure(
                                                config =>
                                                {
                                                    config.GroupId = "consumer1";
                                                    config.AutoCommitIntervalMs = 50;
                                                })))
                            .AddDelegateSubscriber(
                                (BinaryFileMessage binaryFile) => receivedFiles.Add(binaryFile.Content.ReadAll()))
                            .AddSingletonBrokerBehavior<SpyBrokerBehavior>();
                    })
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(12);

            for (int i = 0; i < 6; i++)
            {
                SpyBehavior.OutboundEnvelopes[i].RawMessage.Should().NotBeNull();
                SpyBehavior.OutboundEnvelopes[i].RawMessage!.Length.Should().BeLessOrEqualTo(10);
                SpyBehavior.OutboundEnvelopes[i].RawMessage.ReReadAll().Should()
                    .NotBeEquivalentTo(message1.Content.ReReadAll()!.Skip(i * 10).Take(10));
            }

            for (int i = 0; i < 6; i++)
            {
                SpyBehavior.OutboundEnvelopes[i + 6].RawMessage.Should().NotBeNull();
                SpyBehavior.OutboundEnvelopes[i + 6].RawMessage!.Length.Should().BeLessOrEqualTo(10);
                SpyBehavior.OutboundEnvelopes[i + 6].RawMessage.ReReadAll().Should()
                    .NotBeEquivalentTo(message2.Content.ReReadAll()!.Skip(i * 10).Take(10));
            }

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(message1.Content.ReReadAll(), message2.Content.ReReadAll());
        }
    }
}
