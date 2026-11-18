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
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker.Behaviors;

public class ProduceCallbackTrackerTests
{
    [Fact]
    public async Task OnProduced_ShouldStopActivityWithoutLeavingCallerActivityCurrent_WhenInvokedFromAnotherThread()
    {
        ProducerPipelineContext<int> context = new(
            new OutboundEnvelope(null, null, TestProducerEndpointConfiguration.GetDefault(), Substitute.For<IProducer>()),
            Substitute.For<IProducer>(),
            [],
            (_, _) => ValueTask.CompletedTask,
            Substitute.For<IServiceProvider>());
        Activity? currentActivityInCallback = null;
        ProduceCallbackTracker<int> callbackTracker = new(
            context,
            (_, _) => currentActivityInCallback = Activity.Current,
            (_, _) => { },
            0);

        // The produce activity is a child of the caller's activity, which is what Activity.Stop sets as current
        using Activity callerActivity = new Activity("Caller").Start();
        Activity produceActivity = await Task.Run(() => new Activity("Produce").Start());
        callbackTracker.Activity = produceActivity;
        callbackTracker.OnProducing();
        callbackTracker.OnPipelineCompleted();

        // The acknowledgment is delivered from a thread to which the caller's execution context doesn't flow
        Task<Activity?> deliveryTask;
        using (ExecutionContext.SuppressFlow())
        {
            deliveryTask = Task.Run(() =>
            {
                callbackTracker.OnProduced();
                return Activity.Current;
            });
        }

        Activity? currentActivityAfterDelivery = await deliveryTask;

        produceActivity.IsStopped.ShouldBeTrue();
        currentActivityInCallback.ShouldBeNull();
        currentActivityAfterDelivery.ShouldBeNull();
    }
}
