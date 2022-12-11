// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Domain;

/// <summary>
///     An event that generates inside the domain (model).
/// </summary>
public interface IDomainEvent : IMessageWithSource, IEvent
{
}

/// <inheritdoc cref="IDomainEvent" />
/// <typeparam name="TEntity">
///     The type of the related domain entity.
/// </typeparam>
public interface IDomainEvent<out TEntity> : IDomainEvent
    where TEntity : class
{
    /// <summary>
    ///     Gets the reference to the domain entity that generated this event.
    /// </summary>
    new TEntity? Source { get; }
}
