// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Publishes messages via the message bus, forwarding them to the subscribers.
/// </summary>
/// <remarks>
///     Extended by the <see cref="IPublisher" /> interface to add specific methods to publish <see cref="ICommand" />/<see cref="ICommand{TResult}" />,
///     <see cref="IQuery{TResult}" />, and <see cref="IEvent" /> messages.
/// </remarks>
public partial interface IApplicationPublisher;
