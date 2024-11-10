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
    public void Clone_ShouldReturnNewIdenticalContext()
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
    public void Clone_ShouldReturnNewIdenticalContextWithCallbacks()
    {
        ProducerPipelineContext<int> context = CreateContextWithCallbacks();

        ProducerPipelineContext<int> clonedContext = (ProducerPipelineContext<int>)context.Clone();

        clonedContext.Should().NotBeSameAs(context);
        clonedContext.Envelope.Should().BeSameAs(context.Envelope);
        clonedContext.Producer.Should().BeSameAs(context.Producer);
        clonedContext.Pipeline.Should().BeSameAs(context.Pipeline);
        clonedContext.ServiceProvider.Should().BeSameAs(context.ServiceProvider);
        clonedContext.CurrentStepIndex.Should().Be(context.CurrentStepIndex);
        clonedContext.OnSuccess.Should().Be(context.OnSuccess);
        clonedContext.OnError.Should().Be(context.OnError);
        clonedContext.CallbackState.Should().Be(context.CallbackState);
    }

    [Fact]
    public void Clone_ShouldReturnNewContextWithReplacedEnvelope()
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

    [Fact]
    public void Clone_ShouldReturnNewContextWithCallbacksWithReplacedEnvelope()
    {
        IOutboundEnvelope? newEnvelope = Substitute.For<IOutboundEnvelope>();
        ProducerPipelineContext<int> context = CreateContextWithCallbacks();

        ProducerPipelineContext<int> clonedContext = (ProducerPipelineContext<int>)context.Clone(newEnvelope);

        clonedContext.Should().NotBeSameAs(context);
        clonedContext.Envelope.Should().BeSameAs(newEnvelope);
        clonedContext.Producer.Should().BeSameAs(context.Producer);
        clonedContext.Pipeline.Should().BeSameAs(context.Pipeline);
        clonedContext.ServiceProvider.Should().BeSameAs(context.ServiceProvider);
        clonedContext.CurrentStepIndex.Should().Be(context.CurrentStepIndex);
        clonedContext.OnSuccess.Should().Be(context.OnSuccess);
        clonedContext.OnError.Should().Be(context.OnError);
        clonedContext.CallbackState.Should().Be(context.CallbackState);
    }

    private static ProducerPipelineContext CreateContext() => new(
        Substitute.For<IOutboundEnvelope>(),
        Substitute.For<IProducer>(),
        [],
        (_, _) => ValueTaskFactory.CompletedTask,
        Substitute.For<IServiceProvider>());

    private static ProducerPipelineContext<int> CreateContextWithCallbacks() => new(
        Substitute.For<IOutboundEnvelope>(),
        Substitute.For<IProducer>(),
        [],
        (_, _) => ValueTaskFactory.CompletedTask,
        Substitute.For<IServiceProvider>())
    {
        OnSuccess = (_, _) =>
        {
        },
        OnError = (_, _) =>
        {
        },
        CallbackState = 42
    };
}
