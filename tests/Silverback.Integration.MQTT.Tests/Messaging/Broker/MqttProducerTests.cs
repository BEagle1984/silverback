// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker;

public class MqttProducerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited")]
    [Fact]
    public async Task ProduceAsync_ShouldNotBlockPublishing_WhenContinuationProducesSynchronously()
    {
        IMqttClient nativeClient = Substitute.For<IMqttClient>();
        ConcurrentQueue<byte> published = new();
        bool isConnected = false;

        nativeClient.IsConnected.Returns(_ => isConnected);
        nativeClient.ConnectAsync(Arg.Any<MqttClientOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            isConnected = true;

            return new MqttClientConnectResult();
        });
        nativeClient.PublishAsync(Arg.Any<MqttApplicationMessage>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            published.Enqueue(call.Arg<MqttApplicationMessage>().Payload.ToArray()[0]);

            return new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, null, []);
        });

        MqttClientConfiguration configuration = new MqttClientConfigurationBuilder(Substitute.For<IServiceProvider>())
            .ConnectViaTcp("test-broker")
            .WithClientId("test-client")
            .Produce<string>(endpoint => endpoint.ProduceTo("output"))
            .Build();
        await using MqttClientWrapper client = new(
            "test-client",
            nativeClient,
            configuration,
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<ISilverbackLogger>());
        IBrokerBehaviorsProvider<IProducerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>();
        behaviors.GetBehaviorsList().Returns([]);
        using MqttProducer producer = new(
            "test-producer",
            client,
            configuration,
            behaviors,
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<MqttProducer>>());

        await client.ConnectAsync();

        // Run without a synchronization context, so that an inline continuation would execute on the publishing loop
        Task producing = Task.Run(async () =>
        {
            await producer.RawProduceAsync([1]).ConfigureAwait(false);
            producer.RawProduce([2]);
        });

        await producing.WaitAsync(Timeout);

        published.ToArray().ShouldBe([1, 2]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawProduce_ShouldNotShareByteArray(bool useCallbacks)
    {
        ConcurrentQueue<MqttApplicationMessage> published = new();
        (MqttClientWrapper client, MqttProducer producer) = await CreateProducerAsync(published);
        await using MqttClientWrapper disposableClient = client;
        using MqttProducer disposableProducer = producer;
        byte[] content = [1, 2, 3];

        await RawProduceAsync(producer, content, useCallbacks);

        MqttApplicationMessage message = published.ShouldHaveSingleItem();
        message.Payload.ToArray().ShouldBe([1, 2, 3]);
        GetPayloadArray(message).ShouldNotBeSameAs(content);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawProduce_ShouldNotShareStreamBuffer(bool useCallbacks)
    {
        ConcurrentQueue<MqttApplicationMessage> published = new();
        (MqttClientWrapper client, MqttProducer producer) = await CreateProducerAsync(published);
        await using MqttClientWrapper disposableClient = client;
        using MqttProducer disposableProducer = producer;
        byte[] content = [1, 2, 3];

        await RawProduceAsync(producer, new MemoryStream(content, 0, content.Length, false, true), useCallbacks);

        MqttApplicationMessage message = published.ShouldHaveSingleItem();
        message.Payload.ToArray().ShouldBe([1, 2, 3]);
        GetPayloadArray(message).ShouldNotBeSameAs(content);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawProduce_ShouldNotCopyOwnedBuffer(bool useCallbacks)
    {
        ConcurrentQueue<MqttApplicationMessage> published = new();
        (MqttClientWrapper client, MqttProducer producer) = await CreateProducerAsync(published);
        await using MqttClientWrapper disposableClient = client;
        using MqttProducer disposableProducer = producer;
        byte[] buffer = [1, 2, 3];

        await RawProduceAsync(producer, new OwnedReadOnlyMemoryStream(buffer), useCallbacks);

        GetPayloadArray(published.ShouldHaveSingleItem()).ShouldBeSameAs(buffer);
    }

    private static Task RawProduceAsync(MqttProducer producer, Stream content, bool useCallbacks) =>
        Task.Run(async () =>
        {
            if (useCallbacks)
            {
                TaskCompletionSource taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                producer.RawProduce(content, null, _ => taskCompletionSource.SetResult(), taskCompletionSource.SetException);
                await taskCompletionSource.Task.ConfigureAwait(false);
            }
            else
            {
                await producer.RawProduceAsync(content).ConfigureAwait(false);
            }
        }).WaitAsync(Timeout);

    private static Task RawProduceAsync(MqttProducer producer, byte[] content, bool useCallbacks) =>
        Task.Run(async () =>
        {
            if (useCallbacks)
            {
                TaskCompletionSource taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                producer.RawProduce(content, null, _ => taskCompletionSource.SetResult(), taskCompletionSource.SetException);
                await taskCompletionSource.Task.ConfigureAwait(false);
            }
            else
            {
                await producer.RawProduceAsync(content).ConfigureAwait(false);
            }
        }).WaitAsync(Timeout);

    private static byte[]? GetPayloadArray(MqttApplicationMessage message) =>
        MemoryMarshal.TryGetArray(message.Payload.First, out ArraySegment<byte> segment) ? segment.Array : null;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Returned")]
    private static async Task<(MqttClientWrapper Client, MqttProducer Producer)> CreateProducerAsync(
        ConcurrentQueue<MqttApplicationMessage> published)
    {
        IMqttClient nativeClient = Substitute.For<IMqttClient>();
        bool isConnected = false;

        nativeClient.IsConnected.Returns(_ => isConnected);
        nativeClient.ConnectAsync(Arg.Any<MqttClientOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            isConnected = true;

            return new MqttClientConnectResult();
        });
        nativeClient.PublishAsync(Arg.Any<MqttApplicationMessage>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            published.Enqueue(call.Arg<MqttApplicationMessage>());

            return new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, null, []);
        });

        MqttClientConfiguration configuration = new MqttClientConfigurationBuilder(Substitute.For<IServiceProvider>())
            .ConnectViaTcp("test-broker")
            .WithClientId("test-client")
            .Produce<string>(endpoint => endpoint.ProduceTo("output"))
            .Build();
        MqttClientWrapper client = new(
            "test-client",
            nativeClient,
            configuration,
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<ISilverbackLogger>());
        IBrokerBehaviorsProvider<IProducerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>();
        behaviors.GetBehaviorsList().Returns([]);
        MqttProducer producer = new(
            "test-producer",
            client,
            configuration,
            behaviors,
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<MqttProducer>>());

        await client.ConnectAsync();

        return (client, producer);
    }
}
