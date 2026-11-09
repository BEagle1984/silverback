// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker.Behaviors;

public class ConsumerPipelineContextTests
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

    [Fact]
    public void Dispose_ShouldDisposeServiceScope()
    {
        ConsumerPipelineContext context = CreateContext();
        IServiceScope serviceScope = Substitute.For<IServiceScope>();
        context.ReplaceServiceScope(serviceScope);

        context.Dispose();

        serviceScope.Received(1).Dispose();
    }

    [Fact]
    public void ReplaceServiceScope_ShouldDisposePreviousServiceScope()
    {
        ConsumerPipelineContext context = CreateContext();
        IServiceScope serviceScope1 = Substitute.For<IServiceScope>();
        IServiceScope serviceScope2 = Substitute.For<IServiceScope>();
        context.ReplaceServiceScope(serviceScope1);

        context.ReplaceServiceScope(serviceScope2);

        serviceScope1.Received(1).Dispose();
        serviceScope2.DidNotReceive().Dispose();
        context.ServiceProvider.ShouldBeSameAs(serviceScope2.ServiceProvider);
    }

    [Fact]
    public void LeaseServiceScope_ShouldDisposeServiceScope_WhenContextAndLeaseAreDisposed()
    {
        ConsumerPipelineContext context = CreateContext();
        IServiceScope serviceScope = Substitute.For<IServiceScope>();
        context.ReplaceServiceScope(serviceScope);

        IDisposable? lease = context.LeaseServiceScope();
        context.Dispose();

        serviceScope.DidNotReceive().Dispose();

        lease.ShouldNotBeNull();
        lease.Dispose();

        serviceScope.Received(1).Dispose();
    }

    [Fact]
    public void LeaseServiceScope_ShouldDisposeServiceScope_WhenLeaseIsReleasedBeforeContext()
    {
        ConsumerPipelineContext context = CreateContext();
        IServiceScope serviceScope = Substitute.For<IServiceScope>();
        context.ReplaceServiceScope(serviceScope);

        IDisposable? lease = context.LeaseServiceScope();
        lease.ShouldNotBeNull();
        lease.Dispose();

        serviceScope.DidNotReceive().Dispose();

        context.Dispose();

        serviceScope.Received(1).Dispose();
    }

    [Fact]
    public void LeaseServiceScope_ShouldReleaseScopeOnlyOnce_WhenLeaseIsDisposedTwice()
    {
        ConsumerPipelineContext context = CreateContext();
        IServiceScope serviceScope = Substitute.For<IServiceScope>();
        context.ReplaceServiceScope(serviceScope);

        IDisposable? lease1 = context.LeaseServiceScope();
        IDisposable? lease2 = context.LeaseServiceScope();
        lease1.ShouldNotBeNull();
        lease2.ShouldNotBeNull();
        lease1.Dispose();
        lease1.Dispose();
        context.Dispose();

        serviceScope.DidNotReceive().Dispose();

        lease2.Dispose();

        serviceScope.Received(1).Dispose();
    }

    [Fact]
    public void LeaseServiceScope_ShouldKeepLeasedScope_WhenServiceScopeIsReplaced()
    {
        ConsumerPipelineContext context = CreateContext();
        IServiceScope serviceScope1 = Substitute.For<IServiceScope>();
        IServiceScope serviceScope2 = Substitute.For<IServiceScope>();
        context.ReplaceServiceScope(serviceScope1);
        IDisposable? lease = context.LeaseServiceScope();

        context.ReplaceServiceScope(serviceScope2);

        serviceScope1.DidNotReceive().Dispose();

        lease.ShouldNotBeNull();
        lease.Dispose();

        serviceScope1.Received(1).Dispose();
        serviceScope2.DidNotReceive().Dispose();
    }

    [Fact]
    public void LeaseServiceScope_ShouldReturnNull_WhenContextDoesNotOwnServiceScope()
    {
        ConsumerPipelineContext context = CreateContext();

        IDisposable? lease = context.LeaseServiceScope();

        lease.ShouldBeNull();
    }

    [Fact]
    public void LeaseServiceScope_ShouldThrow_WhenContextIsDisposed()
    {
        ConsumerPipelineContext context = CreateContext();
        context.ReplaceServiceScope(Substitute.For<IServiceScope>());
        context.Dispose();

        Action act = () => context.LeaseServiceScope();

        act.ShouldThrow<ObjectDisposedException>();
    }

    private static ConsumerPipelineContext CreateContext() => new(
        Substitute.For<IRawInboundEnvelope>(),
        Substitute.For<IConsumer>(),
        Substitute.For<ISequenceStore>(),
        [],
        Substitute.For<IServiceProvider>());
}
