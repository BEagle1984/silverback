// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class ProducerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void Produce_ShouldInvokeOnSuccessOnceWithNullIdentifier_WhenBehaviorDoesNotCallNext()
    {
        using TestProducer producer = new(new SkipBehavior());
        CallbacksRecorder callbacks = new();

        callbacks.Produce(producer);

        producer.ProducedMessages.ShouldBeEmpty();
        callbacks.SuccessIdentifiers.ShouldHaveSingleItem().ShouldBeNull();
        callbacks.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Produce_ShouldInvokeOnSuccessOnceWithIdentifier_WhenMessageIsAcknowledged()
    {
        using TestProducer producer = new();
        CallbacksRecorder callbacks = new();
        TestOffset identifier = new("key", "42");

        callbacks.Produce(producer);

        callbacks.InvocationsCount.ShouldBe(0);

        TestProducer.ProducedMessage producedMessage = producer.ProducedMessages.ShouldHaveSingleItem();
        producedMessage.Succeed(identifier);

        callbacks.SuccessIdentifiers.ShouldHaveSingleItem().ShouldBe(identifier);
        callbacks.Errors.ShouldBeEmpty();
        producedMessage.Envelope.BrokerMessageIdentifier.ShouldBe(identifier);
    }

    [Fact]
    public void Produce_ShouldInvokeOnErrorOnce_WhenMessageFails()
    {
        using TestProducer producer = new();
        CallbacksRecorder callbacks = new();
        InvalidOperationException exception = new("test");

        callbacks.Produce(producer);

        callbacks.InvocationsCount.ShouldBe(0);

        producer.ProducedMessages.ShouldHaveSingleItem().Fail(exception);

        callbacks.SuccessIdentifiers.ShouldBeEmpty();
        callbacks.Errors.ShouldHaveSingleItem().ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task Produce_ShouldLogDeliveryFailureWithinProduceActivity()
    {
        CurrentActivityLogger innerLogger = new(IntegrationLogEvents.ErrorProducingMessage);
        using TestProducer producer = new(innerLogger, new ActivityProducerBehavior());
        Activity? produceActivity = null;
        producer.OnProduceCore = _ => produceActivity = Activity.Current;

        new CallbacksRecorder().Produce(producer);
        TestProducer.ProducedMessage producedMessage = producer.ProducedMessages.ShouldHaveSingleItem();

        // The delivery report comes from a thread to which the execution context of the caller doesn't flow
        Task deliveryTask;
        using (ExecutionContext.SuppressFlow())
        {
            deliveryTask = Task.Run(() => producedMessage.Fail(new InvalidOperationException("test")));
        }

        await deliveryTask;

        produceActivity.ShouldNotBeNull();
        innerLogger.CurrentActivity.ShouldBeSameAs(produceActivity);
        produceActivity.IsStopped.ShouldBeTrue();
    }

    [Fact]
    public void Produce_ShouldInvokeOnSuccessOnceAfterAllMessagesAreAcknowledged_WhenFinalActionIsInvokedMultipleTimes()
    {
        using TestProducer producer = new(new MultipleProduceBehavior(3));
        CallbacksRecorder callbacks = new();

        callbacks.Produce(producer);

        producer.ProducedMessages.Count.ShouldBe(3);

        producer.ProducedMessages[2].Succeed(new TestOffset());
        producer.ProducedMessages[0].Succeed(new TestOffset());

        callbacks.InvocationsCount.ShouldBe(0);

        producer.ProducedMessages[1].Succeed(new TestOffset());

        // Same identifier as returned by ProduceAsync, since the messages are produced via cloned contexts
        callbacks.SuccessIdentifiers.ShouldHaveSingleItem().ShouldBeNull();
        callbacks.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProduceAsync_ShouldReturnNullIdentifier_WhenFinalActionIsInvokedMultipleTimes()
    {
        using TestProducer producer = new(new MultipleProduceBehavior(3));

        IBrokerMessageIdentifier? identifier = await producer.ProduceAsync(new TestEventOne());

        identifier.ShouldBeNull();
        producer.ProducedAsyncCount.ShouldBe(3);
    }

    [Fact]
    public void Produce_ShouldInvokeOnErrorOnceWithFirstErrorAfterAllMessagesAreCompleted_WhenSomeMessagesFail()
    {
        using TestProducer producer = new(new MultipleProduceBehavior(4));
        CallbacksRecorder callbacks = new();
        InvalidOperationException firstException = new("first");
        InvalidOperationException secondException = new("second");

        callbacks.Produce(producer);

        producer.ProducedMessages.Count.ShouldBe(4);

        producer.ProducedMessages[1].Fail(firstException);
        producer.ProducedMessages[0].Succeed(new TestOffset());
        producer.ProducedMessages[3].Fail(secondException);

        callbacks.InvocationsCount.ShouldBe(0);

        producer.ProducedMessages[2].Succeed(new TestOffset());

        callbacks.SuccessIdentifiers.ShouldBeEmpty();
        callbacks.Errors.ShouldHaveSingleItem().ShouldBeSameAs(firstException);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public void Produce_ShouldInvokeCallbackOnce_WhenCallbacksAreInvokedSynchronouslyInsideProduceCore(int messagesCount, bool fail)
    {
        InvalidOperationException exception = new("test");
        using TestProducer producer = new(new MultipleProduceBehavior(messagesCount));
        producer.OnProduceCore = producedMessage =>
        {
            if (fail && producedMessage.Index == messagesCount - 1)
                producedMessage.Fail(exception);
            else
                producedMessage.Succeed(new TestOffset());
        };
        CallbacksRecorder callbacks = new();

        callbacks.Produce(producer);

        producer.ProducedMessages.Count.ShouldBe(messagesCount);
        callbacks.InvocationsCount.ShouldBe(1);

        if (fail)
            callbacks.Errors.ShouldHaveSingleItem().ShouldBeSameAs(exception);
        else
            callbacks.SuccessIdentifiers.ShouldHaveSingleItem();
    }

    [Fact]
    public void Produce_ShouldInvokeCallbackOnce_WhenSomeCallbacksAreInvokedSynchronouslyAndOthersLater()
    {
        using TestProducer producer = new(new MultipleProduceBehavior(4));
        producer.OnProduceCore = producedMessage =>
        {
            if (producedMessage.Index % 2 == 0)
                producedMessage.Succeed(new TestOffset());
        };
        CallbacksRecorder callbacks = new();

        callbacks.Produce(producer);

        callbacks.InvocationsCount.ShouldBe(0);

        producer.ProducedMessages[1].Succeed(new TestOffset());
        producer.ProducedMessages[3].Succeed(new TestOffset());

        callbacks.SuccessIdentifiers.ShouldHaveSingleItem();
        callbacks.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Produce_ShouldInvokeCallbackOnce_WhenCallbacksAreInvokedConcurrentlyFromOtherThreads(bool fail)
    {
        const int messagesCount = 20;

        for (int iteration = 0; iteration < 50; iteration++)
        {
            TaskCompletionSource startSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
            List<Task> completionTasks = [];
            using TestProducer producer = new(new MultipleProduceBehavior(messagesCount));
            producer.OnProduceCore = producedMessage =>
            {
                completionTasks.Add(
                    Task.Run(async () =>
                    {
                        await startSignal.Task;

                        if (fail && producedMessage.Index == messagesCount / 2)
                            producedMessage.Fail(new InvalidOperationException("test"));
                        else
                            producedMessage.Succeed(new TestOffset());
                    }));

                // Let some messages be acknowledged while the pipeline is still running
                if (producedMessage.Index == messagesCount / 4)
                    startSignal.TrySetResult();
            };
            CallbacksRecorder callbacks = new();

            callbacks.Produce(producer);

            await Task.WhenAll(completionTasks).WaitAsync(Timeout);
            await callbacks.WaitUntilInvokedAsync(Timeout);

            producer.ProducedMessages.Count.ShouldBe(messagesCount);
            callbacks.InvocationsCount.ShouldBe(1);

            if (fail)
                callbacks.Errors.ShouldHaveSingleItem();
            else
                callbacks.SuccessIdentifiers.ShouldHaveSingleItem();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The action is invoked synchronously")]
    public void Produce_ShouldRethrowAndNotInvokeCallbacks_WhenPipelineThrowsAfterMessageWasProduced(bool acknowledgeSynchronously)
    {
        using TestProducer producer = new(new ProduceAndThrowBehavior());

        if (acknowledgeSynchronously)
            producer.OnProduceCore = producedMessage => producedMessage.Succeed(new TestOffset());

        CallbacksRecorder callbacks = new();

        Action act = () => callbacks.Produce(producer);

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("pipeline failure");

        TestProducer.ProducedMessage producedMessage = producer.ProducedMessages.ShouldHaveSingleItem();

        if (!acknowledgeSynchronously)
            producedMessage.Succeed(new TestOffset());

        callbacks.InvocationsCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Produce_ShouldInvokeOnSuccessOnce_WhenBehaviorRetriesAfterProduceCoreThrows(bool acknowledgeSynchronously)
    {
        using TestProducer producer = new(new RetryBehavior());
        producer.ThrowingProduceCoreCallsCount = 1;

        if (acknowledgeSynchronously)
            producer.OnProduceCore = producedMessage => producedMessage.Succeed(new TestOffset("key", "42"));

        CallbacksRecorder callbacks = new();

        callbacks.Produce(producer);

        TestProducer.ProducedMessage producedMessage = producer.ProducedMessages.ShouldHaveSingleItem();

        if (!acknowledgeSynchronously)
        {
            callbacks.InvocationsCount.ShouldBe(0);
            producedMessage.Succeed(new TestOffset("key", "42"));
        }

        callbacks.SuccessIdentifiers.ShouldHaveSingleItem().ShouldBe(new TestOffset("key", "42"));
        callbacks.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Produce_ShouldInvokeOnSuccessOnceWithNullIdentifier_WhenBehaviorSwallowsProduceCoreException()
    {
        using TestProducer producer = new(new SwallowBehavior());
        producer.ThrowingProduceCoreCallsCount = 1;
        CallbacksRecorder callbacks = new();

        callbacks.Produce(producer);

        producer.ProducedMessages.ShouldBeEmpty();
        callbacks.SuccessIdentifiers.ShouldHaveSingleItem().ShouldBeNull();
        callbacks.Errors.ShouldBeEmpty();
    }

    private sealed class CallbacksRecorder
    {
        private readonly ConcurrentQueue<IBrokerMessageIdentifier?> _successIdentifiers = new();

        private readonly ConcurrentQueue<Exception> _errors = new();

        private readonly TaskCompletionSource _invoked = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _invocationsCount;

        public IReadOnlyCollection<IBrokerMessageIdentifier?> SuccessIdentifiers => _successIdentifiers;

        public IReadOnlyCollection<Exception> Errors => _errors;

        public int InvocationsCount => _invocationsCount;

        public void Produce(IProducer producer) =>
            producer.Produce(
                new TestEventOne(),
                null,
                static (identifier, recorder) => recorder.OnSuccess(identifier),
                static (exception, recorder) => recorder.OnError(exception),
                this);

        public Task WaitUntilInvokedAsync(TimeSpan timeout) => _invoked.Task.WaitAsync(timeout);

        private void OnSuccess(IBrokerMessageIdentifier? identifier)
        {
            _successIdentifiers.Enqueue(identifier);
            Interlocked.Increment(ref _invocationsCount);
            _invoked.TrySetResult();
        }

        private void OnError(Exception exception)
        {
            _errors.Enqueue(exception);
            Interlocked.Increment(ref _invocationsCount);
            _invoked.TrySetResult();
        }
    }

    private sealed class TestProducer : Producer
    {
        private readonly List<ProducedMessage> _producedMessages = [];

        private int _producedAsyncCount;

        public TestProducer(params IProducerBehavior[] behaviors)
            : this(Substitute.For<ILogger>(), behaviors)
        {
        }

        public TestProducer(ILogger innerLogger, params IProducerBehavior[] behaviors)
            : base(
                "test",
                Substitute.For<IBrokerClient>(),
                TestProducerEndpointConfiguration.GetDefault(),
                CreateBehaviorsProvider(behaviors),
                CreateServiceProvider(innerLogger),
                Substitute.For<ISilverbackLogger<IProducer>>())
        {
        }

        public Action<ProducedMessage>? OnProduceCore { get; set; }

        // The first calls to the callback-based ProduceCore throw synchronously, without producing the message
        public int ThrowingProduceCoreCallsCount { get; set; }

        public IReadOnlyList<ProducedMessage> ProducedMessages
        {
            get
            {
                lock (_producedMessages)
                {
                    return [.. _producedMessages];
                }
            }
        }

        public int ProducedAsyncCount => _producedAsyncCount;

        protected override IBrokerMessageIdentifier ProduceCore(IOutboundEnvelope envelope) => new TestOffset();

        protected override void ProduceCore<TState>(
            IOutboundEnvelope envelope,
            Action<IBrokerMessageIdentifier?, TState> onSuccess,
            Action<Exception, TState> onError,
            TState state)
        {
            ProducedMessage producedMessage;

            lock (_producedMessages)
            {
                if (ThrowingProduceCoreCallsCount > 0)
                {
                    ThrowingProduceCoreCallsCount--;
                    throw new InvalidOperationException("produce failure");
                }

                producedMessage = new ProducedMessage(
                    _producedMessages.Count,
                    envelope,
                    identifier => onSuccess.Invoke(identifier, state),
                    exception => onError.Invoke(exception, state));
                _producedMessages.Add(producedMessage);
            }

            OnProduceCore?.Invoke(producedMessage);
        }

        protected override ValueTask<IBrokerMessageIdentifier?> ProduceCoreAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _producedAsyncCount);
            return ValueTask.FromResult<IBrokerMessageIdentifier?>(new TestOffset());
        }

        private static IBrokerBehaviorsProvider<IProducerBehavior> CreateBehaviorsProvider(IProducerBehavior[] behaviors)
        {
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider = Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>();
            behaviorsProvider.GetBehaviorsList().Returns(behaviors);
            return behaviorsProvider;
        }

        private static IServiceProvider CreateServiceProvider(ILogger innerLogger)
        {
            ISilverbackLogger<IProducer> logger = Substitute.For<ISilverbackLogger<IProducer>>();
            logger.InnerLogger.Returns(innerLogger);

            IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(ISilverbackLogger<IProducer>)).Returns(logger);
            return serviceProvider;
        }

        public sealed record ProducedMessage(
            int Index,
            IOutboundEnvelope Envelope,
            Action<IBrokerMessageIdentifier?> Succeed,
            Action<Exception> Fail);
    }

    // Records the current activity when the specified event is logged
    private sealed class CurrentActivityLogger : ILogger
    {
        private readonly EventId _eventId;

        public CurrentActivityLogger(LogEvent logEvent)
        {
            _eventId = logEvent.EventId;
        }

        public Activity? CurrentActivity { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId == _eventId)
                CurrentActivity = Activity.Current;
        }
    }

    private sealed class SkipBehavior : IProducerBehavior
    {
        public int SortIndex => 0;

        public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;
    }

    // Produces the message multiple times via cloned contexts, like the chunking does
    private sealed class MultipleProduceBehavior : IProducerBehavior
    {
        private readonly int _count;

        public MultipleProduceBehavior(int count)
        {
            _count = count;
        }

        public int SortIndex => 0;

        public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
        {
            for (int i = 0; i < _count; i++)
            {
                await next(context.Clone(), cancellationToken);
            }
        }
    }

    private sealed class ProduceAndThrowBehavior : IProducerBehavior
    {
        public int SortIndex => 0;

        public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
        {
            await next(context.Clone(), cancellationToken);

            throw new InvalidOperationException("pipeline failure");
        }
    }

    private sealed class RetryBehavior : IProducerBehavior
    {
        public int SortIndex => 0;

        public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
        {
            try
            {
                await next(context, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                await next(context, cancellationToken);
            }
        }
    }

    private sealed class SwallowBehavior : IProducerBehavior
    {
        public int SortIndex => 0;

        public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
        {
            try
            {
                await next(context, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // Swallow the exception
            }
        }
    }
}
