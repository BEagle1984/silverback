// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Unbounded;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Unbounded;

public class UnboundedSequenceTests
{
    [Fact]
    public void Dispose_ShouldReleaseServiceScopeWithoutDisposingContext()
    {
        (ConsumerPipelineContext context, IServiceScope serviceScope, _) = CreateContext();
        UnboundedSequence sequence = new("unbounded|test", context);

        sequence.Dispose();

        serviceScope.DidNotReceive().Dispose();

        context.Dispose();

        serviceScope.Received(1).Dispose();
    }

    [Fact]
    public void Dispose_ShouldDisposeServiceScope_WhenContextIsAlreadyDisposed()
    {
        (ConsumerPipelineContext context, IServiceScope serviceScope, _) = CreateContext();
        UnboundedSequence sequence = new("unbounded|test", context);

        context.Dispose();

        serviceScope.DidNotReceive().Dispose();

        sequence.Dispose();

        serviceScope.Received(1).Dispose();
    }

    [Fact]
    public async Task AbortAsync_ShouldNotRollBackTransaction_WhenAbortedBecauseOfError()
    {
        (ConsumerPipelineContext context, _, IConsumerTransactionManager transactionManager) = CreateContext();
        using UnboundedSequence sequence = new("unbounded|test", context);
        InvalidOperationException exception = new("Test");

        await sequence.AbortAsync(SequenceAbortReason.Error, exception);

        await transactionManager.DidNotReceiveWithAnyArgs().RollbackAsync(null);
        sequence.IsAborted.ShouldBeTrue();
        sequence.AbortException.ShouldBeSameAs(exception);
        sequence.ProcessingCompletedTask.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task AbortAsync_ShouldNotRemoveFromStore_WhenAbortedBecauseOfError()
    {
        (ConsumerPipelineContext context, _, _) = CreateContext();
        using UnboundedSequence sequence = new("unbounded|test", context);

        await sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("Test"));

        await context.SequenceStore.DidNotReceiveWithAnyArgs().RemoveAsync(null!);
    }

    [Fact]
    public async Task AbortAsync_ShouldRemoveFromStore_WhenAbortedBecauseOfConsumerAbort()
    {
        (ConsumerPipelineContext context, _, _) = CreateContext();
        using UnboundedSequence sequence = new("unbounded|test", context);

        await sequence.AbortAsync(SequenceAbortReason.ConsumerAborted);

        await context.SequenceStore.Received(1).RemoveAsync("unbounded|test");
    }

    [Fact]
    public void EndAdd_ShouldReturnFalse_WhenNotAborted()
    {
        (ConsumerPipelineContext context, _, _) = CreateContext();
        using UnboundedSequence sequence = new("unbounded|test", context);

        sequence.BeginAdd();
        bool result = sequence.EndAdd();

        result.ShouldBeFalse();
        sequence.IsErrorHandledByAbort.ShouldBeFalse();
    }

    [Fact]
    public async Task EndAdd_ShouldReturnFalse_WhenAbortedBecauseOfConsumerAbort()
    {
        (ConsumerPipelineContext context, _, _) = CreateContext();
        using UnboundedSequence sequence = new("unbounded|test", context);

        sequence.BeginAdd();
        await sequence.AbortAsync(SequenceAbortReason.ConsumerAborted);
        bool result = sequence.EndAdd();

        result.ShouldBeFalse();
        sequence.TryClaimErrorForAbort().ShouldBeFalse();
    }

    [Fact]
    public async Task EndAdd_ShouldReturnTrue_WhenAbortedBecauseOfConsumerAbortWithException()
    {
        (ConsumerPipelineContext context, _, _) = CreateContext();
        using UnboundedSequence sequence = new("unbounded|test", context);

        sequence.BeginAdd();
        await sequence.AbortAsync(SequenceAbortReason.ConsumerAborted, new OperationCanceledException());
        bool result = sequence.EndAdd();

        result.ShouldBeTrue();
        sequence.TryClaimErrorForAbort().ShouldBeFalse();
        sequence.IsErrorHandledByAbort.ShouldBeFalse();
    }

    [Fact]
    public async Task EndAdd_ShouldReturnTrueAndClaimError_WhenAbortedBecauseOfErrorWhileAdding()
    {
        (ConsumerPipelineContext context, _, _) = CreateContext();
        using UnboundedSequence sequence = new("unbounded|test", context);

        sequence.BeginAdd();
        await sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("Test"));
        bool claimedForAbort = sequence.TryClaimErrorForAbort();
        bool result = sequence.EndAdd();

        claimedForAbort.ShouldBeFalse();
        result.ShouldBeTrue();
        sequence.TryClaimErrorForAbort().ShouldBeFalse();
        sequence.IsErrorHandledByAbort.ShouldBeFalse();
    }

    [Fact]
    public async Task TryClaimErrorForAbort_ShouldReturnTrue_WhenAbortedBecauseOfErrorWhileNotAdding()
    {
        (ConsumerPipelineContext context, _, _) = CreateContext();
        using UnboundedSequence sequence = new("unbounded|test", context);

        sequence.BeginAdd();
        sequence.EndAdd().ShouldBeFalse();
        await sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("Test"));
        bool result = sequence.TryClaimErrorForAbort();

        result.ShouldBeTrue();
        sequence.IsErrorHandledByAbort.ShouldBeTrue();
        sequence.TryClaimErrorForAbort().ShouldBeFalse();
    }

    [Fact]
    public async Task EndAdd_ShouldReturnTrueWithoutClaimingError_WhenErrorIsAlreadyHandledByAbort()
    {
        (ConsumerPipelineContext context, _, _) = CreateContext();
        using UnboundedSequence sequence = new("unbounded|test", context);
        await sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("Test"));
        sequence.TryClaimErrorForAbort().ShouldBeTrue();

        sequence.BeginAdd();
        bool result = sequence.EndAdd();

        result.ShouldBeTrue();
        sequence.IsErrorHandledByAbort.ShouldBeTrue();
    }

    [Fact]
    public async Task TryClaimErrorForAbort_ShouldReturnFalse_WhenErrorIsAlreadyClaimedByPipeline()
    {
        (ConsumerPipelineContext context, _, _) = CreateContext();
        using UnboundedSequence sequence = new("unbounded|test", context);
        await sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("Test"));

        sequence.BeginAdd();
        sequence.EndAdd().ShouldBeTrue();
        bool result = sequence.TryClaimErrorForAbort();

        result.ShouldBeFalse();
        sequence.IsErrorHandledByAbort.ShouldBeFalse();
    }

    private static (ConsumerPipelineContext Context, IServiceScope ServiceScope, IConsumerTransactionManager TransactionManager)
        CreateContext()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker());
        IServiceScope serviceScope = Substitute.For<IServiceScope>();
        serviceScope.ServiceProvider.Returns(serviceProvider);
        IConsumerTransactionManager transactionManager = Substitute.For<IConsumerTransactionManager>();

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            serviceProvider: serviceProvider,
            transactionManager: transactionManager);
        context.ReplaceServiceScope(serviceScope);

        return (context, serviceScope, transactionManager);
    }
}
