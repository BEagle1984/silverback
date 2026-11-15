// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker.Mqtt;

public class MqttClientWrapperTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(MqttClientSubscribeResultCode.NotAuthorized)]
    [InlineData(MqttClientSubscribeResultCode.UnspecifiedError)] // The only failure code of MQTT 3.1.1 (0x80)
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Modified to simulate a following subscription")]
    public async Task SubscribeAsync_ShouldLogAndExcludeTopicFiltersRejectedByBroker(MqttClientSubscribeResultCode resultCode)
    {
        bool reject = true;
        IMqttClient nativeClient = Substitute.For<IMqttClient>();
        nativeClient.SubscribeAsync(Arg.Any<MqttClientSubscribeOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            MqttClientSubscribeOptions options = call.Arg<MqttClientSubscribeOptions>();

            return Task.FromResult(
                new MqttClientSubscribeResult(
                    0,
                    [
                        new MqttClientSubscribeResultItem(options.TopicFilters[0], MqttClientSubscribeResultCode.GrantedQoS1),
                        new MqttClientSubscribeResultItem(options.TopicFilters[1], reject ? resultCode : MqttClientSubscribeResultCode.GrantedQoS0),
                        new MqttClientSubscribeResultItem(options.TopicFilters[2], MqttClientSubscribeResultCode.GrantedQoS0)
                    ],
                    string.Empty,
                    []));
        });

        LoggerSubstitute<MqttClientWrapperTests> loggerSubstitute = new(LogLevel.Trace);
        SilverbackLogger<MqttClientWrapperTests> logger = new(new MappedLevelsLogger<MqttClientWrapperTests>([], loggerSubstitute));
        MqttClientConfiguration configuration = new MqttClientConfigurationBuilder(Substitute.For<IServiceProvider>())
            .ConnectViaTcp("test-broker")
            .WithClientId("test-client")
            .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2/#", "topic3"))
            .Build();
        MqttClientWrapper client = new("client1", nativeClient, configuration, Substitute.For<IBrokerClientCallbacksInvoker>(), logger);

        client.SubscribedTopicsFilters.Select(filter => filter.Topic).ShouldBe(["topic1", "topic2/#", "topic3"]);

        await client.SubscribeAsync();

        client.SubscribedTopicsFilters.Select(filter => filter.Topic).ShouldBe(["topic1", "topic3"]);
        loggerSubstitute.Received(
            LogLevel.Error,
            null,
            $"Subscription to topic2/# rejected by the broker with result code {resultCode} | ClientName: client1, ClientId: test-client",
            4042);

        // A following subscription (e.g. after reconnecting) resets the rejected filters
        reject = false;
        await client.SubscribeAsync();

        client.SubscribedTopicsFilters.Select(filter => filter.Topic).ShouldBe(["topic1", "topic2/#", "topic3"]);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ConnectAsync_ShouldRaiseSubscribed_UnlessBrokerRejectsAllTopicFilters(bool rejectAll, bool expectedSubscribed)
    {
        bool isConnected = false;
        IMqttClient nativeClient = Substitute.For<IMqttClient>();
        nativeClient.IsConnected.Returns(_ => isConnected);
        nativeClient.ConnectAsync(Arg.Any<MqttClientOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            isConnected = true;
            return new MqttClientConnectResult();
        });
        nativeClient.DisconnectAsync(Arg.Any<MqttClientDisconnectOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            isConnected = false;
            return Task.CompletedTask;
        });
        nativeClient.SubscribeAsync(Arg.Any<MqttClientSubscribeOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
            Task.FromResult(
                new MqttClientSubscribeResult(
                    0,
                    [
                        .. call.Arg<MqttClientSubscribeOptions>().TopicFilters.Select((filter, index) =>
                            new MqttClientSubscribeResultItem(
                                filter,
                                rejectAll || index > 0 ? MqttClientSubscribeResultCode.NotAuthorized : MqttClientSubscribeResultCode.GrantedQoS0))
                    ],
                    string.Empty,
                    [])));

        IBrokerClientCallbacksInvoker callbacksInvoker = Substitute.For<IBrokerClientCallbacksInvoker>();
        MqttClientConfiguration configuration = new MqttClientConfigurationBuilder(Substitute.For<IServiceProvider>())
            .ConnectViaTcp("test-broker")
            .WithClientId("test-client")
            .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2"))
            .Build();
        await using MqttClientWrapper client = new(
            "client1",
            nativeClient,
            configuration,
            callbacksInvoker,
            Substitute.For<ISilverbackLogger>());
        bool subscribed = false;
        client.Subscribed.AddHandler(_ =>
        {
            subscribed = true;
            return ValueTask.CompletedTask;
        });

        await client.ConnectAsync();

        // The connected callbacks are invoked after the Subscribed event
        await WaitUntilAsync(() => callbacksInvoker.ReceivedCalls().Any(call =>
            call.GetMethodInfo().IsGenericMethod &&
            call.GetMethodInfo().GetGenericArguments()[0] == typeof(IMqttClientConnectedCallback)));

        subscribed.ShouldBe(expectedSubscribed);
        client.SubscribedTopicsFilters.Select(filter => filter.Topic).ShouldBe(rejectAll ? [] : ["topic1"]);
        client.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public async Task DisconnectAsync_ShouldUnsubscribeBeforeDisconnecting()
    {
        await using SubscriberHarness harness = new();
        await harness.ConnectAsync();

        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        harness.NativeCalls.ToArray().ShouldBe(["Subscribe", "Unsubscribe", "Disconnect"]);
        harness.UnsubscribedTopicFilters.ShouldBe(["$share/group/topic1", "topic2"]);
        harness.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Theory]
    [InlineData(MqttProtocolVersion.V500, true, 0U, true)]
    [InlineData(MqttProtocolVersion.V500, false, 0U, true)]
    [InlineData(MqttProtocolVersion.V500, true, 3600U, false)]
    [InlineData(MqttProtocolVersion.V500, false, uint.MaxValue, false)]
    [InlineData(MqttProtocolVersion.V311, true, 0U, true)]
    [InlineData(MqttProtocolVersion.V311, true, 3600U, true)] // The session expiry interval doesn't exist in MQTT 3.1.1
    [InlineData(MqttProtocolVersion.V311, false, 0U, false)]
    public async Task DisconnectAsync_ShouldUnsubscribeOnlyWhenSessionEndsOnDisconnect(
        MqttProtocolVersion protocolVersion,
        bool cleanSession,
        uint sessionExpiryInterval,
        bool shouldUnsubscribe)
    {
        await using SubscriberHarness harness = new(configuration => configuration with
        {
            ProtocolVersion = protocolVersion,
            CleanSession = cleanSession,
            SessionExpiryInterval = sessionExpiryInterval
        });
        await harness.ConnectAsync();

        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        harness.NativeCalls.ToArray().ShouldBe(shouldUnsubscribe ? ["Subscribe", "Unsubscribe", "Disconnect"] : ["Subscribe", "Disconnect"]);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldNotUnsubscribe_WhenPersistentSessionIsRequested()
    {
        await using SubscriberHarness harness = new(configureBuilder: builder => builder.RequestPersistentSession());
        await harness.ConnectAsync();

        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        harness.NativeCalls.ToArray().ShouldBe(["Subscribe", "Disconnect"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisconnectAsync_ShouldNotUnsubscribe_WhenNotConnected(bool connectionLost)
    {
        await using SubscriberHarness harness = new();

        if (connectionLost)
        {
            await harness.ConnectAsync();
            harness.LoseConnection();
        }
        else
        {
            harness.LoseConnection();
            await harness.Client.ConnectAsync();
        }

        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        harness.NativeCalls.ToArray().ShouldBe(connectionLost ? ["Subscribe"] : []);
        harness.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldLogAndDisconnect_WhenUnsubscribeFails()
    {
        await using SubscriberHarness harness = new();
        harness.UnsubscribeException = new InvalidOperationException("Unsubscribe failed.");
        await harness.ConnectAsync();

        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        harness.NativeCalls.ToArray().ShouldBe(["Subscribe", "Unsubscribe", "Disconnect"]);
        harness.Client.Status.ShouldBe(ClientStatus.Disconnected);
        harness.Logger.Received(
            LogLevel.Warning,
            typeof(InvalidOperationException),
            eventId: MqttLogEvents.UnsubscribeError.EventId.Id);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldLogAndDisconnect_WhenBrokerRejectsUnsubscription()
    {
        await using SubscriberHarness harness = new();
        harness.UnsubscribeResultCodes["$share/group/topic1"] = MqttClientUnsubscribeResultCode.NotAuthorized;
        harness.UnsubscribeResultCodes["topic2"] = MqttClientUnsubscribeResultCode.NoSubscriptionExisted;
        await harness.ConnectAsync();

        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        harness.NativeCalls.ToArray().ShouldBe(["Subscribe", "Unsubscribe", "Disconnect"]);
        harness.Client.Status.ShouldBe(ClientStatus.Disconnected);
        harness.Logger.Received(
            LogLevel.Warning,
            null,
            "Unsubscription from $share/group/topic1 rejected by the broker with result code NotAuthorized | ClientName: client1, ClientId: test-client",
            MqttLogEvents.UnsubscriptionRejected.EventId.Id);

        // No subscription existing (e.g. rejected at subscription time) isn't reported
        harness.Logger.DidNotReceive(
            LogLevel.Warning,
            null,
            "Unsubscription from topic2 rejected by the broker with result code NoSubscriptionExisted | ClientName: client1, ClientId: test-client",
            MqttLogEvents.UnsubscriptionRejected.EventId.Id);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldLogAndDisconnect_WhenUnsubscribeTimesOut()
    {
        await using SubscriberHarness harness = new(timeout: TimeSpan.FromMilliseconds(100));
        harness.HangUnsubscribe = true;
        await harness.ConnectAsync();

        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        harness.NativeCalls.ToArray().ShouldBe(["Subscribe", "Unsubscribe", "Disconnect"]);
        harness.Client.Status.ShouldBe(ClientStatus.Disconnected);
        harness.Logger.Received(
            LogLevel.Warning,
            typeof(MqttCommunicationTimedOutException),
            eventId: MqttLogEvents.UnsubscribeError.EventId.Id);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldUnsubscribeOnlyTopicFiltersGrantedByBroker()
    {
        await using SubscriberHarness harness = new(topics: ["$share/group/topic1", "topic2", "topic3"]);
        harness.SubscribeResultCodes["topic2"] = MqttClientSubscribeResultCode.NotAuthorized;
        await harness.ConnectAsync();

        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        harness.NativeCalls.ToArray().ShouldBe(["Subscribe", "Unsubscribe", "Disconnect"]);
        harness.UnsubscribedTopicFilters.ShouldBe(["$share/group/topic1", "topic3"]);
    }

    [Theory]
    [InlineData("topic1", null)]
    [InlineData("$share/group/topic1", "$share/group/topic1")]
    public async Task DisconnectAsync_ShouldNotUnsubscribe_WhenNoSharedSubscriptionIsSubscribed(string topic, string? rejectedTopic)
    {
        await using SubscriberHarness harness = new(topics: [topic, "topic2"]);

        if (rejectedTopic != null)
            harness.SubscribeResultCodes[rejectedTopic] = MqttClientSubscribeResultCode.NotAuthorized;

        await harness.ConnectAsync();

        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        harness.NativeCalls.ToArray().ShouldBe(["Subscribe", "Disconnect"]);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldCompleteInFlightPublishBeforeDisconnecting()
    {
        await using ProducerHarness harness = new();
        await harness.ConnectAsync();

        Task publishing = harness.ProduceAsync(1);
        await harness.PublishStarted.Task.WaitAsync(Timeout);
        Task disconnecting = harness.Client.DisconnectAsync().AsTask();

        harness.ReleasePublish.TrySetResult(true);
        await disconnecting.WaitAsync(Timeout);
        await publishing.WaitAsync(Timeout);

        harness.PublishedBeforeDisconnect.ShouldBeTrue();
        harness.PublishCancellationToken.IsCancellationRequested.ShouldBeFalse();
    }

    [Fact]
    public async Task Produce_ShouldFailPromptlyAfterDisconnect()
    {
        await using ProducerHarness harness = new();
        await harness.ConnectAsync();
        await harness.Client.DisconnectAsync();

        await Should.ThrowAsync<ProduceException>(() => harness.ProduceAsync(1).WaitAsync(Timeout));
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The registration is disposed first")]
    [Fact]
    public async Task DisconnectAsync_ShouldFlushQueuedMessagesInOrder_AndRejectNewMessages()
    {
        await using ProducerHarness harness = new();

        // Keep the attempt pending, since its linked token stops being canceled once it completes
        harness.BlockConnection(reportConnected: true);
        await harness.Client.ConnectAsync();
        await harness.ConnectionStarted.Task.WaitAsync(Timeout);

        TaskCompletionSource<bool> cancellationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseCancellation = new();

        // Hold cancellation open to verify that the publish queue is already closed
        using CancellationTokenRegistration registration = harness.ConnectionCancellationToken.Register(() =>
        {
            cancellationStarted.TrySetResult(true);
            releaseCancellation.Wait(TimeSpan.FromSeconds(30)).ShouldBeTrue();
        });

        Task first = harness.ProduceAsync(1);
        await harness.PublishStarted.Task.WaitAsync(Timeout);
        Task second = harness.ProduceAsync(2);
        Task third = harness.ProduceAsync(3);
        Task disconnecting = harness.Client.DisconnectAsync().AsTask();

        try
        {
            await cancellationStarted.Task.WaitAsync(Timeout);
            disconnecting.IsCompleted.ShouldBeFalse();
            await Should.ThrowAsync<ProduceException>(() => harness.ProduceAsync(4).WaitAsync(Timeout));
        }
        finally
        {
            releaseCancellation.Set();
            harness.ReleasePublish.TrySetResult(true);
            await Task.WhenAll(first, second, third, disconnecting).WaitAsync(Timeout);
        }

        harness.Published.ToArray().ShouldBe([1, 2, 3]);
        harness.PublishedBeforeDisconnect.ShouldBeTrue();
    }

    [Fact]
    public async Task DisconnectAsync_ShouldFailEveryPendingPublish_WhenFlushTimesOut()
    {
        await using ProducerHarness harness = new(TimeSpan.FromMilliseconds(100));
        await harness.ConnectAsync();

        Task first = harness.ProduceAsync(1);
        await harness.PublishStarted.Task.WaitAsync(Timeout);
        Task second = harness.ProduceAsync(2);
        Task third = harness.ProduceAsync(3);

        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        await Should.ThrowAsync<ProduceException>(() => first.WaitAsync(Timeout));
        await Should.ThrowAsync<ProduceException>(() => second.WaitAsync(Timeout));
        await Should.ThrowAsync<ProduceException>(() => third.WaitAsync(Timeout));
        harness.Published.ShouldBeEmpty();
        harness.Client.Status.ShouldBe(ClientStatus.Disconnected);

        harness.ReleasePublish.TrySetResult(true);
        await harness.ConnectAsync();
        await harness.ProduceAsync(4).WaitAsync(Timeout);
        harness.Published.ToArray().ShouldBe([4]);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldFailPendingMessages_WhenConnectionIsUnavailable()
    {
        await using ProducerHarness harness = new();
        await harness.ConnectAsync();
        harness.LoseConnection();

        Task first = harness.ProduceAsync(1);
        Task second = harness.ProduceAsync(2);
        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        await Should.ThrowAsync<ProduceException>(() => first.WaitAsync(Timeout));
        await Should.ThrowAsync<ProduceException>(() => second.WaitAsync(Timeout));
        harness.Published.ShouldBeEmpty();
        harness.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task ConnectAsync_ShouldPublishFromNewQueue_AfterGracefulDisconnect()
    {
        await using ProducerHarness harness = new();
        harness.ReleasePublish.TrySetResult(true);
        await harness.ConnectAsync();
        await harness.ProduceAsync(1).WaitAsync(Timeout);
        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        await harness.ConnectAsync();
        await harness.ProduceAsync(2).WaitAsync(Timeout);
        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        harness.Published.ToArray().ShouldBe([1, 2]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProduceAsync_ShouldContinuePublishing_WhenCallerCancelsPendingPublish(bool publishFails)
    {
        await using ProducerHarness harness = new();
        using CancellationTokenSource cancellation = new();
        harness.FailFirstPublish = publishFails;
        await harness.ConnectAsync();

        Task first = harness.Producer.RawProduceAsync([1], cancellationToken: cancellation.Token).AsTask();
        await harness.PublishStarted.Task.WaitAsync(Timeout);
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => first.WaitAsync(Timeout));

        Task second = harness.ProduceAsync(2);
        harness.ReleasePublish.TrySetResult(true);
        await second.WaitAsync(Timeout);
        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        harness.Published.ToArray().ShouldBe(publishFails ? [2] : [1, 2]);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The callback completes before the harness is disposed")]
    public async Task ConnectionLost_ShouldFailPendingPublish_WhenDisconnectionCleanupAwaitsIt()
    {
        await using ProducerHarness harness = new();
        TaskCompletionSource<bool> cleanedUp = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<Task> publishing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Client.Disconnected.AddHandler(async _ =>
        {
            if (cleanedUp.Task.IsCompleted)
                return;

            Task pendingPublish = await publishing.Task.WaitAsync(Timeout);
            await Should.ThrowAsync<ProduceException>(() => pendingPublish.WaitAsync(Timeout));
            harness.ConnectionAttempts.ShouldBe(1);
            cleanedUp.TrySetResult(true);
        });
        await harness.ConnectAsync();
        harness.LoseConnection();
        publishing.SetResult(harness.ProduceAsync(1));

        await cleanedUp.Task.WaitAsync(Timeout);

        // Wait until the wrapper has returned from the cleanup callback before publishing again
        await WaitUntilAsync(() => harness.ConnectionAttempts >= 2);
        harness.RestoreConnection();
        harness.ReleasePublish.TrySetResult(true);
        await harness.ProduceAsync(2).WaitAsync(Timeout);
        harness.Published.ToArray().ShouldBe([2]);
    }

    [Fact]
    public async Task ConnectionLost_ShouldNotifyOncePerConnection_DespiteFailedReconnectAttempts()
    {
        await using ProducerHarness harness = new();
        int notifications = 0;
        harness.Client.Disconnected.AddHandler(_ =>
        {
            Interlocked.Increment(ref notifications);

            return ValueTask.CompletedTask;
        });
        harness.ReleasePublish.TrySetResult(true);
        await harness.ConnectAsync();

        for (int loss = 1; loss <= 2; loss++)
        {
            int previousAttempts = harness.ConnectionAttempts;
            harness.LoseConnection();
            await WaitUntilAsync(() => harness.ConnectionAttempts >= previousAttempts + 2);
            notifications.ShouldBe(loss);

            harness.RestoreConnection();
            await harness.ProduceAsync((byte)loss).WaitAsync(Timeout);
            notifications.ShouldBe(loss);
        }

        harness.Published.ToArray().ShouldBe([1, 2]);
    }

    [Fact]
    public async Task ConnectionLost_ShouldRetryCleanup_WhenDisconnectedHandlerFails()
    {
        await using ProducerHarness harness = new();
        int notifications = 0;
        TaskCompletionSource<bool> cleanedUp = new(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Client.Disconnected.AddHandler(_ =>
        {
            if (Interlocked.Increment(ref notifications) == 1)
                throw new InvalidOperationException("Cleanup failed.");

            cleanedUp.TrySetResult(true);

            return ValueTask.CompletedTask;
        });
        await harness.ConnectAsync();
        harness.LoseConnection();

        await cleanedUp.Task.WaitAsync(Timeout);
        notifications.ShouldBe(2);

        // Wait until the wrapper has returned from the cleanup callback before publishing again
        await WaitUntilAsync(() => harness.ConnectionAttempts >= 2);
        harness.RestoreConnection();
        harness.ReleasePublish.TrySetResult(true);
        await harness.ProduceAsync(1).WaitAsync(Timeout);
        notifications.ShouldBe(2);
    }

    [Fact]
    public async Task ConnectionLost_ShouldNotReconnect_WhenDisconnectedHandlerStopsClient()
    {
        await using ProducerHarness harness = new();
        TaskCompletionSource<bool> stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Client.Disconnected.AddHandler(async client =>
        {
            if (client.Status != ClientStatus.Initialized)
                return;

            await client.DisconnectAsync();
            stopped.TrySetResult(true);
        });
        await harness.ConnectAsync();
        harness.LoseConnection();

        await stopped.Task.WaitAsync(Timeout);
        await Task.Delay(TimeSpan.FromSeconds(1));

        harness.ConnectionAttempts.ShouldBe(1);
        harness.Client.Status.ShouldBe(ClientStatus.Disconnected);

        harness.RestoreConnection();
        await harness.ConnectAsync();
        harness.ConnectionAttempts.ShouldBe(2);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldNotLogConnectError_WhenConnectionAttemptIsCanceled()
    {
        await using ProducerHarness harness = new();
        harness.BlockConnection();
        await harness.Client.ConnectAsync();
        await harness.ConnectionStarted.Task.WaitAsync(Timeout);

        await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);
        await harness.ConnectionCanceled.Task.WaitAsync(Timeout);

        // Give the connection loop the time to handle the canceled attempt
        await Task.Delay(100);

        harness.Logger.DidNotReceive(
            LogLevel.Warning,
            typeof(OperationCanceledException),
            eventId: MqttLogEvents.ConnectError.EventId.Id);
        harness.Logger.DidNotReceive(
            LogLevel.Debug,
            typeof(OperationCanceledException),
            eventId: MqttLogEvents.ConnectRetryError.EventId.Id);
    }

    [Fact]
    public async Task ConnectAsync_ShouldLogErrorAndRetry_WhenConnectionAttemptTimesOut()
    {
        await using ProducerHarness harness = new(TimeSpan.FromMilliseconds(100));
        harness.BlockConnection();
        Task connecting = harness.ConnectAsync();

        await harness.ConnectionCanceled.Task.WaitAsync(Timeout);
        harness.UnblockConnection();
        await connecting;

        harness.Client.IsConnected.ShouldBeTrue();
        harness.ConnectionAttempts.ShouldBeGreaterThanOrEqualTo(2);
        harness.Logger.Received(
            LogLevel.Warning,
            typeof(OperationCanceledException),
            eventId: MqttLogEvents.ConnectError.EventId.Id);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using CancellationTokenSource cancellation = new(Timeout);

        while (!condition())
        {
            await Task.Delay(10, cancellation.Token);
        }
    }

    private sealed class SubscriberHarness : IAsyncDisposable
    {
        private readonly IMqttClient _nativeClient = Substitute.For<IMqttClient>();

        private readonly TaskCompletionSource<bool> _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool _isConnected;

        private bool _connectionUnavailable;

        public SubscriberHarness(
            Func<MqttClientConfiguration, MqttClientConfiguration>? configure = null,
            Func<MqttClientConfigurationBuilder, MqttClientConfigurationBuilder>? configureBuilder = null,
            TimeSpan? timeout = null,
            string[]? topics = null)
        {
            _nativeClient.IsConnected.Returns(_ => _isConnected);

            _nativeClient.ConnectAsync(Arg.Any<MqttClientOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                if (_connectionUnavailable)
                    throw new InvalidOperationException("The broker is unavailable.");

                _isConnected = true;

                return new MqttClientConnectResult();
            });

            _nativeClient.SubscribeAsync(Arg.Any<MqttClientSubscribeOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                NativeCalls.Enqueue("Subscribe");

                return Task.FromResult(
                    new MqttClientSubscribeResult(
                        0,
                        [
                            .. call.Arg<MqttClientSubscribeOptions>().TopicFilters.Select(filter =>
                                new MqttClientSubscribeResultItem(
                                    filter,
                                    SubscribeResultCodes.GetValueOrDefault(filter.Topic, MqttClientSubscribeResultCode.GrantedQoS1)))
                        ],
                        string.Empty,
                        []));
            });

            _nativeClient.UnsubscribeAsync(Arg.Any<MqttClientUnsubscribeOptions>(), Arg.Any<CancellationToken>()).Returns(async call =>
            {
                NativeCalls.Enqueue("Unsubscribe");
                UnsubscribedTopicFilters = call.Arg<MqttClientUnsubscribeOptions>().TopicFilters;

                if (UnsubscribeException != null)
                    throw UnsubscribeException;

                // Like MQTTnet, which fails the pending request when the token is canceled
                if (HangUnsubscribe)
                {
                    await Task.Delay(System.Threading.Timeout.Infinite, call.Arg<CancellationToken>())
                        .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                    throw new MqttCommunicationTimedOutException();
                }

                return new MqttClientUnsubscribeResult(
                    0,
                    [
                        .. UnsubscribedTopicFilters.Select(filter =>
                            new MqttClientUnsubscribeResultItem(
                                filter,
                                UnsubscribeResultCodes.GetValueOrDefault(filter, MqttClientUnsubscribeResultCode.Success)))
                    ],
                    string.Empty,
                    []);
            });

            _nativeClient.DisconnectAsync(Arg.Any<MqttClientDisconnectOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                NativeCalls.Enqueue("Disconnect");
                _isConnected = false;

                return Task.CompletedTask;
            });

            MqttClientConfigurationBuilder builder = new MqttClientConfigurationBuilder(Substitute.For<IServiceProvider>())
                .ConnectViaTcp("test-broker")
                .WithClientId("test-client")
                .WithTimeout(timeout ?? TimeSpan.FromSeconds(30))
                .Consume(endpoint => endpoint.ConsumeFrom(topics ?? ["$share/group/topic1", "topic2"]));
            MqttClientConfiguration configuration = (configureBuilder?.Invoke(builder) ?? builder).Build();
            Client = new MqttClientWrapper(
                "client1",
                _nativeClient,
                configure?.Invoke(configuration) ?? configuration,
                Substitute.For<IBrokerClientCallbacksInvoker>(),
                Logger);

            Client.Subscribed.AddHandler(_ =>
            {
                _subscribed.TrySetResult(true);

                return ValueTask.CompletedTask;
            });
        }

        public MqttClientWrapper Client { get; }

        public SilverbackLoggerSubstitute<MqttClientWrapper> Logger { get; } = new();

        public ConcurrentQueue<string> NativeCalls { get; } = new();

        public IReadOnlyCollection<string>? UnsubscribedTopicFilters { get; private set; }

        public Dictionary<string, MqttClientSubscribeResultCode> SubscribeResultCodes { get; } = [];

        public Dictionary<string, MqttClientUnsubscribeResultCode> UnsubscribeResultCodes { get; } = [];

        public Exception? UnsubscribeException { get; set; }

        public bool HangUnsubscribe { get; set; }

        public async Task ConnectAsync()
        {
            await Client.ConnectAsync();
            await _subscribed.Task.WaitAsync(Timeout);
        }

        public void LoseConnection()
        {
            _connectionUnavailable = true;
            _isConnected = false;
        }

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
            _nativeClient.Dispose();
        }
    }

    private sealed class ProducerHarness : IAsyncDisposable
    {
        private readonly IMqttClient _nativeClient = Substitute.For<IMqttClient>();

        private readonly MqttProducerEndpoint _endpoint;

        private TaskCompletionSource<bool> _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool _isConnected;

        private bool _published;

        private bool _connectionUnavailable;

        private bool _connectionBlocked;

        private bool _reportConnectedWhileBlocked;

        private int _connectionAttempts;

        public ProducerHarness(TimeSpan? timeout = null)
        {
            _nativeClient.IsConnected.Returns(_ => _isConnected);

            _nativeClient.ConnectAsync(Arg.Any<MqttClientOptions>(), Arg.Any<CancellationToken>()).Returns(async call =>
            {
                Interlocked.Increment(ref _connectionAttempts);
                ConnectionCancellationToken = call.Arg<CancellationToken>();

                if (_connectionBlocked)
                {
                    _isConnected = _reportConnectedWhileBlocked;
                    ConnectionStarted.TrySetResult(true);
                    await Task.Delay(System.Threading.Timeout.Infinite, ConnectionCancellationToken)
                        .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                    ConnectionCanceled.TrySetResult(true);
                    ConnectionCancellationToken.ThrowIfCancellationRequested();
                }

                if (_connectionUnavailable)
                    throw new InvalidOperationException("The broker is unavailable.");

                _isConnected = true;

                return new MqttClientConnectResult();
            });

            _nativeClient.PublishAsync(Arg.Any<MqttApplicationMessage>(), Arg.Any<CancellationToken>()).Returns(async call =>
            {
                PublishCancellationToken = call.Arg<CancellationToken>();
                PublishStarted.TrySetResult(true);
                await ReleasePublish.Task.WaitAsync(PublishCancellationToken);
                byte number = call.Arg<MqttApplicationMessage>().Payload.ToArray()[0];

                if (FailFirstPublish && number == 1)
                    throw new InvalidOperationException("Publishing failed.");

                _published = true;
                Published.Enqueue(number);

                return new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, null, []);
            });

            _nativeClient.DisconnectAsync(Arg.Any<MqttClientDisconnectOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                PublishedBeforeDisconnect = _published;
                _isConnected = false;

                return Task.CompletedTask;
            });

            MqttClientConfiguration configuration = new MqttClientConfigurationBuilder(Substitute.For<IServiceProvider>())
                .ConnectViaTcp("shutdown-test-broker")
                .WithClientId("shutdown-test")
                .WithTimeout(timeout ?? TimeSpan.FromSeconds(30))
                .Produce<string>(endpoint => endpoint.ProduceTo("output"))
                .Build();
            _endpoint = new MqttProducerEndpoint("output", configuration.ProducerEndpoints.Single());
            Client = new MqttClientWrapper(
                "shutdown-test",
                _nativeClient,
                configuration,
                Substitute.For<IBrokerClientCallbacksInvoker>(),
                Logger);
            IBrokerBehaviorsProvider<IProducerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>();
            behaviors.GetBehaviorsList().Returns([]);
            Producer = new MqttProducer(
                "shutdown-test",
                Client,
                configuration,
                behaviors,
                Substitute.For<IServiceProvider>(),
                Substitute.For<ISilverbackLogger<MqttProducer>>());

            Client.Subscribed.AddHandler(_ =>
            {
                _subscribed.TrySetResult(true);

                return ValueTask.CompletedTask;
            });
        }

        public MqttClientWrapper Client { get; }

        public MqttProducer Producer { get; }

        public SilverbackLoggerSubstitute<MqttClientWrapper> Logger { get; } = new();

        public ConcurrentQueue<byte> Published { get; } = new();

        public TaskCompletionSource<bool> PublishStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> ReleasePublish { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> ConnectionStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> ConnectionCanceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken PublishCancellationToken { get; private set; }

        public CancellationToken ConnectionCancellationToken { get; private set; }

        public bool PublishedBeforeDisconnect { get; private set; }

        public bool FailFirstPublish { get; set; }

        public int ConnectionAttempts => _connectionAttempts;

        public async Task ConnectAsync()
        {
            _subscribed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await Client.ConnectAsync();
            await _subscribed.Task.WaitAsync(Timeout);
        }

        public void LoseConnection()
        {
            _connectionUnavailable = true;
            _isConnected = false;
        }

        public void RestoreConnection() => _connectionUnavailable = false;

        public void BlockConnection(bool reportConnected = false)
        {
            _reportConnectedWhileBlocked = reportConnected;
            _connectionBlocked = true;
        }

        public void UnblockConnection() => _connectionBlocked = false;

        public Task ProduceAsync(byte number)
        {
            TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Client.Produce([number], null, _endpoint, _ => completion.SetResult(true), completion.SetException);

            return completion.Task;
        }

        public async ValueTask DisposeAsync()
        {
            ReleasePublish.TrySetResult(true);
            await Client.DisposeAsync();
            Producer.Dispose();
            _nativeClient.Dispose();
        }
    }
}
