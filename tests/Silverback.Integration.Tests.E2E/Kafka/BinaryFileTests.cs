// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class BinaryFileTests : KafkaTestFixture
{
    public BinaryFileTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task BinaryFile_DefaultSettings_ProducedAndConsumed()
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .ConfigureClient(
                                    configuration =>
                                    {
                                        configuration.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IBinaryFileMessage>(producer => producer.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    consumer => consumer
                                        .ConsumeFrom(DefaultTopicName)
                                        .ConfigureClient(
                                            configuration =>
                                            {
                                                configuration.GroupId = DefaultConsumerGroupId;
                                            })))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                lock (receivedFiles)
                                {
                                    receivedFiles.Add(binaryFile.Content.ReadAll());
                                }
                            })
                        .AddIntegrationSpy();
                })
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.As<BinaryFileMessage>().ContentType)
            .Should().BeEquivalentTo("application/pdf", "text/plain");

        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(
            new[]
            {
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            });
    }

    [Fact]
    public async Task BinaryFile_ForcingBinaryFileSerializer_ProducedAndConsumed()
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

        ConcurrentBag<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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
                                    .ProduceBinaryFiles())
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })
                                    .ConsumeBinaryFiles()))
                    .AddDelegateSubscriber((BinaryFileMessage binaryFile) => receivedFiles.Add(binaryFile.Content.ReadAll()))
                    .AddIntegrationSpy())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.As<BinaryFileMessage>().ContentType)
            .Should().BeEquivalentTo("application/pdf", "text/plain");

        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(
            new[]
            {
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            });
    }

    [Fact]
    public async Task BinaryFile_ForcingBinaryFileSerializerWithoutTypeHeader_Consumed()
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
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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
                                    .ProduceBinaryFiles(
                                        serializer => serializer
                                            .UseModel<BinaryFileMessage>()))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })
                                    .ConsumeBinaryFiles()))
                    .AddDelegateSubscriber(
                        (BinaryFileMessage binaryFile) =>
                        {
                            lock (receivedFiles)
                            {
                                receivedFiles.Add(binaryFile.Content.ReadAll());
                            }
                        })
                    .AddSingletonBrokerBehavior<RemoveMessageTypeHeaderProducerBehavior>()
                    .AddIntegrationSpy())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        Helper.Spy.OutboundEnvelopes.ForEach(
            envelope =>
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType).Should().BeNull());
        Helper.Spy.OutboundEnvelopes
            .Select(envelope => envelope.Message.As<BinaryFileMessage>().ContentType)
            .Should().BeEquivalentTo("application/pdf", "text/plain");

        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(
            new[]
            {
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            });
    }

    [Fact]
    public async Task BinaryFile_ForcingTypedBinaryFileSerializerWithWrongTypeHeader_Consumed()
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
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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
                                    .ProduceBinaryFiles())
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })
                                    .ConsumeBinaryFiles(
                                        serializer => serializer
                                            .UseModel<CustomBinaryFileMessage>())))
                    .AddDelegateSubscriber(
                        (CustomBinaryFileMessage binaryFile) =>
                        {
                            lock (receivedFiles)
                            {
                                receivedFiles.Add(binaryFile.Content.ReadAll());
                            }
                        })
                    .AddIntegrationSpy())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.ForEach(
            envelope =>
            {
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType).Should().NotBeNull();
                envelope.Message.Should().BeOfType<CustomBinaryFileMessage>();
                envelope.Headers.GetValue("x-custom-header").Should().BeNull();
            });
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.As<BinaryFileMessage>().ContentType)
            .Should().BeEquivalentTo("application/pdf", "text/plain");

        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(
            new[]
            {
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            });
    }

    [Fact]
    public async Task BinaryFile_WithCustomHeaders_ProducedAndConsumed()
    {
        CustomBinaryFileMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "application/pdf",
            CustomHeader = "one"
        };

        CustomBinaryFileMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "text/plain",
            CustomHeader = "two"
        };

        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IBinaryFileMessage>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })
                                    .ConsumeBinaryFiles(
                                        serializer => serializer
                                            .UseModel<CustomBinaryFileMessage>())))
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

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeOfType<CustomBinaryFileMessage>());
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Headers.GetValue("x-custom-header"))
            .Should().BeEquivalentTo("one", "two");
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.As<CustomBinaryFileMessage>().CustomHeader)
            .Should().BeEquivalentTo("one", "two");
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.As<BinaryFileMessage>().ContentType)
            .Should().BeEquivalentTo("application/pdf", "text/plain");

        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(
            new[]
            {
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            });
    }

    [Fact]
    public async Task BinaryFile_WithoutTypeHeaderAndWithCustomHeaders_Consumed()
    {
        CustomBinaryFileMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "application/pdf",
            CustomHeader = "one"
        };

        CustomBinaryFileMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "text/plain",
            CustomHeader = "two"
        };

        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IBinaryFileMessage>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })
                                    .ConsumeBinaryFiles(serializer => serializer.UseModel<CustomBinaryFileMessage>())))
                    .AddDelegateSubscriber(
                        (BinaryFileMessage binaryFile) =>
                        {
                            lock (receivedFiles)
                            {
                                receivedFiles.Add(binaryFile.Content.ReadAll());
                            }
                        })
                    .AddSingletonBrokerBehavior<RemoveMessageTypeHeaderProducerBehavior>()
                    .AddIntegrationSpy())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes.ForEach(envelope => envelope.Headers.GetValue(DefaultMessageHeaders.MessageType).Should().BeNull());
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeOfType<CustomBinaryFileMessage>());
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Headers.GetValue("x-custom-header"))
            .Should().BeEquivalentTo("one", "two");
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.As<CustomBinaryFileMessage>().CustomHeader)
            .Should().BeEquivalentTo("one", "two");
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.As<BinaryFileMessage>().ContentType)
            .Should().BeEquivalentTo("application/pdf", "text/plain");

        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(
            new[]
            {
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            });
    }
}
