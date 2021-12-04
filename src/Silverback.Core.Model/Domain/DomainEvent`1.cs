// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Domain;

/// <inheritdoc cref="IDomainEvent{TEntity}" />
public abstract class DomainEvent<TEntity> : IDomainEvent<TEntity>
    where TEntity : class
{
    /// <inheritdoc cref="IDomainEvent{TEntity}.Source" />
    public TEntity? Source { get; set; }

    /// <inheritdoc cref="IMessageWithSource.Source" />
    object? IMessageWithSource.Source
    {
        get => Source;
        set => Source = (TEntity?)value;
    }
}
