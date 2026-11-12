// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Consuming;

public class PublisherConsumerBehaviorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task HandleAsync_ShouldExposeUnboundedSequenceAbortedWithError_WhenStreamSubscriberThrowsOperationCanceledException()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        static async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne dummy in stream)
            {
                // Not caused by the consumer stop (e.g. a timed out HTTP request)
                throw new TaskCanceledException("Unrelated timeout");
            }
        }

        using ConsumerPipelineContext context = CreateStreamingContext(serviceProvider);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        Task handling = behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None).AsTask();

        // The failure is rethrown pushing the message and the sequence is exposed to let the TransactionHandlerConsumerBehavior check why
        // it was aborted, since the token might have been canceled in the meantime by a stop that didn't cause the exception
        await Should.ThrowAsync<TaskCanceledException>(handling.WaitAsync(Timeout));
        ISequence unboundedSequence = context.UnboundedSequence.ShouldNotBeNull();
        unboundedSequence.IsAborted.ShouldBeTrue();
        unboundedSequence.AbortReason.ShouldBe(SequenceAbortReason.Error);
    }

    [Fact]
    public async Task HandleAsync_ShouldAbortUnboundedSequenceWithError_WhenStreamSubscriberThrowsOperationCanceledExceptionWhileOtherIsEnumerating()
    {
        // The other subscriber is registered first: the test fails if the cancellation of its stream, aborted because of the failure, is
        // rethrown instead of the exception of the failed subscriber
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(EnumerateStreamAsync)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        static async Task EnumerateStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne dummy in stream)
            {
                // Just enumerate
            }
        }

        static async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne dummy in stream)
            {
                // Not caused by the consumer stop (e.g. a timed out HTTP request)
                throw new TaskCanceledException("Unrelated timeout");
            }
        }

        using ConsumerPipelineContext context = CreateStreamingContext(serviceProvider);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        Task handling = behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None).AsTask();

        // The exception of the failed subscriber is rethrown, not the cancellation of the stream of the other subscriber
        await Should.ThrowAsync<TaskCanceledException>(handling.WaitAsync(Timeout));
        ISequence unboundedSequence = context.UnboundedSequence.ShouldNotBeNull();
        unboundedSequence.IsAborted.ShouldBeTrue();
        unboundedSequence.AbortReason.ShouldBe(SequenceAbortReason.Error);
        unboundedSequence.AbortException.ShouldBeOfType<TaskCanceledException>().Message.ShouldBe("Unrelated timeout");
    }

    [Fact]
    public async Task HandleAsync_ShouldNotAbortUnboundedSequenceWithError_WhenStreamSubscriberThrowsStopCancellationWhileOtherIsEnumerating()
    {
        TaskCompletionSource processingStartedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(EnumerateStreamAsync)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, CancellationToken>(HandleStreamAsync));

        static async Task EnumerateStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne dummy in stream)
            {
                // Ignore the cancellation token
            }
        }

        async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream, CancellationToken cancellationToken)
        {
            await foreach (TestEventOne dummy in stream)
            {
                processingStartedTaskCompletionSource.TrySetResult();
                await Task.Delay(System.Threading.Timeout.Infinite, cancellationToken);
            }
        }

        using ConsumerPipelineContext context = CreateStreamingContext(serviceProvider);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());
        using CancellationTokenSource cancellationTokenSource = new();

        Task handling = behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, cancellationTokenSource.Token).AsTask();

        // The consumer is stopping
        await processingStartedTaskCompletionSource.Task.WaitAsync(Timeout);
        await cancellationTokenSource.CancelAsync();

        // The streams end as if all the subscribers returned (EnumerationAborted), or the failed subscriber aborts the sequence as the
        // consumer stop does (ConsumerAborted), whichever comes first: the message is rolled back without applying the error policies
        // either way, since the cancellation requested by the stop is rethrown
        await Should.ThrowAsync<OperationCanceledException>(handling.WaitAsync(Timeout));
        ISequence unboundedSequence = context.UnboundedSequence.ShouldNotBeNull();
        unboundedSequence.IsAborted.ShouldBeTrue();
        unboundedSequence.AbortReason.ShouldBeOneOf(SequenceAbortReason.EnumerationAborted, SequenceAbortReason.ConsumerAborted);
    }

    private static ConsumerPipelineContext CreateStreamingContext(IServiceProvider serviceProvider)
    {
        TestConsumerEndpoint endpoint = new TestConsumerEndpointConfiguration("test") { AllowStreaming = true }.GetDefaultEndpoint();
        IConsumer consumer = Substitute.For<IConsumer>();

        return ConsumerPipelineContextHelper.CreateSubstitute(
            new InboundEnvelope<TestEventOne>(new TestEventOne(), Stream.Null, [], endpoint, consumer, new TestOffset()),
            serviceProvider,
            consumer: consumer,
            sequenceStore: new SequenceStore(new SilverbackLoggerSubstitute<SequenceStore>()));
    }
}
