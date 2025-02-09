// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
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

        clonedContext.ShouldNotBeSameAs(context);
        clonedContext.Envelope.ShouldBeSameAs(context.Envelope);
        clonedContext.Producer.ShouldBeSameAs(context.Producer);
        clonedContext.Pipeline.ShouldBeSameAs(context.Pipeline);
        clonedContext.ServiceProvider.ShouldBeSameAs(context.ServiceProvider);
        clonedContext.CurrentStepIndex.ShouldBe(context.CurrentStepIndex);
    }

    [Fact]
    public void Clone_ShouldReturnNewIdenticalContextWithCallbacks()
    {
        ProducerPipelineContext<int> context = CreateContextWithCallbacks();

        ProducerPipelineContext<int> clonedContext = (ProducerPipelineContext<int>)context.Clone();

        clonedContext.ShouldNotBeSameAs(context);
        clonedContext.Envelope.ShouldBeSameAs(context.Envelope);
        clonedContext.Producer.ShouldBeSameAs(context.Producer);
        clonedContext.Pipeline.ShouldBeSameAs(context.Pipeline);
        clonedContext.ServiceProvider.ShouldBeSameAs(context.ServiceProvider);
        clonedContext.CurrentStepIndex.ShouldBe(context.CurrentStepIndex);
        clonedContext.OnSuccess.ShouldBe(context.OnSuccess);
        clonedContext.OnError.ShouldBe(context.OnError);
        clonedContext.CallbackState.ShouldBe(context.CallbackState);
    }

    [Fact]
    public void Clone_ShouldReturnNewContextWithReplacedEnvelope()
    {
        IOutboundEnvelope? newEnvelope = Substitute.For<IOutboundEnvelope>();
        ProducerPipelineContext context = CreateContext();

        ProducerPipelineContext clonedContext = context.Clone(newEnvelope);

        clonedContext.ShouldNotBeSameAs(context);
        clonedContext.Envelope.ShouldBeSameAs(newEnvelope);
        clonedContext.Producer.ShouldBeSameAs(context.Producer);
        clonedContext.Pipeline.ShouldBeSameAs(context.Pipeline);
        clonedContext.ServiceProvider.ShouldBeSameAs(context.ServiceProvider);
        clonedContext.CurrentStepIndex.ShouldBe(context.CurrentStepIndex);
    }

    [Fact]
    public void Clone_ShouldReturnNewContextWithCallbacksWithReplacedEnvelope()
    {
        IOutboundEnvelope? newEnvelope = Substitute.For<IOutboundEnvelope>();
        ProducerPipelineContext<int> context = CreateContextWithCallbacks();

        ProducerPipelineContext<int> clonedContext = (ProducerPipelineContext<int>)context.Clone(newEnvelope);

        clonedContext.ShouldNotBeSameAs(context);
        clonedContext.Envelope.ShouldBeSameAs(newEnvelope);
        clonedContext.Producer.ShouldBeSameAs(context.Producer);
        clonedContext.Pipeline.ShouldBeSameAs(context.Pipeline);
        clonedContext.ServiceProvider.ShouldBeSameAs(context.ServiceProvider);
        clonedContext.CurrentStepIndex.ShouldBe(context.CurrentStepIndex);
        clonedContext.OnSuccess.ShouldBe(context.OnSuccess);
        clonedContext.OnError.ShouldBe(context.OnError);
        clonedContext.CallbackState.ShouldBe(context.CallbackState);
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
