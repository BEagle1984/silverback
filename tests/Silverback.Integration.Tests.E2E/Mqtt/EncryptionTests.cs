// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt
{
    public class EncryptionTests : MqttTestFixture
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c,
            0x1d, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        private static readonly byte[] AesEncryptionKey2 =
        {
            0x2d, 0x2e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c,
            0x1d, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
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
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
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
        }

        [Fact]
        public async Task Encryption_SimpleMessagesWithKeyRotation_EncryptedAndDecrypted()
        {
            var message1 = new TestEventOne { Content = "Message 1" };
            var message2 = new TestEventTwo { Content = "Message 2" };
            const string keyIdentifier1 = "my-encryption-key-1";
            const string keyIdentifier2 = "my-encryption-key-2";

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey, keyIdentifier1))
                                .AddOutbound<TestEventTwo>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey2, keyIdentifier2))
                                .AddInbound(
                                    endpoint => endpoint
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
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
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
                            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                            .AddMqttEndpoints(
                                endpoints => endpoints
                                    .Configure(
                                        config => config
                                            .WithClientId("e2e-test")
                                            .ConnectViaTcp("e2e-mqtt-broker"))
                                    .AddOutbound<IBinaryFileMessage>(
                                        endpoint => endpoint
                                            .ProduceTo(DefaultTopicName)
                                            .EncryptUsingAes(AesEncryptionKey))
                                    .AddInbound(
                                        endpoint => endpoint
                                            .ConsumeFrom(DefaultTopicName)
                                            .DecryptUsingAes(AesEncryptionKey)))
                            .AddDelegateSubscriber(
                                (BinaryFileMessage binaryFile) =>
                                    receivedFiles.Add(binaryFile.Content.ReadAll()))
                            .AddIntegrationSpy();
                    })
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.OutboundEnvelopes[0].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
            Helper.Spy.OutboundEnvelopes[1].RawMessage.Should().BeOfType<SymmetricEncryptStream>();

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(message1.Content.ReReadAll(), message2.Content.ReReadAll());
        }
    }
}
