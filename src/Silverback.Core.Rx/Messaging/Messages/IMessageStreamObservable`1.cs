// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Represent a stream of messages being published via the message bus. It is an observable that is asynchronously pushed with messages.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being streamed.
/// </typeparam>
public interface IMessageStreamObservable<out TMessage> : IObservable<TMessage>;
