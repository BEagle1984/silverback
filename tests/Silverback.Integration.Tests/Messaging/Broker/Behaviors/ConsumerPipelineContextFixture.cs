// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
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

        clonedContext.ShouldNotBeSameAs(context);
        clonedContext.Envelope.ShouldBeSameAs(context.Envelope);
        clonedContext.Consumer.ShouldBeSameAs(context.Consumer);
        clonedContext.SequenceStore.ShouldBeSameAs(context.SequenceStore);
        clonedContext.Pipeline.ShouldBeSameAs(context.Pipeline);
        clonedContext.ServiceProvider.ShouldBeSameAs(context.ServiceProvider);
        clonedContext.CurrentStepIndex.ShouldBe(context.CurrentStepIndex);
    }

    [Fact]
    public void Clone_ShouldReturnNewContextWithProvidedEnvelope()
    {
        IRawInboundEnvelope? newEnvelope = Substitute.For<IRawInboundEnvelope>();
        ConsumerPipelineContext context = CreateContext();

        ConsumerPipelineContext clonedContext = context.Clone(newEnvelope);

        clonedContext.ShouldNotBeSameAs(context);
        clonedContext.Envelope.ShouldBeSameAs(newEnvelope);
        clonedContext.Consumer.ShouldBeSameAs(context.Consumer);
        clonedContext.SequenceStore.ShouldBeSameAs(context.SequenceStore);
        clonedContext.Pipeline.ShouldBeSameAs(context.Pipeline);
        clonedContext.ServiceProvider.ShouldBeSameAs(context.ServiceProvider);
        clonedContext.CurrentStepIndex.ShouldBe(context.CurrentStepIndex);
    }

    private static ConsumerPipelineContext CreateContext() => new(
        Substitute.For<IRawInboundEnvelope>(),
        Substitute.For<IConsumer>(),
        Substitute.For<ISequenceStore>(),
        [],
        Substitute.For<IServiceProvider>());
}
