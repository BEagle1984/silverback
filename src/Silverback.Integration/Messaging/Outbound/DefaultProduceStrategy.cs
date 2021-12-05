// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound;

/// <summary>
///     This is the default produce strategy, which immediately pushes to the message broker via the underlying library and
///     according to the endpoint settings.
/// </summary>
public sealed class DefaultProduceStrategy : IProduceStrategy, IEquatable<DefaultProduceStrategy>
{
    private DefaultProduceStrategyImplementation? _implementation;

    /// <inheritdoc cref="op_Equality" />
    public static bool operator ==(DefaultProduceStrategy? left, DefaultProduceStrategy? right) => Equals(left, right);

    /// <inheritdoc cref="op_Inequality" />
    public static bool operator !=(DefaultProduceStrategy? left, DefaultProduceStrategy? right) => !Equals(left, right);

    /// <inheritdoc cref="IProduceStrategy.Build" />
    public IProduceStrategyImplementation Build(IServiceProvider serviceProvider) =>
        _implementation ??= new DefaultProduceStrategyImplementation(serviceProvider.GetRequiredService<IBrokerCollection>());

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
        private readonly IBrokerCollection _brokerCollection;

        public DefaultProduceStrategyImplementation(IBrokerCollection brokerCollection)
        {
            _brokerCollection = brokerCollection;
        }

        public async Task ProduceAsync(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            IProducer producer = await _brokerCollection.GetProducerAsync(envelope.Endpoint.Configuration).ConfigureAwait(false);
            await producer.ProduceAsync(envelope).ConfigureAwait(false);
        }
    }
}
