// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.TransactionalOutbox;

public class OutboxProduceStrategyTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static readonly TestProducerEndpointConfiguration EndpointConfiguration = new("test")
    {
        FriendlyName = "test-endpoint",
        Serializer = new TestSerializer()
    };

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingWithSameTypeAndSettings()
    {
        OutboxProduceStrategy strategy1 = new(new TestSettings("outbox"));
        OutboxProduceStrategy strategy2 = new(new TestSettings("outbox"));

        strategy1.Equals(strategy2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithDifferentType()
    {
        OutboxProduceStrategy strategy1 = new(new TestSettings("outbox"));
        DefaultProduceStrategy strategy2 = new();

        strategy1.Equals(strategy2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithDifferentSettings()
    {
        OutboxProduceStrategy strategy1 = new(new TestSettings("outbox1"));
        OutboxProduceStrategy strategy2 = new(new TestSettings("outbox2"));

        strategy1.Equals(strategy2).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProduceAsync_ShouldAddBatchToOutbox(bool asyncEnumerable)
    {
        TestOutboxWriter writer = new();
        IProduceStrategyImplementation strategy = BuildStrategy(writer);

        await ProduceBatchAsync(strategy, [CreateEnvelope("one"), CreateEnvelope("two"), CreateEnvelope("three")], asyncEnumerable);

        writer.Messages.Select(message => Encoding.UTF8.GetString(message.Content!)).ShouldBe(["one", "two", "three"]);
        writer.Messages.ShouldAllBe(message => message.EndpointName == "test-endpoint");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProduceAsync_ShouldThrow_WhenProducingBatchFails(bool asyncEnumerable)
    {
        TestOutboxWriter writer = new();
        IProduceStrategyImplementation strategy = BuildStrategy(writer);

        Exception? exception = await Record.ExceptionAsync(() => ProduceBatchAsync(
            strategy,
            [CreateEnvelope("one"), CreateEnvelope(TestSerializer.FailingContent), CreateEnvelope("three")],
            asyncEnumerable));

        exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Serialization failed.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProduceAsync_ShouldThrow_WhenEnumeratingBatchFails(bool asyncEnumerable)
    {
        TestOutboxWriter writer = new();
        IProduceStrategyImplementation strategy = BuildStrategy(writer);

        static IEnumerable<IOutboundEnvelope> GetEnvelopes()
        {
            yield return CreateEnvelope("one");

            throw new InvalidOperationException("Enumeration failed.");
        }

        Exception? exception = await Record.ExceptionAsync(() => ProduceBatchAsync(strategy, GetEnvelopes(), asyncEnumerable));

        exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Enumeration failed.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProduceAsync_ShouldThrow_WhenBatchIsCanceled(bool asyncEnumerable)
    {
        TestOutboxWriter writer = new();
        IProduceStrategyImplementation strategy = BuildStrategy(writer);
        using CancellationTokenSource cancellationTokenSource = new();

        IEnumerable<IOutboundEnvelope> GetEnvelopes()
        {
            yield return CreateEnvelope("one");

            cancellationTokenSource.Cancel();

            yield return CreateEnvelope("two");
        }

        Exception? exception = await Record.ExceptionAsync(() => ProduceBatchAsync(
            strategy,
            GetEnvelopes(),
            asyncEnumerable,
            cancellationTokenSource.Token));

        exception.ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProduceAsync_ShouldThrow_WhenBatchIsCanceledWhileWriting(bool asyncEnumerable)
    {
        using CancellationTokenSource cancellationTokenSource = new();
        TestOutboxWriter writer = new()
        {
            OnMessageWritten = cancellationTokenSource.Cancel
        };
        IProduceStrategyImplementation strategy = BuildStrategy(writer);

        Exception? exception = await Record.ExceptionAsync(() => ProduceBatchAsync(
            strategy,
            [CreateEnvelope("one"), CreateEnvelope("two")],
            asyncEnumerable,
            cancellationTokenSource.Token));

        exception.ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public async Task ProduceAsync_ShouldThrow_WhenBatchIsAlreadyCanceled(bool asyncEnumerable, int count)
    {
        TestOutboxWriter writer = new();
        IProduceStrategyImplementation strategy = BuildStrategy(writer);

        Exception? exception = await Record.ExceptionAsync(() => ProduceBatchAsync(
            strategy,
            Enumerable.Range(1, count).Select(index => CreateEnvelope($"message {index}")),
            asyncEnumerable,
            new CancellationToken(true)));

        exception.ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProduceAsync_ShouldThrow_WhenWritingBatchFailsBeforeEnumerating(bool asyncEnumerable)
    {
        TestOutboxWriter writer = new()
        {
            OnEnumerating = () => throw new InvalidOperationException("Write failed.")
        };
        IProduceStrategyImplementation strategy = BuildStrategy(writer);

        Exception? exception = await Record.ExceptionAsync(() => ProduceBatchAsync(
            strategy,
            [CreateEnvelope("one"), CreateEnvelope("two"), CreateEnvelope("three")],
            asyncEnumerable));

        exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Write failed.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProduceAsync_ShouldThrow_WhenWritingBatchFailsWhileEnumerating(bool asyncEnumerable)
    {
        TestOutboxWriter writer = new()
        {
            OnMessageWritten = () => throw new InvalidOperationException("Write failed.")
        };
        IProduceStrategyImplementation strategy = BuildStrategy(writer);

        Exception? exception = await Record.ExceptionAsync(() => ProduceBatchAsync(
            strategy,
            [CreateEnvelope("one"), CreateEnvelope("two"), CreateEnvelope("three")],
            asyncEnumerable));

        exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Write failed.");
        writer.Messages.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProduceAsync_ShouldThrow_WhenWritingBatchFailsAfterEnumerating(bool asyncEnumerable)
    {
        TestOutboxWriter writer = new()
        {
            OnEnumerated = () => throw new InvalidOperationException("Write failed.")
        };
        IProduceStrategyImplementation strategy = BuildStrategy(writer);

        Exception? exception = await Record.ExceptionAsync(() => ProduceBatchAsync(
            strategy,
            [CreateEnvelope("one"), CreateEnvelope("two"), CreateEnvelope("three")],
            asyncEnumerable));

        exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Write failed.");
        writer.Messages.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ProduceAsync_ShouldThrowWriterException_WhenAsyncBatchFailsAfterWriterFailure()
    {
        TestOutboxWriter writer = new()
        {
            OnEnumerating = () => throw new InvalidOperationException("Write failed.")
        };
        IProduceStrategyImplementation strategy = BuildStrategy(writer);
        bool isEnumerationStopped = false;

        // Waits until canceled because of the writer failure, then fails as well
        async IAsyncEnumerable<IOutboundEnvelope> GetEnvelopesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Delay the failure, to ensure that the producing task is awaited
                await Task.Delay(100, CancellationToken.None);
                isEnumerationStopped = true;
                throw new InvalidOperationException("Enumeration failed.");
            }

            yield return CreateEnvelope("one");
        }

        Exception? exception = await Record.ExceptionAsync(() => RunWithTimeoutAsync(() => strategy.ProduceAsync(
            GetEnvelopesAsync(),
            CancellationToken.None)));

        exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Write failed.");
        isEnumerationStopped.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited")]
    public async Task ProduceAsync_ShouldThrowWriterException_WhenCancellationCallbackFails()
    {
        using ManualResetEventSlim callbackRegistered = new();
        TestOutboxWriter writer = new()
        {
            OnEnumerating = () =>
            {
                // Fail only after the callback is registered, so that it's invoked when the producing is canceled
                callbackRegistered.Wait(Timeout).ShouldBeTrue();
                throw new InvalidOperationException("Write failed.");
            }
        };
        IProduceStrategyImplementation strategy = BuildStrategy(writer);

        async IAsyncEnumerable<IOutboundEnvelope> GetEnvelopesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using CancellationTokenRegistration registration =
                cancellationToken.Register(() => throw new InvalidOperationException("Callback failed."));
            callbackRegistered.Set();

            await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, cancellationToken);

            yield return CreateEnvelope("one");
        }

        Exception? exception = await Record.ExceptionAsync(() => RunWithTimeoutAsync(() => strategy.ProduceAsync(
            GetEnvelopesAsync(),
            CancellationToken.None)));

        exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Write failed.");
    }

    private static IProduceStrategyImplementation BuildStrategy(TestOutboxWriter writer)
    {
        ServiceCollection services = [];
        services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        TestSettings settings = new("outbox");
        serviceProvider.GetRequiredService<OutboxWriterFactory>().AddFactory<TestSettings>((_, _) => writer);

        return new OutboxProduceStrategy(settings).Build(new SilverbackContext(serviceProvider), EndpointConfiguration);
    }

    private static OutboundEnvelope<TestEventOne> CreateEnvelope(string content) =>
        new(new TestEventOne { Content = content }, null, EndpointConfiguration, Substitute.For<IProducer>());

    private static Task ProduceBatchAsync(
        IProduceStrategyImplementation strategy,
        IEnumerable<IOutboundEnvelope> envelopes,
        bool asyncEnumerable,
        CancellationToken cancellationToken = default) =>
        RunWithTimeoutAsync(() => asyncEnumerable
            ? strategy.ProduceAsync(envelopes.ToAsyncEnumerable(), cancellationToken)
            : strategy.ProduceAsync(envelopes, cancellationToken));

    // Run in a separate task, so that a synchronous hang makes the test fail instead of blocking it
    private static Task RunWithTimeoutAsync(Func<Task> action) =>
        Task.Run(action, CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing")]
    private record TestSettings(string Name) : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }

    private sealed class TestSerializer : IMessageSerializer
    {
        public const string FailingContent = "fail";

        public ValueTask<Stream?> SerializeAsync(
            object? message,
            MessageHeaderCollection headers,
            ProducerEndpoint endpoint)
        {
            string content = ((TestEventOne)message!).Content!;

            if (content == FailingContent)
                throw new InvalidOperationException("Serialization failed.");

            return ValueTask.FromResult<Stream?>(new MemoryStream(Encoding.UTF8.GetBytes(content)));
        }
    }

    private sealed class TestOutboxWriter : IOutboxWriter
    {
        public List<OutboxMessage> Messages { get; } = [];

        public Action? OnEnumerating { get; init; }

        public Action? OnMessageWritten { get; init; }

        public Action? OnEnumerated { get; init; }

        public Task AddAsync(
            OutboxMessage outboxMessage,
            ISilverbackContext? context = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(
            IEnumerable<OutboxMessage> outboxMessages,
            ISilverbackContext? context = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async Task AddAsync(
            IAsyncEnumerable<OutboxMessage> outboxMessages,
            ISilverbackContext? context = null,
            CancellationToken cancellationToken = default)
        {
            OnEnumerating?.Invoke();

            await foreach (OutboxMessage outboxMessage in outboxMessages.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                Messages.Add(outboxMessage);
                OnMessageWritten?.Invoke();
            }

            OnEnumerated?.Invoke();
        }
    }
}
