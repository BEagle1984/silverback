// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing;

public class DefaultProduceStrategyTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan BlockingWaitTimeout = TimeSpan.FromSeconds(3);

    private static readonly TestProducerEndpointConfiguration EndpointConfiguration = new("test");

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameType()
    {
        DefaultProduceStrategy strategy = new();
        DefaultProduceStrategy otherStrategy = new();

        strategy.Equals(otherStrategy).ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentType()
    {
        DefaultProduceStrategy strategy = new();
        OutboxProduceStrategy otherStrategy = new(new InMemoryOutboxSettings());

        strategy.Equals(otherStrategy).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The callbacks run before the test completes")]
    public async Task ProduceAsync_ShouldNotContinueOnBrokerClientThread_WhenProducingBatch(bool asyncEnumerable)
    {
        using CallbackThread callbackThread = new();
        IProducer producer = Substitute.For<IProducer>();
        producer
            .When(mock => mock.Produce(
                Arg.Any<IOutboundEnvelope>(),
                Arg.Any<Action<IBrokerMessageIdentifier?>>(),
                Arg.Any<Action<Exception>>()))
            .Do(call => callbackThread.Enqueue(() => call.ArgAt<Action<IBrokerMessageIdentifier?>>(1).Invoke(null)));
        IProduceStrategyImplementation strategy = new DefaultProduceStrategy().Build(
            Substitute.For<ISilverbackContext>(),
            EndpointConfiguration);
        OutboundEnvelope<TestEventOne>[] envelopes = [CreateEnvelope(producer), CreateEnvelope(producer)];

        Task produceTask = asyncEnumerable
            ? strategy.ProduceAsync(envelopes.ToAsyncEnumerable(), CancellationToken.None)
            : strategy.ProduceAsync(envelopes, CancellationToken.None);
        Task<bool> continuationTask = ContinueAfterProducingAsync();

        callbackThread.Resume();

        bool hasCompleted = await continuationTask.WaitAsync(Timeout);
        hasCompleted.ShouldBeTrue();

        // Await without synchronization context, like most applications do, and then block until the broker client's thread handles a
        // new message, like a synchronous produce does
        async Task<bool> ContinueAfterProducingAsync()
        {
            await produceTask.ConfigureAwait(false);
            return callbackThread.InvokeAndWait(BlockingWaitTimeout);
        }
    }

    private static OutboundEnvelope<TestEventOne> CreateEnvelope(IProducer producer) =>
        new(new TestEventOne(), null, EndpointConfiguration, producer);

    // Invokes the producer callbacks on a single dedicated thread, like the broker clients do, starting only when resumed
    private sealed class CallbackThread : IDisposable
    {
        private readonly BlockingCollection<Action> _actions = [];

        private readonly ManualResetEventSlim _resumedEvent = new();

        private readonly Thread _thread;

        public CallbackThread()
        {
            _thread = new Thread(Run)
            {
                IsBackground = true,
                Name = "Broker client callbacks"
            };
            _thread.Start();
        }

        public void Enqueue(Action action) => _actions.Add(action);

        public void Resume() => _resumedEvent.Set();

        public bool InvokeAndWait(TimeSpan timeout)
        {
            TaskCompletionSource taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _actions.Add(() => taskCompletionSource.TrySetResult());
            return taskCompletionSource.Task.Wait(timeout);
        }

        public void Dispose()
        {
            _actions.CompleteAdding();
            _resumedEvent.Set();
            _thread.Join(Timeout);
            _actions.Dispose();
            _resumedEvent.Dispose();
        }

        private void Run()
        {
            _resumedEvent.Wait();

            foreach (Action action in _actions.GetConsumingEnumerable())
            {
                action.Invoke();
            }
        }
    }
}
