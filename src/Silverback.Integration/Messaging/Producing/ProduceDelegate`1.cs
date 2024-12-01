// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing;

/// <summary>
///     Used by the <see cref="DelegatedProducer{T}" />.
/// </summary>
/// <typeparam name="T">
///     The type of the state object that can be passed to the delegate.
/// </typeparam>
internal delegate Task ProduceDelegate<in T>(IOutboundEnvelope envelope, T state, CancellationToken cancellationToken);
