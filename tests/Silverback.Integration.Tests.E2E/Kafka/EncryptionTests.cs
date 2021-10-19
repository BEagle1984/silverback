// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class EncryptionTests : KafkaTestFixture
    {
        private static readonly byte[] AesEncryptionKey = BytesUtil.GetRandomBytes(32);
        private static readonly byte[] AesEncryptionKey2 = BytesUtil.GetRandomBytes(32);

        public EncryptionTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Encryption_SimpleMessages_EncryptedAndDecrypted()
        {
            TestEventOne message1 = new() { Content = "Message 1" };
            TestEventOne message2 = new() { Content = "Message 2" };

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
                                .ConfigureClient(
                                    configuration =>
                                    {
                                        configuration.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    producer => producer
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey))
                                .AddInbound(
                                    consumer => consumer
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey)
                                        .ConfigureClient(
                                            configuration =>
                                            {
                                                configuration.GroupId = DefaultConsumerGroupId;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.OutboundEnvelopes[0].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
            Helper.Spy.OutboundEnvelopes[1].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
            Helper.Spy.OutboundEnvelopes.ForEach(
                envelope =>
                    envelope.Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().BeNull());

            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
            Helper.Spy.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
            Helper.Spy.InboundEnvelopes.ForEach(
                envelope =>
                    envelope.Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().BeNull());

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
        }

        [Fact]
        public async Task Encryption_SimpleMessagesWithKeyRotation_EncryptedAndDecrypted()
        {
            TestEventOne message1 = new() { Content = "Message 1" };
            TestEventTwo message2 = new() { Content = "Message 2" };
            const string keyIdentifier1 = "my-encryption-key-1";
            const string keyIdentifier2 = "my-encryption-key-2";

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
                                .ConfigureClient(
                                    configuration =>
                                    {
                                        configuration.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<TestEventOne>(
                                    producer => producer
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey, keyIdentifier1))
                                .AddOutbound<TestEventTwo>(
                                    producer => producer
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey2, keyIdentifier2))
                                .AddInbound(
                                    consumer => consumer
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(
                                            keyIdentifier =>
                                            {
                                                switch (keyIdentifier)
                                                {
                                                    case keyIdentifier1:
                                                        return AesEncryptionKey;
                                                    default:
                                                        return AesEncryptionKey2;
                                                }
                                            })
                                        .ConfigureClient(
                                            configuration =>
                                            {
                                                configuration.GroupId = DefaultConsumerGroupId;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.OutboundEnvelopes[0].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
            Helper.Spy.OutboundEnvelopes[0].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().Be(keyIdentifier1);
            Helper.Spy.OutboundEnvelopes[1].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
            Helper.Spy.OutboundEnvelopes[1].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().Be(keyIdentifier2);

            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
            Helper.Spy.InboundEnvelopes[0].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().Be(keyIdentifier1);
            Helper.Spy.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
            Helper.Spy.InboundEnvelopes[1].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().Be(keyIdentifier2);

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
        }

        [Fact]
        public async Task Encryption_SimpleMessagesNoKeyIdentifier_EncryptedAndDecrypted()
        {
            TestEventOne message1 = new() { Content = "Message 1" };
            TestEventTwo message2 = new() { Content = "Message 2" };

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
                                .ConfigureClient(
                                    configuration =>
                                    {
                                        configuration.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    producer => producer
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey))
                                .AddInbound(
                                    consumer => consumer
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(
                                            keyIdentifier =>
                                            {
                                                return keyIdentifier switch
                                                {
                                                    "another-encryption-key-id" => AesEncryptionKey2,
                                                    _ => AesEncryptionKey
                                                };
                                            })
                                        .ConfigureClient(
                                            configuration =>
                                            {
                                                configuration.GroupId = DefaultConsumerGroupId;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.OutboundEnvelopes[0].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
            Helper.Spy.OutboundEnvelopes[0].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should()
                .BeNull();
            Helper.Spy.OutboundEnvelopes[1].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
            Helper.Spy.OutboundEnvelopes[1].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should()
                .BeNull();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
            Helper.Spy.InboundEnvelopes[0].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should()
                .BeNull();
            Helper.Spy.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
            Helper.Spy.InboundEnvelopes[1].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should()
                .BeNull();

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
        }

        [Fact]
        public async Task Encryption_ChunkedMessages_EncryptedAndDecrypted()
        {
            TestEventOne message1 = new() { Content = "Message 1" };
            Stream rawMessageStream1 = EndpointConfiguration.DefaultSerializer.Serialize(message1);

            TestEventOne message2 = new() { Content = "Message 2" };
            Stream rawMessageStream2 = EndpointConfiguration.DefaultSerializer.Serialize(message2);

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
                                .ConfigureClient(
                                    configuration =>
                                    {
                                        configuration.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    producer => producer
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10)
                                        .EncryptUsingAes(AesEncryptionKey))
                                .AddInbound(
                                    consumer => consumer
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey)
                                        .ConfigureClient(
                                            configuration =>
                                            {
                                                configuration.GroupId = DefaultConsumerGroupId;
                                                configuration.EnableAutoCommit = false;
                                                configuration.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(12);

            for (int i = 0; i < 6; i++)
            {
                Helper.Spy.RawOutboundEnvelopes[i].RawMessage.Should().NotBeNull();
                Helper.Spy.RawOutboundEnvelopes[i].RawMessage!.Length.Should().BeLessOrEqualTo(10);
                Helper.Spy.RawOutboundEnvelopes[i].RawMessage.ReReadAll().Should()
                    .NotBeEquivalentTo(rawMessageStream1.ReReadAll()!.Skip(i * 10).Take(10));
            }

            for (int i = 0; i < 6; i++)
            {
                Helper.Spy.RawOutboundEnvelopes[i + 6].RawMessage.Should().NotBeNull();
                Helper.Spy.RawOutboundEnvelopes[i + 6].RawMessage!.Length.Should().BeLessOrEqualTo(10);
                Helper.Spy.RawOutboundEnvelopes[i + 6].RawMessage.ReReadAll().Should()
                    .NotBeEquivalentTo(rawMessageStream2.ReReadAll()!.Skip(i * 10).Take(10));
            }

            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
            Helper.Spy.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(12);
        }

        [Fact]
        public async Task Encryption_BinaryFile_EncryptedAndDecrypted()
        {
            BinaryFileMessage message1 = new()
            {
                Content = BytesUtil.GetRandomStream(),
                ContentType = "application/pdf"
            };

            BinaryFileMessage message2 = new()
            {
                Content = BytesUtil.GetRandomStream(),
                ContentType = "text/plain"
            };

            List<byte[]?> receivedFiles = new();

            Host.ConfigureServices(
                    services =>
                    {
                        services
                            .AddLogging()
                            .AddSilverback()
                            .UseModel()
                            .WithConnectionToMessageBroker(
                                options => options
                                    .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                            .AddKafkaEndpoints(
                                endpoints => endpoints
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.BootstrapServers = "PLAINTEXT://e2e";
                                        })
                                    .AddOutbound<IBinaryFileMessage>(
                                        endpoint => endpoint
                                            .ProduceTo(DefaultTopicName)
                                            .EncryptUsingAes(AesEncryptionKey))
                                    .AddInbound(
                                        endpoint => endpoint
                                            .ConsumeFrom(DefaultTopicName)
                                            .DecryptUsingAes(AesEncryptionKey)
                                            .ConfigureClient(
                                                configuration =>
                                                {
                                                    configuration.GroupId = DefaultConsumerGroupId;
                                                })))
                            .AddDelegateSubscriber(
                                (BinaryFileMessage binaryFile) =>
                                    receivedFiles.Add(binaryFile.Content.ReadAll()))
                            .AddIntegrationSpy();
                    })
                .Run();

            IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.OutboundEnvelopes[0].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
            Helper.Spy.OutboundEnvelopes[1].RawMessage.Should().BeOfType<SymmetricEncryptStream>();

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(
                new[]
                {
                    message1.Content.ReReadAll(),
                    message2.Content.ReReadAll()
                });
        }

        [Fact]
        public async Task Encryption_ChunkedBinaryFile_EncryptedAndDecrypted()
        {
            BinaryFileMessage message1 = new()
            {
                Content = BytesUtil.GetRandomStream(30),
                ContentType = "application/pdf"
            };

            BinaryFileMessage message2 = new()
            {
                Content = BytesUtil.GetRandomStream(30),
                ContentType = "text/plain"
            };

            List<byte[]?> receivedFiles = new();

            Host.ConfigureServices(
                    services =>
                    {
                        services
                            .AddLogging()
                            .AddSilverback()
                            .UseModel()
                            .WithConnectionToMessageBroker(
                                options => options
                                    .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                            .AddKafkaEndpoints(
                                endpoints => endpoints
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.BootstrapServers = "PLAINTEXT://e2e";
                                        })
                                    .AddOutbound<IBinaryFileMessage>(
                                        endpoint => endpoint
                                            .ProduceTo(DefaultTopicName)
                                            .EnableChunking(10)
                                            .EncryptUsingAes(AesEncryptionKey))
                                    .AddInbound(
                                        endpoint => endpoint
                                            .ConsumeFrom(DefaultTopicName)
                                            .DecryptUsingAes(AesEncryptionKey)
                                            .ConfigureClient(
                                                configuration =>
                                                {
                                                    configuration.GroupId = DefaultConsumerGroupId;
                                                })))
                            .AddDelegateSubscriber(
                                (BinaryFileMessage binaryFile) =>
                                    receivedFiles.Add(binaryFile.Content.ReadAll()))
                            .AddIntegrationSpy();
                    })
                .Run();

            IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(12);

            for (int i = 0; i < 6; i++)
            {
                Helper.Spy.RawOutboundEnvelopes[i].RawMessage.Should().NotBeNull();
                Helper.Spy.RawOutboundEnvelopes[i].RawMessage!.Length.Should().BeLessOrEqualTo(10);
                Helper.Spy.RawOutboundEnvelopes[i].RawMessage.ReReadAll().Should()
                    .NotBeEquivalentTo(message1.Content.ReReadAll()!.Skip(i * 10).Take(10));
            }

            for (int i = 0; i < 6; i++)
            {
                Helper.Spy.RawOutboundEnvelopes[i + 6].RawMessage.Should().NotBeNull();
                Helper.Spy.RawOutboundEnvelopes[i + 6].RawMessage!.Length.Should().BeLessOrEqualTo(10);
                Helper.Spy.RawOutboundEnvelopes[i + 6].RawMessage.ReReadAll().Should()
                    .NotBeEquivalentTo(message2.Content.ReReadAll()!.Skip(i * 10).Take(10));
            }

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(
                new[]
                {
                    message1.Content.ReReadAll(),
                    message2.Content.ReReadAll()
                });
        }
    }
}
