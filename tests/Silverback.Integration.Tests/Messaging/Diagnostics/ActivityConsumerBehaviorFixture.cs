// Copyright (c) 2024 Sergio Aquilini
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
using Silverback.Messaging.Sequences;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics;

public class ActivityConsumerBehaviorFixture
{
    public ActivityConsumerBehaviorFixture()
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
    }

    [Fact]
    public async Task HandleAsync_ShouldStartNewActivityAndSetParentIdFromHeader()
    {
        RawInboundEnvelope rawEnvelope = new(
            new byte[5],
            new MessageHeaderCollection
            {
                {
                    DefaultMessageHeaders.TraceId,
                    "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"
                }
            },
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        bool entered = false;
        await new ActivityConsumerBehavior(Substitute.For<IActivityEnricherFactory>())
            .HandleAsync(
                new ConsumerPipelineContext(
                    rawEnvelope,
                    Substitute.For<IConsumer>(),
                    Substitute.For<ISequenceStore>(),
                    [],
                    Substitute.For<IServiceProvider>()),
                (_, _) =>
                {
                    Activity.Current.ShouldNotBeNull();
                    Activity.Current.ParentId.ShouldBe("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
                    Activity.Current.Id.ShouldStartWith("00-0af7651916cd43dd8448eb211c80319c");

                    entered = true;

                    return default;
                },
                CancellationToken.None);

        entered.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldStartNewActivity_WhenNoHeaderIsSet()
    {
        RawInboundEnvelope rawEnvelope = new(
            new byte[5],
            new MessageHeaderCollection
            {
                {
                    DefaultMessageHeaders.TraceId,
                    "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"
                }
            },
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        bool entered = false;
        await new ActivityConsumerBehavior(Substitute.For<IActivityEnricherFactory>())
            .HandleAsync(
                new ConsumerPipelineContext(
                    rawEnvelope,
                    Substitute.For<IConsumer>(),
                    Substitute.For<ISequenceStore>(),
                    [],
                    Substitute.For<IServiceProvider>()),
                (_, _) =>
                {
                    Activity.Current.ShouldNotBeNull();
                    Activity.Current.Id.ShouldNotBeNullOrEmpty();

                    entered = true;

                    return default;
                },
                CancellationToken.None);

        entered.ShouldBeTrue();
    }
}
