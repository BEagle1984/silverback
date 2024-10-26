// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics;

public class ActivityProducerBehaviorFixture
{
    public ActivityProducerBehaviorFixture()
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
    }

    [Fact]
    public async Task HandleAsync_ShouldAddTraceIdHeader()
    {
        Activity activity = new("test");
        activity.SetParentId("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
        activity.Start();
        OutboundEnvelope envelope = new(null, null, TestProducerEndpoint.GetDefault(), Substitute.For<IProducer>());

        await new ActivityProducerBehavior(Substitute.For<IActivityEnricherFactory>()).HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                [],
                (_, _) => ValueTaskFactory.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Headers.Should().Contain(
            header =>
                header.Name == DefaultMessageHeaders.TraceId &&
                header.Value != null &&
                header.Value.StartsWith("00-0af7651916cd43dd8448eb211c80319c", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleAsync_ShouldNotAddTraceIdHeader_WhenNoActivity()
    {
        OutboundEnvelope envelope = new(null, null, TestProducerEndpoint.GetDefault(), Substitute.For<IProducer>());

        await new ActivityProducerBehavior(Substitute.For<IActivityEnricherFactory>()).HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                [],
                (_, _) => ValueTaskFactory.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Headers.Should().Contain(header => header.Name == DefaultMessageHeaders.TraceId && !string.IsNullOrEmpty(header.Value));
    }
}
