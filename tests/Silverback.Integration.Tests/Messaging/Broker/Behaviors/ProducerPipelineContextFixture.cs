// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker.Behaviors;

public class ProducerPipelineContextFixture
{
    [Fact]
    public void Clone_ShouldReturnNewContext()
    {
        ProducerPipelineContext context = CreateContext();

        ProducerPipelineContext clonedContext = context.Clone();

        clonedContext.Should().NotBeSameAs(context);
        clonedContext.Envelope.Should().BeSameAs(context.Envelope);
        clonedContext.Producer.Should().BeSameAs(context.Producer);
        clonedContext.Pipeline.Should().BeSameAs(context.Pipeline);
        clonedContext.ServiceProvider.Should().BeSameAs(context.ServiceProvider);
        clonedContext.CurrentStepIndex.Should().Be(context.CurrentStepIndex);
    }

    [Fact]
    public void Clone_ShouldReturnNewContextWithProvidedEnvelope()
    {
        IOutboundEnvelope? newEnvelope = Substitute.For<IOutboundEnvelope>();
        ProducerPipelineContext context = CreateContext();

        ProducerPipelineContext clonedContext = context.Clone(newEnvelope);

        clonedContext.Should().NotBeSameAs(context);
        clonedContext.Envelope.Should().BeSameAs(newEnvelope);
        clonedContext.Producer.Should().BeSameAs(context.Producer);
        clonedContext.Pipeline.Should().BeSameAs(context.Pipeline);
        clonedContext.ServiceProvider.Should().BeSameAs(context.ServiceProvider);
        clonedContext.CurrentStepIndex.Should().Be(context.CurrentStepIndex);
    }

    private static ProducerPipelineContext CreateContext() => new(
        Substitute.For<IOutboundEnvelope>(),
        Substitute.For<IProducer>(),
        [],
        (_, _) => ValueTaskFactory.CompletedTask,
        Substitute.For<IServiceProvider>());
}
