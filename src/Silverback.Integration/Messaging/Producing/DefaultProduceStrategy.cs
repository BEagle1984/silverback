// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing;

/// <summary>
///     This is the default produce strategy, which immediately pushes to the message broker via the underlying library and
///     according to the endpoint settings.
/// </summary>
public sealed class DefaultProduceStrategy : IProduceStrategy, IEquatable<DefaultProduceStrategy>
{
    private static readonly DefaultProduceStrategyImplementation Implementation = new();

    /// <inheritdoc cref="op_Equality" />
    public static bool operator ==(DefaultProduceStrategy? left, DefaultProduceStrategy? right) => Equals(left, right);

    /// <inheritdoc cref="op_Inequality" />
    public static bool operator !=(DefaultProduceStrategy? left, DefaultProduceStrategy? right) => !Equals(left, right);

    /// <inheritdoc cref="IProduceStrategy.Build" />
    public IProduceStrategyImplementation Build(IServiceProvider serviceProvider, ProducerEndpointConfiguration endpointConfiguration) =>
        Implementation;

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(DefaultProduceStrategy? other) => other != null;

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(IProduceStrategy? other) => other is DefaultProduceStrategy;

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj) => obj is DefaultProduceStrategy;

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => GetType().GetHashCode();

    private sealed class DefaultProduceStrategyImplementation : IProduceStrategyImplementation
    {
        public async Task ProduceAsync(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            await envelope.Producer.ProduceAsync(envelope).ConfigureAwait(false);
        }

        [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Intentional and not an issue here.")]
        [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "False positive.")]
        public async Task ProduceAsync(IEnumerable<IOutboundEnvelope> envelopes)
        {
            Check.NotNull(envelopes, nameof(envelopes));

            // The extra 1 is to prevent the completion before the iteration is over and all messages are enqueued
            int pending = 1;
            TaskCompletionSource<bool> taskCompletionSource = new();
            Exception? produceException = null;

            foreach (IOutboundEnvelope envelope in envelopes)
            {
                if (taskCompletionSource.Task.IsCompleted)
                    break;

                Interlocked.Increment(ref pending);

                envelope.Producer.Produce(
                    envelope,
                    _ =>
                    {
                        if (Interlocked.Decrement(ref pending) == 0)
                            taskCompletionSource.TrySetResult(true);
                    },
                    exception =>
                    {
                        produceException = exception;

                        if (Interlocked.Decrement(ref pending) == 0)
                            taskCompletionSource.TrySetResult(false);
                    });
            }

            if (Interlocked.Decrement(ref pending) > 0)
                await taskCompletionSource.Task.ConfigureAwait(false);

            if (produceException != null)
                throw produceException;
        }

        [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Intentional and not an issue here.")]
        [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "False positive.")]
        public async Task ProduceAsync(IAsyncEnumerable<IOutboundEnvelope> envelopes)
        {
            // The extra 1 is to prevent the completion before the iteration is over and all messages are enqueued
            int pending = 1;
            TaskCompletionSource<bool> taskCompletionSource = new();
            Exception? produceException = null;

            await foreach (IOutboundEnvelope envelope in envelopes)
            {
                if (taskCompletionSource.Task.IsCompleted)
                    break;

                Interlocked.Increment(ref pending);

                envelope.Producer.Produce(
                    envelope,
                    _ =>
                    {
                        if (Interlocked.Decrement(ref pending) == 0)
                            taskCompletionSource.TrySetResult(true);
                    },
                    exception =>
                    {
                        produceException = exception;

                        if (Interlocked.Decrement(ref pending) == 0)
                            taskCompletionSource.TrySetResult(false);
                    });
            }

            if (Interlocked.Decrement(ref pending) > 0)
                await taskCompletionSource.Task.ConfigureAwait(false);

            if (produceException != null)
                throw produceException;
        }
    }
}
