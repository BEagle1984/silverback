// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Sequences.Unbounded;
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

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public async Task HandleAsync_ShouldPublishMessageUnlessFiltered(bool isFiltered, int expectedReceivedCount)
    {
        List<TestEventOne> receivedMessages = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<TestEventOne>(receivedMessages.Add));
        using ConsumerPipelineContext context = CreateContext(serviceProvider, isFiltered);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());
        bool nextCalled = false;

        await behavior.HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None);

        receivedMessages.Count.ShouldBe(expectedReceivedCount);
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotThrow_WhenFilteredMessageIsUnhandled()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker());
        using ConsumerPipelineContext context = CreateContext(serviceProvider, true);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        Task handleTask = behavior.HandleAsync(context, (_, _) => default, CancellationToken.None).AsTask();

        // The filtered message is handled like a message without subscribers when IgnoreUnhandledMessages is set
        context.Envelope.Endpoint.Configuration.ThrowIfUnhandled.ShouldBeTrue();
        await Should.NotThrowAsync(handleTask);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public async Task HandleAsync_ShouldPublishMessageInRawSequenceUnlessFiltered(bool isFiltered, int expectedReceivedCount)
    {
        List<TestEventOne> receivedMessages = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<TestEventOne>(receivedMessages.Add));
        ConsumerPipelineContext context = CreateContext(serviceProvider, isFiltered);
        using ChunkSequence chunkSequence = new("chunk-sequence", 2, context); // Disposes the context
        context.SetSequence(chunkSequence, true);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());
        bool nextCalled = false;

        await behavior.HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None);

        receivedMessages.Count.ShouldBe(expectedReceivedCount);
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotStreamFilteredMessage()
    {
        int receivedCount = 0;
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne dummy in stream)
            {
                Interlocked.Increment(ref receivedCount);
            }
        }

        using ConsumerPipelineContext context = CreateStreamingContext(serviceProvider);
        ((InboundEnvelope)context.Envelope).IsFiltered = true;
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());
        bool nextCalled = false;

        await behavior.HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        // The filtered message doesn't start the unbounded sequence (and therefore doesn't invoke the stream subscribers)
        context.UnboundedSequence.ShouldBeNull();
        context.SequenceStore.ShouldBeEmpty();
        receivedCount.ShouldBe(0);
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public void SortIndex_ShouldBePublisherSortIndex()
    {
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        behavior.SortIndex.ShouldBe(BrokerBehaviorsSortIndexes.Consumer.Publisher);
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishRawEnvelope()
    {
        List<IRawInboundEnvelope> receivedEnvelopes = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IRawInboundEnvelope>(receivedEnvelopes.Add));
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(serviceProvider: serviceProvider);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        await behavior.HandleAsync(context, (_, _) => default, CancellationToken.None);

        // The envelope that isn't deserialized (and therefore can't be filtered) is published as is
        context.Envelope.ShouldBeOfType<RawInboundEnvelope>();
        receivedEnvelopes.ShouldBe([context.Envelope]);
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishSequence()
    {
        List<TestEventOne> receivedMessages = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne message in batch)
            {
                receivedMessages.Add(message);
            }
        }

        ConsumerPipelineContext context = CreateBatchContext(serviceProvider);
        using BatchSequence batchSequence = new("batch", context); // Disposes the context
        context.SetSequence(batchSequence, true);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());
        bool nextCalled = false;

        await behavior.HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None);

        // The batch subscriber processes the sequence asynchronously, while the messages are added to it
        nextCalled.ShouldBeTrue();
        Task processingTask = context.ProcessingTask.ShouldNotBeNull();
        processingTask.IsCompleted.ShouldBeFalse();

        await batchSequence.AddAsync(context.Envelope, null, true).WaitAsync(Timeout);
        await processingTask.WaitAsync(Timeout);

        receivedMessages.Count.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetCompletedProcessingTask_WhenSequenceHasNoSubscribers()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker());
        ConsumerPipelineContext context = CreateBatchContext(serviceProvider);
        using BatchSequence batchSequence = new("batch", context); // Disposes the context
        context.SetSequence(batchSequence, true);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());
        bool nextCalled = false;

        await behavior.HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None);

        nextCalled.ShouldBeTrue();
        context.ProcessingTask.ShouldBe(Task.CompletedTask);
    }

    [Fact]
    public async Task HandleAsync_ShouldStreamMessage_WhenOnlyStreamSubscriberHandlesIt()
    {
        List<TestEventOne> receivedMessages = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne message in stream)
            {
                lock (receivedMessages)
                {
                    receivedMessages.Add(message);
                }
            }
        }

        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context = CreateStreamingContext(serviceProvider, sequenceStore);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        Task handling = behavior.HandleAsync(context, (_, _) => default, CancellationToken.None).AsTask();

        // The streamed message is handled, even if no other subscriber handles it and the unhandled messages aren't ignored
        await Should.NotThrowAsync(handling.WaitAsync(Timeout));
        context.Envelope.Endpoint.Configuration.ThrowIfUnhandled.ShouldBeTrue();
        receivedMessages.ShouldBe([((IInboundEnvelope)context.Envelope).Message]);
        context.UnboundedSequence.ShouldNotBeNull().IsPending.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldAddMessagesToPendingUnboundedSequence()
    {
        int invocationsCount = 0;
        List<TestEventOne> receivedMessages = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);

            await foreach (TestEventOne message in stream)
            {
                lock (receivedMessages)
                {
                    receivedMessages.Add(message);
                }
            }
        }

        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context1 = CreateStreamingContext(serviceProvider, sequenceStore, new TestEventOne { Content = "1" });
        using ConsumerPipelineContext context2 = CreateStreamingContext(serviceProvider, sequenceStore, new TestEventOne { Content = "2" });
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        await behavior.HandleAsync(context1, (_, _) => default, CancellationToken.None).AsTask().WaitAsync(Timeout);
        await behavior.HandleAsync(context2, (_, _) => default, CancellationToken.None).AsTask().WaitAsync(Timeout);

        // The second message is pushed to the stream started by the first one, without invoking the subscriber again
        context2.UnboundedSequence.ShouldNotBeNull().ShouldBeSameAs(context1.UnboundedSequence);
        invocationsCount.ShouldBe(1);
        receivedMessages.Select(message => message.Content).ShouldBe(["1", "2"]);
    }

    [Fact]
    public async Task HandleAsync_ShouldIgnoreAbortedUnboundedSequenceStillInStore()
    {
        int invocationsCount = 0;
        List<TestEventOne> receivedMessages = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);

            await foreach (TestEventOne message in stream)
            {
                lock (receivedMessages)
                {
                    receivedMessages.Add(message);
                }
            }
        }

        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext abortedSequenceContext = CreateStreamingContext(serviceProvider, sequenceStore);
        using ConsumerPipelineContext context1 = CreateStreamingContext(serviceProvider, sequenceStore, new TestEventOne { Content = "1" });
        using ConsumerPipelineContext context2 = CreateStreamingContext(serviceProvider, sequenceStore, new TestEventOne { Content = "2" });
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        // The sequence whose subscribers stopped enumerating stays in the store until its abort completes, ahead of the next sequences
        using UnboundedSequence abortedSequence = new("unbounded|aborted", abortedSequenceContext);
        await abortedSequence.AbortAsync(SequenceAbortReason.EnumerationAborted);
        await sequenceStore.AddAsync(abortedSequence);

        await behavior.HandleAsync(context1, (_, _) => default, CancellationToken.None).AsTask().WaitAsync(Timeout);
        await behavior.HandleAsync(context2, (_, _) => default, CancellationToken.None).AsTask().WaitAsync(Timeout);

        // The first message starts a new sequence instead of being added to the aborted one, and the second one finds the new sequence
        // behind the aborted one
        ISequence unboundedSequence = context1.UnboundedSequence.ShouldNotBeNull();
        unboundedSequence.ShouldNotBeSameAs(abortedSequence);
        unboundedSequence.IsPending.ShouldBeTrue();
        context2.UnboundedSequence.ShouldBeSameAs(unboundedSequence);
        invocationsCount.ShouldBe(1);
        receivedMessages.Select(message => message.Content).ShouldBe(["1", "2"]);
    }

    [Fact]
    public async Task HandleAsync_ShouldRethrowAbortException_WhenUnboundedSequenceAbortedByErrorIsStillInStore()
    {
        int invocationsCount = 0;
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);
            return Task.CompletedTask;
        }

        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext failedSequenceContext = CreateStreamingContext(serviceProvider, sequenceStore);
        using ConsumerPipelineContext context = CreateStreamingContext(serviceProvider, sequenceStore);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());
        InvalidOperationException exception = new("Subscriber failed");

        // The sequence aborted by an error stays in the store until the error is handled
        using UnboundedSequence failedSequence = await sequenceStore.AddAsync(new UnboundedSequence("unbounded|failed", failedSequenceContext));
        await failedSequence.AbortAsync(SequenceAbortReason.Error, exception);

        Task handling = behavior.HandleAsync(context, (_, _) => default, CancellationToken.None).AsTask();

        // The message consumed in the meantime fails with the same error, instead of starting a new stream
        InvalidOperationException thrownException = await Should.ThrowAsync<InvalidOperationException>(handling.WaitAsync(Timeout));
        thrownException.ShouldBeSameAs(exception);
        context.UnboundedSequence.ShouldBeSameAs(failedSequence);
        invocationsCount.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ShouldRethrowPushException_WhenStreamsAreAbortedBeforeUnboundedSequence()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(_ => Task.CompletedTask));
        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext pendingSequenceContext = CreateStreamingContext(serviceProvider, sequenceStore);
        using ConsumerPipelineContext context = CreateStreamingContext(serviceProvider, sequenceStore);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        // The publisher aborts the streams before the sequence, when one of several subscribers fails
        using UnboundedSequence pendingSequence =
            await sequenceStore.AddAsync(new UnboundedSequence("unbounded|pending", pendingSequenceContext));
        ((MessageStreamProvider)pendingSequence.StreamProvider).Abort();

        Task handling = behavior.HandleAsync(context, (_, _) => default, CancellationToken.None).AsTask();

        await Should.ThrowAsync<InvalidOperationException>(handling.WaitAsync(Timeout));
        context.UnboundedSequence.ShouldBeSameAs(pendingSequence);

        // The failed message is no longer being added: the abort handles the error that follows
        await pendingSequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("Subscriber failed"));
        pendingSequence.TryClaimErrorForAbort().ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotPushMessageToNewSequence_WhenSubscriberReturnsWithoutEnumerating()
    {
        int invocationsCount = 0;
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);
            return Task.CompletedTask;
        }

        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context = CreateStreamingContext(serviceProvider, sequenceStore);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        Task handling = behavior.HandleAsync(context, (_, _) => default, CancellationToken.None).AsTask();

        // The subscriber was invoked for the message that started the stream and returned without receiving it: the message is handled
        // (and committed), without invoking the subscriber again
        await Should.NotThrowAsync(handling.WaitAsync(Timeout));
        invocationsCount.ShouldBe(1);
        ISequence unboundedSequence = context.UnboundedSequence.ShouldNotBeNull();
        unboundedSequence.AbortReason.ShouldBe(SequenceAbortReason.EnumerationAborted);
    }

    [Fact]
    public async Task HandleAsync_ShouldPushMessageToNewSequence_WhenSubscriberStopsEnumeratingBeforeReceivingIt()
    {
        int invocationsCount = 0;
        List<TestEventOne> receivedMessages = [];
        TaskCompletionSource firstMessageReceivedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource stopEnumeratingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            bool isFirstInvocation = Interlocked.Increment(ref invocationsCount) == 1;

            await foreach (TestEventOne message in stream)
            {
                lock (receivedMessages)
                {
                    receivedMessages.Add(message);
                }

                if (!isFirstInvocation)
                    continue;

                firstMessageReceivedTaskCompletionSource.TrySetResult();
                await stopEnumeratingTaskCompletionSource.Task;
                break;
            }
        }

        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context1 = CreateStreamingContext(serviceProvider, sequenceStore, new TestEventOne { Content = "1" });
        using ConsumerPipelineContext context2 = CreateStreamingContext(serviceProvider, sequenceStore, new TestEventOne { Content = "2" });
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        Task handling1 = behavior.HandleAsync(context1, (_, _) => default, CancellationToken.None).AsTask();
        await firstMessageReceivedTaskCompletionSource.Task.WaitAsync(Timeout);

        // The second message waits to be pushed to the stream, while the subscriber is still processing the first one
        Task handling2 = behavior.HandleAsync(context2, (_, _) => default, CancellationToken.None).AsTask();
        ISequence firstSequence = context1.UnboundedSequence.ShouldNotBeNull();
        context2.UnboundedSequence.ShouldBeSameAs(firstSequence);

        stopEnumeratingTaskCompletionSource.SetResult();
        await Task.WhenAll(handling1, handling2).WaitAsync(Timeout);

        // The subscriber stopped enumerating without receiving the second message, which is pushed to a new stream, invoking the
        // subscriber again, instead of being committed
        firstSequence.AbortReason.ShouldBe(SequenceAbortReason.EnumerationAborted);
        ISequence secondSequence = context2.UnboundedSequence.ShouldNotBeNull();
        secondSequence.ShouldNotBeSameAs(firstSequence);
        invocationsCount.ShouldBe(2);
        receivedMessages.Select(message => message.Content).ShouldBe(["1", "2"]);
    }

    [Fact]
    public async Task HandleAsync_ShouldPushMessageToNewSequence_WhenPushFailsOnAbortedStreamsWhileSubscribersStoppedEnumerating()
    {
        int invocationsCount = 0;
        List<TestEventOne> receivedMessages = [];
        TaskCompletionSource releaseFirstSubscriberTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AbortOnAddFailedLogger sequenceLogger = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSingleton<ISilverbackLogger<SequenceBase<IInboundEnvelope>>>(sequenceLogger)
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            bool isFirstInvocation = Interlocked.Increment(ref invocationsCount) == 1;

            try
            {
                await foreach (TestEventOne message in stream)
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(message);
                    }
                }
            }
            catch (OperationCanceledException) when (isFirstInvocation)
            {
                // The streams were aborted
            }

            // The first subscriber doesn't return, which would abort the sequence (see AbortAndDisposeWhenProcessedAsync)
            if (isFirstInvocation)
                await releaseFirstSubscriberTaskCompletionSource.Task;
        }

        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context1 = CreateStreamingContext(serviceProvider, sequenceStore, new TestEventOne { Content = "1" });
        using ConsumerPipelineContext context2 = CreateStreamingContext(serviceProvider, sequenceStore, new TestEventOne { Content = "2" });
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        await behavior.HandleAsync(context1, (_, _) => default, CancellationToken.None).AsTask().WaitAsync(Timeout);
        UnboundedSequence firstSequence = context1.UnboundedSequence.ShouldBeOfType<UnboundedSequence>();
        sequenceLogger.SequenceToAbort = firstSequence;

        // The streams are aborted before the sequence, which is still pending
        ((MessageStreamProvider)firstSequence.StreamProvider).AbortIfPending();
        firstSequence.IsPending.ShouldBeTrue();

        Task handling = behavior.HandleAsync(context2, (_, _) => default, CancellationToken.None).AsTask();

        // The push fails on the aborted streams, while the sequence is being aborted because the subscribers stopped enumerating: the
        // message, which no subscriber received, is pushed to a new stream, invoking the subscriber again
        await Should.NotThrowAsync(handling.WaitAsync(Timeout));
        sequenceLogger.AddFailedException.ShouldBeOfType<InvalidOperationException>();
        await sequenceLogger.AbortTask.ShouldNotBeNull().WaitAsync(Timeout);
        firstSequence.AbortReason.ShouldBe(SequenceAbortReason.EnumerationAborted);
        ISequence secondSequence = context2.UnboundedSequence.ShouldNotBeNull();
        secondSequence.ShouldNotBeSameAs(firstSequence);
        secondSequence.IsPending.ShouldBeTrue();
        invocationsCount.ShouldBe(2);
        receivedMessages.Select(message => message.Content).ShouldBe(["1", "2"]);

        releaseFirstSubscriberTaskCompletionSource.SetResult();
    }

    [Fact]
    public async Task HandleAsync_ShouldAbortUnboundedSequenceAndRethrow_WhenStreamSubscribersCannotBeInvoked()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddScopedSubscriber<UnresolvableStreamSubscriber>());
        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context = CreateStreamingContext(serviceProvider, sequenceStore);
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        Task handling = behavior.HandleAsync(context, (_, _) => default, CancellationToken.None).AsTask();

        // The pipeline of the message handles the exception, while the aborted sequence is removed from the store
        InvalidOperationException thrownException = await Should.ThrowAsync<InvalidOperationException>(handling.WaitAsync(Timeout));
        thrownException.Message.ShouldBe("Subscriber can't be resolved");
        context.UnboundedSequence.ShouldBeNull();
        await AsyncTestingUtil.WaitAsync(() => sequenceStore.Count == 0, Timeout);
        sequenceStore.Count.ShouldBe(0);
        await context.Consumer.DidNotReceive().StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task HandleAsync_ShouldLogError_WhenUnboundedSequenceFailsToDispose()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddScoped<DisposeFailingService>()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, DisposeFailingService>(HandleStreamAsync));

        static async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream, DisposeFailingService service)
        {
            await foreach (TestEventOne message in stream)
            {
                if (message.Content == "2")
                    break;
            }
        }

        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        ConsumerPipelineContext context1 = CreateStreamingContext(serviceProvider, sequenceStore, new TestEventOne { Content = "1" });
        using ConsumerPipelineContext context2 = CreateStreamingContext(serviceProvider, sequenceStore, new TestEventOne { Content = "2" });
        context1.ReplaceServiceScope(serviceProvider.CreateScope());
        RecordingLogger logger = new();
        PublisherConsumerBehavior behavior = new(logger);

        // The subscriber, resolving its services from the scope of the message that started the sequence, stops enumerating once the
        // pipeline of that message completed and disposed its context
        await behavior.HandleAsync(context1, (_, _) => default, CancellationToken.None).AsTask().WaitAsync(Timeout);
        ISequence unboundedSequence = context1.UnboundedSequence.ShouldNotBeNull();
        context1.Dispose();
        await behavior.HandleAsync(context2, (_, _) => default, CancellationToken.None).AsTask().WaitAsync(Timeout);

        // The sequence releases the scope once disposed, failing to dispose the scoped service
        Exception? loggedException = await logger.WaitForLogAsync(IntegrationLogEvents.SequenceDisposeError);
        loggedException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Dispose failed");
        unboundedSequence.AbortReason.ShouldBe(SequenceAbortReason.EnumerationAborted);
        context2.UnboundedSequence.ShouldBeSameAs(unboundedSequence);
    }

    [Theory]
    [InlineData("consumer stopped")]
    [InlineData("stop failed")]
    [InlineData("disconnect failed")]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldStopConsumerAndDisconnectClient_WhenStreamSubscriberFailsWhileNoMessageIsAdded(string scenario)
    {
        using CancellationTokenSource failureCancellationTokenSource = new();
        IServiceProvider serviceProvider = GetServiceProviderWithFailingStreamSubscriber(failureCancellationTokenSource.Token);
        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        TestOffset offset = new("test", "1");
        IConsumer consumer = Substitute.For<IConsumer>();
        using ConsumerPipelineContext context = CreateStreamingContext(serviceProvider, sequenceStore, consumer: consumer, identifier: offset);
        RecordingLogger logger = new();
        PublisherConsumerBehavior behavior = new(logger);

        InvalidOperationException stopException = new("Stop failed");
        if (scenario == "stop failed")
        {
            consumer.StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>())
                .Returns(_ => ValueTask.FromException(stopException));
        }

        TaskCompletionSource disconnectedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        consumer.Client.DisconnectAsync().Returns(_ =>
        {
            disconnectedTaskCompletionSource.TrySetResult();
            return scenario == "disconnect failed"
                ? ValueTask.FromException(new InvalidOperationException("Disconnect failed"))
                : ValueTask.CompletedTask;
        });

        await behavior.HandleAsync(context, (_, _) => default, CancellationToken.None).AsTask().WaitAsync(Timeout);

        // The subscriber fails while waiting for the next message: no pipeline rethrows the error, which is handled by the abort,
        // applying the error policies to the message that started the sequence and stopping the consumer
        await failureCancellationTokenSource.CancelAsync();
        await disconnectedTaskCompletionSource.Task.WaitAsync(Timeout);

        await consumer.Received(1).StopAsync(offset, false);
        Exception? loggedException = await logger.WaitForLogAsync(IntegrationLogEvents.ProcessingConsumedMessageFatalError);

        if (scenario == "stop failed")
            loggedException.ShouldBeSameAs(stopException);
        else
            loggedException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Subscriber failed");

        // The sequence is released anyway
        await AsyncTestingUtil.WaitAsync(() => sequenceStore.Count == 0, Timeout);
        sequenceStore.Count.ShouldBe(0);
        ((UnboundedSequence)context.UnboundedSequence.ShouldNotBeNull()).IsErrorHandledByAbort.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldReleaseUnboundedSequence_WhenErrorHandlingThrowsWhileNoMessageIsAdded()
    {
        using CancellationTokenSource failureCancellationTokenSource = new();
        IServiceProvider serviceProvider = GetServiceProviderWithFailingStreamSubscriber(failureCancellationTokenSource.Token);
        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        IConsumer consumer = Substitute.For<IConsumer>();
        using ConsumerPipelineContext context = CreateStreamingContext(serviceProvider, sequenceStore, consumer: consumer);
        RecordingLogger logger = new() { FailingEventId = IntegrationLogEvents.ProcessingConsumedMessageFatalError.EventId.Id };
        PublisherConsumerBehavior behavior = new(logger);

        await behavior.HandleAsync(context, (_, _) => default, CancellationToken.None).AsTask().WaitAsync(Timeout);

        // The subscriber fails while waiting for the next message: the abort handles the error, which throws once the consumer is stopped
        await failureCancellationTokenSource.CancelAsync();
        await logger.WaitForLogAsync(IntegrationLogEvents.ProcessingConsumedMessageFatalError);

        // The sequence is released anyway
        await AsyncTestingUtil.WaitAsync(() => sequenceStore.Count == 0, Timeout);
        sequenceStore.Count.ShouldBe(0);
        await consumer.Received(1).StopAsync(Arg.Any<IBrokerMessageIdentifier>(), false);
        await consumer.Client.DidNotReceive().DisconnectAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotStopConsumer_WhenErrorPolicyHandlesFailureOfStreamSubscriberWhileNoMessageIsAdded()
    {
        using CancellationTokenSource failureCancellationTokenSource = new();
        IServiceProvider serviceProvider = GetServiceProviderWithFailingStreamSubscriber(failureCancellationTokenSource.Token);
        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>());
        IConsumer consumer = Substitute.For<IConsumer>();
        using ConsumerPipelineContext context = CreateStreamingContext(
            serviceProvider,
            sequenceStore,
            consumer: consumer,
            errorPolicy: new SkipMessageErrorPolicyBuilder().Build());
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        await behavior.HandleAsync(context, (_, _) => default, CancellationToken.None).AsTask().WaitAsync(Timeout);

        // The skip error policy, applied by the abort to the message that started the sequence, handles the error
        await failureCancellationTokenSource.CancelAsync();
        await AsyncTestingUtil.WaitAsync(() => sequenceStore.Count == 0, Timeout);

        sequenceStore.Count.ShouldBe(0);
        ((UnboundedSequence)context.UnboundedSequence.ShouldNotBeNull()).IsErrorHandledByAbort.ShouldBeTrue();
        await context.TransactionManager.Received(1).RollbackAsync(
            Arg.Is<Exception?>(exception => exception is InvalidOperationException),
            Arg.Is(true),
            Arg.Any<bool>(),
            Arg.Any<bool>());
        await consumer.DidNotReceive().StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>());
        await consumer.Client.DidNotReceive().DisconnectAsync();
    }

    // The stream subscriber fails while waiting for the next message, once the token is canceled
    private static IServiceProvider GetServiceProviderWithFailingStreamSubscriber(CancellationToken failureCancellationToken)
    {
        return ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            try
            {
                await foreach (TestEventOne dummy in stream.WithCancellation(failureCancellationToken))
                {
                    // Just enumerate
                }
            }
            catch (OperationCanceledException)
            {
                throw new InvalidOperationException("Subscriber failed");
            }
        }
    }

    private static ConsumerPipelineContext CreateContext(IServiceProvider serviceProvider, bool isFiltered = false)
    {
        TestConsumerEndpoint endpoint = TestConsumerEndpoint.GetDefault();
        IConsumer consumer = Substitute.For<IConsumer>();
        InboundEnvelope<TestEventOne> envelope = new(new TestEventOne(), Stream.Null, [], endpoint, consumer, new TestOffset())
        {
            IsFiltered = isFiltered
        };

        return ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            serviceProvider,
            consumer: consumer,
            sequenceStore: new SequenceStore(new SilverbackLoggerSubstitute<SequenceStore>()));
    }

    private static ConsumerPipelineContext CreateBatchContext(IServiceProvider serviceProvider)
    {
        TestConsumerEndpoint endpoint = new TestConsumerEndpointConfiguration("test")
        {
            Batch = new BatchSettings
            {
                Size = 1
            }
        }.GetDefaultEndpoint();
        IConsumer consumer = Substitute.For<IConsumer>();

        return ConsumerPipelineContextHelper.CreateSubstitute(
            new InboundEnvelope<TestEventOne>(new TestEventOne(), Stream.Null, [], endpoint, consumer, new TestOffset()),
            serviceProvider,
            consumer: consumer,
            sequenceStore: new SequenceStore(new SilverbackLoggerSubstitute<SequenceStore>()));
    }

    private static ConsumerPipelineContext CreateStreamingContext(
        IServiceProvider serviceProvider,
        ISequenceStore? sequenceStore = null,
        TestEventOne? message = null,
        IConsumer? consumer = null,
        IBrokerMessageIdentifier? identifier = null,
        IErrorPolicy? errorPolicy = null)
    {
        TestConsumerEndpointConfiguration configuration = new("test") { AllowStreaming = true };

        if (errorPolicy != null)
            configuration = configuration with { ErrorPolicy = errorPolicy };

        TestConsumerEndpoint endpoint = configuration.GetDefaultEndpoint();
        consumer ??= Substitute.For<IConsumer>();

        return ConsumerPipelineContextHelper.CreateSubstitute(
            new InboundEnvelope<TestEventOne>(message ?? new TestEventOne(), Stream.Null, [], endpoint, consumer, identifier ?? new TestOffset()),
            serviceProvider,
            consumer: consumer,
            sequenceStore: sequenceStore ?? new SequenceStore(new SilverbackLoggerSubstitute<SequenceStore>()));
    }

    private sealed class UnresolvableStreamSubscriber
    {
        public UnresolvableStreamSubscriber()
        {
            throw new InvalidOperationException("Subscriber can't be resolved");
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Subscriber method")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used for routing")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task HandleAsync(IAsyncEnumerable<TestEventOne> stream) => Task.CompletedTask;
    }

    [SuppressMessage("Usage", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Simulates a service failing to dispose")]
    private sealed class DisposeFailingService : IDisposable
    {
        public void Dispose() => throw new InvalidOperationException("Dispose failed");
    }

    // Signals the logged events, which are logged asynchronously by the sequence processing, optionally failing to log one of them
    private sealed class RecordingLogger : ISilverbackLogger<PublisherConsumerBehavior>, ILogger
    {
        private readonly ConcurrentDictionary<int, TaskCompletionSource<Exception?>> _loggedEvents = new();

        public int? FailingEventId { get; init; }

        public ILogger InnerLogger => this;

        public bool IsEnabled(LogEvent logEvent) => true;

        public bool IsEnabled(LogLevel logLevel) => true;

        public Task<Exception?> WaitForLogAsync(LogEvent logEvent) => GetLoggedEvent(logEvent.EventId.Id).Task.WaitAsync(Timeout);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            GetLoggedEvent(eventId.Id).TrySetResult(exception);

            if (eventId.Id == FailingEventId)
                throw new InvalidOperationException("Logging failed");
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        private TaskCompletionSource<Exception?> GetLoggedEvent(int eventId) =>
            _loggedEvents.GetOrAdd(eventId, _ => new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously));
    }

    // Starts aborting the sequence, once, as if the subscribers stopped enumerating, when adding a message to it fails. The sequence logs
    // the failure while holding the add semaphore: the abort switches it to the aborted state synchronously, then waits for the add to
    // complete.
    private sealed class AbortOnAddFailedLogger : ISilverbackLogger<SequenceBase<IInboundEnvelope>>, ILogger
    {
        private const string SequenceAddFailedEventName = "Silverback.Integration_Tracing_SequenceAddFailed";

        public ISequence? SequenceToAbort { get; set; }

        public Exception? AddFailedException { get; private set; }

        public Task? AbortTask { get; private set; }

        public ILogger InnerLogger => this;

        public bool IsEnabled(LogEvent logEvent) => true;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (eventId.Name != SequenceAddFailedEventName || SequenceToAbort == null || AbortTask != null)
                return;

            AddFailedException = exception;
            AbortTask = SequenceToAbort.AbortAsync(SequenceAbortReason.EnumerationAborted);
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
    }
}
