// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class BinaryFileTests : E2ETestFixture
    {
        public BinaryFileTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task BinaryFile_DefaultSettings_ProducedAndConsumed()
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
                            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                            .AddEndpoints(
                                endpoints => endpoints
                                    .AddOutbound<IBinaryFileMessage>(new KafkaProducerEndpoint(DefaultTopicName))
                                    .AddInbound(
                                        new KafkaConsumerEndpoint(DefaultTopicName)
                                        {
                                            Configuration =
                                            {
                                                GroupId = "consumer1",
                                                AutoCommitIntervalMs = 100
                                            }
                                        }))
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
            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes
                .Select(envelope => envelope.Message.As<BinaryFileMessage>().ContentType)
                .Should().BeEquivalentTo("application/pdf", "text/plain");

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(message1.Content.ReReadAll(), message2.Content.ReReadAll());
        }

        [Fact]
        public async Task BinaryFile_ForcingBinaryFileSerializer_ProducedAndConsumed()
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

            var receivedFiles = new ConcurrentBag<byte[]?>();

            Host.ConfigureServices(
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
                                        Serializer = BinaryFileMessageSerializer.Default
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration =
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        Serializer = BinaryFileMessageSerializer.Default
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) => receivedFiles.Add(binaryFile.Content.ReadAll()))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes
                .Select(envelope => envelope.Message.As<BinaryFileMessage>().ContentType)
                .Should().BeEquivalentTo("application/pdf", "text/plain");

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(message1.Content.ReReadAll(), message2.Content.ReReadAll());
        }

        [Fact]
        public async Task BinaryFile_ForcingBinaryFileSerializerWithoutTypeHeader_Consumed()
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
                                    new KafkaProducerEndpoint(DefaultTopicName)
                                    {
                                        Serializer = BinaryFileMessageSerializer.Default
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration =
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        Serializer = BinaryFileMessageSerializer.Default
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                lock (receivedFiles)
                                {
                                    receivedFiles.Add(binaryFile.Content.ReadAll());
                                }
                            })
                        .AddSingletonBrokerBehavior<RemoveMessageTypeHeaderProducerBehavior>()
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);

            SpyBehavior.InboundEnvelopes.ForEach(
                envelope =>
                    envelope.Headers.GetValue(DefaultMessageHeaders.MessageType).Should().BeNull());

            SpyBehavior.InboundEnvelopes
                .Select(envelope => envelope.Message.As<BinaryFileMessage>().ContentType)
                .Should().BeEquivalentTo("application/pdf", "text/plain");

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(message1.Content.ReReadAll(), message2.Content.ReReadAll());
        }

        [Fact]
        public async Task BinaryFile_ForcingTypedBinaryFileSerializerWithWrongTypeHeader_Consumed()
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
                                    new KafkaProducerEndpoint(DefaultTopicName)
                                    {
                                        Serializer = new BinaryFileMessageSerializer()
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration =
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        Serializer = new BinaryFileMessageSerializer<CustomBinaryFileMessage>()
                                    }))
                        .AddDelegateSubscriber(
                            (CustomBinaryFileMessage binaryFile) =>
                            {
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

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.Headers.GetValue(DefaultMessageHeaders.MessageType).Should().NotBeNull();
                    envelope.Message.Should().BeOfType<CustomBinaryFileMessage>();
                    envelope.Headers.GetValue("x-custom-header").Should().BeNull();
                });
            SpyBehavior.InboundEnvelopes
                .Select(envelope => envelope.Message.As<BinaryFileMessage>().ContentType)
                .Should().BeEquivalentTo("application/pdf", "text/plain");

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(message1.Content.ReReadAll(), message2.Content.ReReadAll());
        }

        [Fact]
        public async Task BinaryFile_WithCustomHeaders_ProducedAndConsumed()
        {
            var message1 = new CustomBinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                        0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
                    }),
                ContentType = "application/pdf",
                CustomHeader = "one"
            };

            var message2 = new CustomBinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                        0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x40,
                        0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x50
                    }),
                ContentType = "text/plain",
                CustomHeader = "two"
            };

            var receivedFiles = new List<byte[]?>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration =
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        Serializer = new BinaryFileMessageSerializer<CustomBinaryFileMessage>()
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
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

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.ForEach(
                envelope => envelope.Message.Should().BeOfType<CustomBinaryFileMessage>());
            SpyBehavior.InboundEnvelopes
                .Select(envelope => envelope.Headers.GetValue("x-custom-header"))
                .Should().BeEquivalentTo("one", "two");
            SpyBehavior.InboundEnvelopes
                .Select(envelope => envelope.Message.As<CustomBinaryFileMessage>().CustomHeader)
                .Should().BeEquivalentTo("one", "two");
            SpyBehavior.InboundEnvelopes
                .Select(envelope => envelope.Message.As<BinaryFileMessage>().ContentType)
                .Should().BeEquivalentTo("application/pdf", "text/plain");

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(message1.Content.ReReadAll(), message2.Content.ReReadAll());
        }

        [Fact]
        public async Task BinaryFile_WithoutTypeHeaderAndWithCustomHeaders_Consumed()
        {
            var message1 = new CustomBinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                        0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
                    }),
                ContentType = "application/pdf",
                CustomHeader = "one"
            };

            var message2 = new CustomBinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                        0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x40,
                        0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x50
                    }),
                ContentType = "text/plain",
                CustomHeader = "two"
            };

            var receivedFiles = new List<byte[]?>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration =
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        Serializer = new BinaryFileMessageSerializer<CustomBinaryFileMessage>()
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                lock (receivedFiles)
                                {
                                    receivedFiles.Add(binaryFile.Content.ReadAll());
                                }
                            })
                        .AddSingletonBrokerBehavior<RemoveMessageTypeHeaderProducerBehavior>()
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.OutboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(2);
            SpyBehavior.InboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.Message.Should().BeOfType<CustomBinaryFileMessage>();
                    envelope.Headers.GetValue(DefaultMessageHeaders.MessageType).Should().BeNull();
                });
            SpyBehavior.InboundEnvelopes
                .Select(envelope => envelope.Headers.GetValue("x-custom-header"))
                .Should().BeEquivalentTo("one", "two");
            SpyBehavior.InboundEnvelopes
                .Select(envelope => envelope.Message.As<CustomBinaryFileMessage>().CustomHeader)
                .Should().BeEquivalentTo("one", "two");
            SpyBehavior.InboundEnvelopes
                .Select(envelope => envelope.Message.As<BinaryFileMessage>().ContentType)
                .Should().BeEquivalentTo("application/pdf", "text/plain");

            receivedFiles.Should().HaveCount(2);
            receivedFiles.Should().BeEquivalentTo(message1.Content.ReReadAll(), message2.Content.ReReadAll());
        }
    }
}
