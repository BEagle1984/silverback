// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
    }
}
