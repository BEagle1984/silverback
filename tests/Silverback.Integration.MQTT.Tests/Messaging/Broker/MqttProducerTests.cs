// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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

        nativeClient.IsConnected.Returns(_ => Volatile.Read(ref isConnected));
        nativeClient.ConnectAsync(Arg.Any<MqttClientOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Volatile.Write(ref isConnected, true);

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
}
