// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics;

public class ActivityProducerBehaviorTests
{
    public ActivityProducerBehaviorTests()
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
    }

    [Fact]
    public async Task HandleAsync_ShouldAddTraceIdHeader()
    {
        Activity activity = new("test");
        activity.SetParentId("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
        activity.Start();
        OutboundEnvelope envelope = new(null, null, TestProducerEndpointConfiguration.GetDefault(), Substitute.For<IProducer>());

        await new ActivityProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Headers.ShouldContain(header =>
            header.Name == DefaultMessageHeaders.TraceId &&
            header.Value != null &&
            header.Value.StartsWith("00-0af7651916cd43dd8448eb211c80319c", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleAsync_ShouldNotAddTraceIdHeader_WhenNoActivity()
    {
        OutboundEnvelope envelope = new(null, null, TestProducerEndpointConfiguration.GetDefault(), Substitute.For<IProducer>());

        await new ActivityProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Headers.ShouldContain(header => header.Name == DefaultMessageHeaders.TraceId && !string.IsNullOrEmpty(header.Value));
    }

    [Fact]
    public async Task HandleAsync_ShouldStopActivity_WhenPipelineReturns()
    {
        ProducerPipelineContext context = new(
            CreateEnvelope(),
            Substitute.For<IProducer>(),
            [],
            (_, _) => ValueTask.CompletedTask,
            Substitute.For<IServiceProvider>());
        Activity? activity = null;

        await new ActivityProducerBehavior().HandleAsync(
            context,
            (nextContext, _) =>
            {
                activity = nextContext.Activity;
                return default;
            },
            CancellationToken.None);

        activity.ShouldNotBeNull().IsStopped.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldStopActivityWhenMessagesAreAcknowledged_WhenProducingWithCallbacks()
    {
        ProducerPipelineContext<int> context = CreateCallbackContext(out ProduceCallbackTracker<int> callbackTracker);
        Activity? activity = null;

        await new ActivityProducerBehavior().HandleAsync(
            context,
            (nextContext, _) =>
            {
                activity = nextContext.Activity;
                callbackTracker.OnProducing();
                return default;
            },
            CancellationToken.None);
        callbackTracker.OnPipelineCompleted();

        activity.ShouldNotBeNull().IsStopped.ShouldBeFalse();
        callbackTracker.Activity.ShouldBe(activity);

        callbackTracker.OnProduced();

        activity.IsStopped.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldStopActivity_WhenProducingWithCallbacksAndPipelineThrows()
    {
        ProducerPipelineContext<int> context = CreateCallbackContext(out _);
        Activity? activity = null;

        Func<Task> act = async () => await new ActivityProducerBehavior().HandleAsync(
            context,
            (nextContext, _) =>
            {
                activity = nextContext.Activity;
                throw new InvalidOperationException("test");
            },
            CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
        activity.ShouldNotBeNull().IsStopped.ShouldBeTrue();
    }

    private static OutboundEnvelope CreateEnvelope() =>
        new(null, null, TestProducerEndpointConfiguration.GetDefault(), Substitute.For<IProducer>());

    private static ProducerPipelineContext<int> CreateCallbackContext(out ProduceCallbackTracker<int> callbackTracker)
    {
        ProducerPipelineContext<int> context = new(
            CreateEnvelope(),
            Substitute.For<IProducer>(),
            [],
            (_, _) => ValueTask.CompletedTask,
            Substitute.For<IServiceProvider>());
        callbackTracker = new ProduceCallbackTracker<int>(context, (_, _) => { }, (_, _) => { }, 0);
        context.CallbackTracker = callbackTracker;
        return context;
    }
}
