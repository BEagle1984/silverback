// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker.Behaviors;

public class ConsumerPipelineContextFixture
{
    [Fact]
    public void Clone_ShouldReturnNewContext()
    {
        ConsumerPipelineContext context = CreateContext();

        ConsumerPipelineContext clonedContext = context.Clone();

        clonedContext.Should().NotBeSameAs(context);
        clonedContext.Envelope.Should().BeSameAs(context.Envelope);
        clonedContext.Consumer.Should().BeSameAs(context.Consumer);
        clonedContext.SequenceStore.Should().BeSameAs(context.SequenceStore);
        clonedContext.Pipeline.Should().BeSameAs(context.Pipeline);
        clonedContext.ServiceProvider.Should().BeSameAs(context.ServiceProvider);
        clonedContext.CurrentStepIndex.Should().Be(context.CurrentStepIndex);
    }

    [Fact]
    public void Clone_ShouldReturnNewContextWithProvidedEnvelope()
    {
        IRawInboundEnvelope? newEnvelope = Substitute.For<IRawInboundEnvelope>();
        ConsumerPipelineContext context = CreateContext();

        ConsumerPipelineContext clonedContext = context.Clone(newEnvelope);

        clonedContext.Should().NotBeSameAs(context);
        clonedContext.Envelope.Should().BeSameAs(newEnvelope);
        clonedContext.Consumer.Should().BeSameAs(context.Consumer);
        clonedContext.SequenceStore.Should().BeSameAs(context.SequenceStore);
        clonedContext.Pipeline.Should().BeSameAs(context.Pipeline);
        clonedContext.ServiceProvider.Should().BeSameAs(context.ServiceProvider);
        clonedContext.CurrentStepIndex.Should().Be(context.CurrentStepIndex);
    }

    private static ConsumerPipelineContext CreateContext() => new(
        Substitute.For<IRawInboundEnvelope>(),
        Substitute.For<IConsumer>(),
        Substitute.For<ISequenceStore>(),
        [],
        Substitute.For<IServiceProvider>());
}
