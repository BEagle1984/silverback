// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps a message when it's being transferred over a message broker.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the message.
/// </typeparam>
public interface IEnvelope<out TMessage> : IEnvelope, IMessageWrapper<TMessage>;
