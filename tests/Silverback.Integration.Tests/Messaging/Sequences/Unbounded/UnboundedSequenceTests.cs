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
