// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Packets;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ChunkingTests : MqttTests
{
    private const int ChunkSize = 10;

    public ChunkingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Chunking_ShouldProduceAndConsumeChunkedJson()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableChunking(ChunkSize))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS())))
            .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(5);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBeGreaterThan(10);
        Helper.Spy.RawOutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.ShouldBeLessThanOrEqualTo(ChunkSize));
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(5);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 5).Select(i => $"Long message {i}"));

        // The chunks of each message are correlated by the chunked message id, sent as user property
        IReadOnlyList<MqttApplicationMessage> messages = GetDefaultTopicMessages();
        messages.Count.ShouldBe(Helper.Spy.RawOutboundEnvelopes.Count);
        string?[] messageIds = [.. messages.Select(message => GetUserProperty(message, MqttMessageHeaders.ChunkMessageId))];
        messageIds.ShouldAllBe(messageId => !string.IsNullOrEmpty(messageId));
        messageIds.Distinct().Count().ShouldBe(5);

        foreach (IGrouping<string?, MqttApplicationMessage> chunks in messages.GroupBy(message => GetUserProperty(message, MqttMessageHeaders.ChunkMessageId)))
        {
            chunks.Select(chunk => GetUserProperty(chunk, DefaultMessageHeaders.ChunkIndex))
                .ShouldBe(Enumerable.Range(0, chunks.Count()).Select(index => index.ToString(CultureInfo.InvariantCulture)));
        }

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Chunking_ShouldConsumeChunkedJson_WhenProcessingInParallel()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .EnableParallelProcessing(4)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableChunking(ChunkSize))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS())))
            .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 20; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(20);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 20).Select(i => $"Long message {i}"), ignoreOrder: true);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Chunking_ShouldProduceAndConsumeChunkedJsonWithCustomHeaderNames()
    {
        const int chunksPerMessage = 4;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .WithCustomHeaderName(DefaultMessageHeaders.ChunkIndex, "x-quack-index")
            .WithCustomHeaderName(DefaultMessageHeaders.ChunksCount, "x-quack-count")
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableChunking(ChunkSize))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS())))
            .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(5);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(5 * chunksPerMessage);
        Helper.Spy.RawOutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.ShouldBeLessThanOrEqualTo(ChunkSize));

        // The chunk headers are renamed, while the chunked message id (which can't be renamed) is still sent to correlate the chunks
        IReadOnlyList<MqttApplicationMessage> messages = GetDefaultTopicMessages();
        messages.Count.ShouldBe(5 * chunksPerMessage);

        for (int i = 0; i < messages.Count; i++)
        {
            MqttApplicationMessage message = messages[i];

            GetUserProperty(message, DefaultMessageHeaders.ChunkIndex).ShouldBeNull();
            GetUserProperty(message, DefaultMessageHeaders.ChunksCount).ShouldBeNull();
            GetUserProperty(message, "x-quack-index").ShouldBe((i % chunksPerMessage).ToString(CultureInfo.InvariantCulture));
            GetUserProperty(message, "x-quack-count").ShouldBe(chunksPerMessage.ToString(CultureInfo.InvariantCulture));
            GetUserProperty(message, MqttMessageHeaders.ChunkMessageId).ShouldNotBeNullOrEmpty();
        }

        messages.Chunk(chunksPerMessage)
            .Select(chunks => chunks
                .Select(chunk => GetUserProperty(chunk, MqttMessageHeaders.ChunkMessageId))
                .Distinct()
                .ShouldHaveSingleItem())
            .Distinct()
            .Count()
            .ShouldBe(5);

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(5);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 5).Select(i => $"Long message {i}"));
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Chunking_ShouldReplayChunkedMessage_WhenRetryPolicyIsApplied()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableChunking(ChunkSize))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .OnError(policy => policy.Retry(2)))))
            .AddDelegateSubscriber<TestEventOne>(HandleMessage)
            .AddIntegrationSpy());

        void HandleMessage(TestEventOne message)
        {
            if (message.ContentEventOne == "Long message 2" && Interlocked.Increment(ref tryCount) <= 2)
                throw new InvalidOperationException("Retry!");
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(["Long message 1", "Long message 2", "Long message 2", "Long message 2", "Long message 3"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    private static string? GetUserProperty(MqttApplicationMessage message, string name) =>
        message.UserProperties?.FirstOrDefault(property => property.Name == name)?.ReadValueAsString();
}
