// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class ErrorPoliciesHelperTests
{
    private readonly TestErrorPolicyCallback _callback = new();

    private readonly InvalidOperationException _exception = new("test");

    private readonly ServiceProvider _serviceProvider;

    public ErrorPoliciesHelperTests()
    {
        _serviceProvider = BuildServiceProvider(builder => builder.AddSingletonErrorPolicyCallback(_callback));
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldInvokeStopCallbacks_WhenStopPolicyIsApplied()
    {
        ConsumerPipelineContext context = CreateContext(new StopConsumerErrorPolicy());

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        result.ShouldBeFalse();
        _callback.Invocations.ShouldBe([nameof(IStopConsumerErrorPolicyCallback.OnStoppingConsumerAsync)]);
        _callback.Envelopes.ShouldBe([context.Envelope]);
        _callback.Exceptions.ShouldBe([_exception]);

        // The consumer is stopped by the rollback (see ConsumerTransactionManager)
        await context.Consumer.DidNotReceive().StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldStopConsumerAndRethrow_WhenStopCallbackThrowsForUnhandledError()
    {
        IStopConsumerErrorPolicyCallback stopCallback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        stopCallback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Stop callback failed.")));
        ServiceProvider serviceProvider = BuildServiceProvider(builder => builder.AddSingletonErrorPolicyCallback(stopCallback));
        ConsumerPipelineContext context = CreateContext(new StopConsumerErrorPolicy(), serviceProvider: serviceProvider);

        Func<Task> act = () => ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        // The error handling failed, so the rollback won't stop the consumer
        ErrorPolicyCallbackInvocationException exception = await act.ShouldThrowAsync<ErrorPolicyCallbackInvocationException>();
        exception.InnerException?.Message.ShouldBe("Stop callback failed.");
        await context.Consumer.Received(1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldInvokeStopCallbacks_WhenRetriesAreExhausted()
    {
        ConsumerPipelineContext context = CreateContext(
            new RetryErrorPolicy
            {
                MaxFailedAttempts = 2
            },
            failedAttempts: 3);

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        result.ShouldBeFalse();
        _callback.Invocations.ShouldBe([nameof(IStopConsumerErrorPolicyCallback.OnStoppingConsumerAsync)]);
        _callback.Envelopes.ShouldBe([context.Envelope]);
        _callback.Exceptions.ShouldBe([_exception]);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldInvokeStopCallbacks_WhenExceptionIsExcluded()
    {
        ConsumerPipelineContext context = CreateContext(
            new SkipMessageErrorPolicy
            {
                ExcludedExceptions = [typeof(InvalidOperationException)]
            });

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        result.ShouldBeFalse();
        _callback.Invocations.ShouldBe([nameof(IStopConsumerErrorPolicyCallback.OnStoppingConsumerAsync)]);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldInvokeStopCallbacks_WhenNoPolicyInChainCanHandle()
    {
        ConsumerPipelineContext context = CreateContext(
            new ErrorPolicyChain(
                new RetryErrorPolicy
                {
                    MaxFailedAttempts = 1
                },
                new SkipMessageErrorPolicy
                {
                    MaxFailedAttempts = 1
                }),
            failedAttempts: 3);

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        result.ShouldBeFalse();
        _callback.Invocations.ShouldBe([nameof(IStopConsumerErrorPolicyCallback.OnStoppingConsumerAsync)]);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldNotInvokeStopCallbacks_WhenErrorIsHandled()
    {
        ConsumerPipelineContext context = CreateContext(new SkipMessageErrorPolicy());

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        result.ShouldBeTrue();
        _callback.Invocations.ShouldBe([nameof(ISkipMessageErrorPolicyCallback.OnSkippingMessageAsync)]);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldInvokeStopCallbacksAndRethrow_WhenErrorHandlingThrows()
    {
        ISkipMessageErrorPolicyCallback skipCallback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        skipCallback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Skip callback failed.")));
        ServiceProvider serviceProvider = BuildServiceProvider(builder => builder
            .AddSingletonErrorPolicyCallback(skipCallback)
            .AddSingletonErrorPolicyCallback(_callback));
        ConsumerPipelineContext context = CreateContext(new SkipMessageErrorPolicy(), serviceProvider: serviceProvider);

        Func<Task> act = () => ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        ErrorPolicyCallbackInvocationException exception = await act.ShouldThrowAsync<ErrorPolicyCallbackInvocationException>();
        exception.InnerException?.Message.ShouldBe("Skip callback failed.");
        _callback.Invocations.ShouldBe([nameof(IStopConsumerErrorPolicyCallback.OnStoppingConsumerAsync)]);
        _callback.Envelopes.ShouldBe([context.Envelope]);
        _callback.Exceptions.ShouldBe([_exception]);
        await context.Consumer.Received(1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldRethrowErrorHandlingException_WhenStopCallbackThrows()
    {
        ISkipMessageErrorPolicyCallback skipCallback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        skipCallback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Skip callback failed.")));
        IStopConsumerErrorPolicyCallback stopCallback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        stopCallback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Stop callback failed.")));
        ServiceProvider serviceProvider = BuildServiceProvider(builder => builder
            .AddSingletonErrorPolicyCallback(skipCallback)
            .AddSingletonErrorPolicyCallback(stopCallback));
        ConsumerPipelineContext context = CreateContext(new SkipMessageErrorPolicy(), serviceProvider: serviceProvider);

        Func<Task> act = () => ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        ErrorPolicyCallbackInvocationException exception = await act.ShouldThrowAsync<ErrorPolicyCallbackInvocationException>();
        exception.InnerException?.Message.ShouldBe("Skip callback failed.");
        await stopCallback.Received(1).OnStoppingConsumerAsync(context.Envelope, _exception);
        await context.Consumer.Received(1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ApplyErrorPoliciesAsync_ShouldRollBackUnlessDisabledAndStopConsumer_WhenErrorHandlingThrows(bool isRollbackOnFailure)
    {
        ISkipMessageErrorPolicyCallback skipCallback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        skipCallback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Skip callback failed.")));
        ServiceProvider serviceProvider = BuildServiceProvider(builder => builder.AddSingletonErrorPolicyCallback(skipCallback));
        ConsumerPipelineContext context = CreateContext(
            new SkipMessageErrorPolicy(),
            serviceProvider: serviceProvider,
            isTransactionManagerInitialized: true);
        ConsumerTransactionManager transactionManager = (ConsumerTransactionManager)context.TransactionManager;
        int abortedCount = 0;
        transactionManager.Aborted.AddHandler(_ =>
        {
            abortedCount++;
            return ValueTask.CompletedTask;
        });

        Func<Task> act = () => ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception, isRollbackOnFailure);

        // The rollback is disabled when the context isn't the one of the failed message (e.g., the unbounded sequence aborted while no
        // message was being added)
        ErrorPolicyCallbackInvocationException exception = await act.ShouldThrowAsync<ErrorPolicyCallbackInvocationException>();
        exception.InnerException?.Message.ShouldBe("Skip callback failed.");
        abortedCount.ShouldBe(isRollbackOnFailure ? 1 : 0);
        transactionManager.IsConsumerRolledBack.ShouldBe(isRollbackOnFailure);
        await context.Consumer.Received(isRollbackOnFailure ? 1 : 0).RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.Received(1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldRollBackStopConsumerAndRethrow_WhenIncrementingFailedAttemptsThrows()
    {
        ConsumerPipelineContext context = CreateContext(new SkipMessageErrorPolicy(), isTransactionManagerInitialized: true);
        context.Consumer.IncrementFailedAttempts(context.Envelope).Returns(_ => throw new InvalidOperationException("Increment failed."));
        ConsumerTransactionManager transactionManager = (ConsumerTransactionManager)context.TransactionManager;
        int abortedCount = 0;
        transactionManager.Aborted.AddHandler(_ =>
        {
            abortedCount++;
            return ValueTask.CompletedTask;
        });

        Func<Task> act = () => ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        // The bookkeeping failure is an error handling failure: the stop callbacks are invoked, the transaction is rolled back without
        // committing the consumer and the consumer is stopped
        InvalidOperationException exception = await act.ShouldThrowAsync<InvalidOperationException>();
        exception.Message.ShouldBe("Increment failed.");
        _callback.Invocations.ShouldBe([nameof(IStopConsumerErrorPolicyCallback.OnStoppingConsumerAsync)]);
        abortedCount.ShouldBe(1);
        transactionManager.IsConsumerRolledBack.ShouldBeTrue();
        await context.Consumer.Received(1).RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.Received(1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ApplyErrorPoliciesAsync_ShouldApplyPolicies_WhenStopIfRollbackPreventedIsSetButRollbackIsNotPrevented(
        bool isTransactionManagerInitialized)
    {
        ConsumerPipelineContext context = CreateContext(
            new SkipMessageErrorPolicy(),
            isTransactionManagerInitialized: isTransactionManagerInitialized);

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception, isStopIfRollbackPrevented: true);

        result.ShouldBeTrue();
        _callback.Invocations.ShouldBe([nameof(ISkipMessageErrorPolicyCallback.OnSkippingMessageAsync)]);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldNotApplyPoliciesAndInvokeStopCallbacks_WhenStopIfRollbackPreventedIsSetAndRollbackIsPrevented()
    {
        ConsumerPipelineContext context = CreateContext(new SkipMessageErrorPolicy(), isTransactionManagerInitialized: true);
        await CommitWithFailingCommittedHandlerAsync(context);

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception, isStopIfRollbackPrevented: true);

        // The message was already committed: it can't be skipped, the error is handled as when no policy handles it
        result.ShouldBeFalse();
        _callback.Invocations.ShouldBe([nameof(IStopConsumerErrorPolicyCallback.OnStoppingConsumerAsync)]);
        _callback.Envelopes.ShouldBe([context.Envelope]);
        _callback.Exceptions.ShouldBe([_exception]);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldApplyPolicies_WhenRollbackIsPreventedButStopIfRollbackPreventedIsNotSet()
    {
        ConsumerPipelineContext context = CreateContext(new SkipMessageErrorPolicy(), isTransactionManagerInitialized: true);
        await CommitWithFailingCommittedHandlerAsync(context);

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        // The skip policy is applied, but it can't roll back the committed transaction and reconnects the consumer
        result.ShouldBeTrue();
        _callback.Invocations.ShouldBe([nameof(ISkipMessageErrorPolicyCallback.OnSkippingMessageAsync)]);
        await context.Consumer.Received(1).TriggerReconnectAsync();
    }

    [Fact]
    public async Task RollbackOrStopConsumerAsync_ShouldRollBack_WhenTransactionManagerIsNotConsumerTransactionManager()
    {
        ConsumerPipelineContext context = CreateContext(new StopConsumerErrorPolicy());

        await ErrorPoliciesHelper.RollbackOrStopConsumerAsync(context, _exception);

        await context.TransactionManager.Received(1).RollbackAsync(_exception);
        await context.Consumer.DidNotReceive().StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task RollbackOrStopConsumerAsync_ShouldRollBackAndStopConsumer_WhenTransactionIsNotCompleted()
    {
        ConsumerPipelineContext context = CreateContext(new StopConsumerErrorPolicy(), isTransactionManagerInitialized: true);
        ConsumerTransactionManager transactionManager = (ConsumerTransactionManager)context.TransactionManager;
        int abortedCount = 0;
        transactionManager.Aborted.AddHandler(_ =>
        {
            abortedCount++;
            return ValueTask.CompletedTask;
        });

        await ErrorPoliciesHelper.RollbackOrStopConsumerAsync(context, _exception);

        abortedCount.ShouldBe(1);
        transactionManager.IsConsumerRolledBack.ShouldBeTrue();
        await context.Consumer.Received(1).RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.Received(1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
    }

    [Fact]
    public async Task RollbackOrStopConsumerAsync_ShouldStopConsumerWithoutRollingBack_WhenTransactionWasCommitted()
    {
        ConsumerPipelineContext context = CreateContext(new StopConsumerErrorPolicy(), isTransactionManagerInitialized: true);
        ConsumerTransactionManager transactionManager = (ConsumerTransactionManager)context.TransactionManager;
        await CommitWithFailingCommittedHandlerAsync(context);
        int abortedCount = 0;
        transactionManager.Aborted.AddHandler(_ =>
        {
            abortedCount++;
            return ValueTask.CompletedTask;
        });

        await ErrorPoliciesHelper.RollbackOrStopConsumerAsync(context, _exception);

        // The transaction can't be rolled back anymore after the consumer was committed: the consumer is just stopped
        abortedCount.ShouldBe(0);
        transactionManager.IsConsumerRolledBack.ShouldBeFalse();
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.Received(1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
    }

    // The consumer is committed, but the commit fails afterwards (e.g., a Committed handler throws)
    private static async Task CommitWithFailingCommittedHandlerAsync(ConsumerPipelineContext context)
    {
        ConsumerTransactionManager transactionManager = (ConsumerTransactionManager)context.TransactionManager;
        transactionManager.Committed.AddHandler(_ => throw new InvalidOperationException("Committed handler failed."));

        await Should.ThrowAsync<InvalidOperationException>(transactionManager.CommitAsync);

        transactionManager.IsRollbackPrevented.ShouldBeTrue();
        await context.Consumer.Received(1).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    private static ServiceProvider BuildServiceProvider(Action<SilverbackBuilder> configureBuilder)
    {
        ServiceCollection services = [];

        SilverbackBuilder builder = services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker();

        configureBuilder.Invoke(builder);

        return services.BuildServiceProvider();
    }

    private ConsumerPipelineContext CreateContext(
        IErrorPolicy errorPolicy,
        int failedAttempts = 1,
        IServiceProvider? serviceProvider = null,
        bool isTransactionManagerInitialized = false)
    {
        IConsumer consumer = Substitute.For<IConsumer>();
        InboundEnvelope envelope = new(
            new MemoryStream(),
            [],
            new TestConsumerEndpointConfiguration("test")
            {
                ErrorPolicy = errorPolicy
            }.GetDefaultEndpoint(),
            consumer,
            new TestOffset());
        consumer.IncrementFailedAttempts(envelope).Returns(failedAttempts);

        if (!isTransactionManagerInitialized)
            return ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider ?? _serviceProvider, consumer: consumer);

        ConsumerPipelineContext context = new(envelope, consumer, Substitute.For<ISequenceStore>(), [], serviceProvider ?? _serviceProvider);
        context.TransactionManager = new ConsumerTransactionManager(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        return context;
    }
}
