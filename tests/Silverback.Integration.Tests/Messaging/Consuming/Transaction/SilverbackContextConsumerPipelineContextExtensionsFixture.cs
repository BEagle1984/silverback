// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Consuming.Transaction;

public class SilverbackContextConsumerPipelineContextExtensionsFixture
{
    [Fact]
    public void SetConsumerPipelineContext_ShouldSetPipelineContext()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        ConsumerPipelineContext pipelineContext = ConsumerPipelineContextHelper.CreateSubstitute();

        context.SetConsumerPipelineContext(pipelineContext);

        context.TryGetConsumerPipelineContext(out ConsumerPipelineContext? storedPipelineContext);
        storedPipelineContext.Should().BeSameAs(pipelineContext);
    }

    [Fact]
    public void GetConsumerPipelineContext_ShouldReturnPipelineContext()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        ConsumerPipelineContext pipelineContext = ConsumerPipelineContextHelper.CreateSubstitute();
        context.SetConsumerPipelineContext(pipelineContext);

        ConsumerPipelineContext result = context.GetConsumerPipelineContext();

        result.Should().BeSameAs(pipelineContext);
    }

    [Fact]
    public void GetConsumerPipelineContext_ShouldThrow_WhenNoPipelineContextIsStored()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        Action act = () => context.GetConsumerPipelineContext();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TryGetConsumerPipelineContext_ShouldReturnTrue_WhenPipelineContextIsStored()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        ConsumerPipelineContext pipelineContext = ConsumerPipelineContextHelper.CreateSubstitute();
        context.SetConsumerPipelineContext(pipelineContext);

        bool result = context.TryGetConsumerPipelineContext(out ConsumerPipelineContext? storedPipelineContext);

        result.Should().BeTrue();
        storedPipelineContext.Should().BeSameAs(pipelineContext);
    }

    [Fact]
    public void TryGetConsumerPipelineContext_ShouldReturnFalse_WhenNoPipelineContextIsStored()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        bool result = context.TryGetConsumerPipelineContext(out ConsumerPipelineContext? storedPipelineContext);

        result.Should().BeFalse();
        storedPipelineContext.Should().BeNull();
    }
}
